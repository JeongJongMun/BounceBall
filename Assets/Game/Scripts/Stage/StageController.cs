using System.Collections;
using System.Collections.Generic;
using Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    // 스테이지 기본 데이터 + 목표 아이템 집계, 클리어 판정, 맵 이탈 판정과 체크포인트 부활
    // (기획 §21~§24). 체크포인트 컴포넌트는 SaveCheckpoint를 호출해 이 계층에 얹힌다.
    public class StageController : MonoBehaviour
    {
        // 인스펙터 그룹은 StageControllerEditor가 그린다. 여기서는 라벨과 설명만 붙인다.
        [Label("스테이지 ID")]
        [SerializeField] private string stageId = "Stage01";

        [Label("시작 위치")]
        [Tooltip("플레이어가 스폰되는 지점. 체크포인트가 없으면 여기서 부활한다")]
        [SerializeField] private Transform startPosition;

        [Label("왼쪽 경계 X")]
        [SerializeField] private float stageMinX = -10f;

        [Label("오른쪽 경계 X")]
        [SerializeField] private float stageMaxX = 10f;

        [Label("아래 경계 Y")]
        [SerializeField] private float stageMinY = -6f;

        [Label("위 경계 Y")]
        [SerializeField] private float stageMaxY = 6f;

        [Label("낙사선 Y")]
        [Tooltip("이 아래로 떨어지면 체크포인트에서 부활한다")]
        [SerializeField] private float stageFallLimitY = -8f;

        [Label("경계 여백")]
        [Tooltip("타일 끝에서 카메라가 더 보여주는 빈 공간")]
        [SerializeField] private float boundsPadding = 3f;

        [Label("카메라 크기 (줌)")]
        [Tooltip("화면에 얼마나 넓게 보일지. 세로 절반 크기 기준 — 5면 위아래로 10칸이 보인다")]
        [SerializeField] private float cameraZoom = 5f;

        [Label("세로 오프셋")]
        [Tooltip("값을 올리면 캐릭터가 화면 아래쪽에 놓여 위쪽이 더 보인다")]
        [SerializeField] private float cameraVerticalOffset = 0f;

        [Label("가로 추적 잠금")]
        [Tooltip("켜면 카메라가 좌우로 움직이지 않는다 (한 화면 스테이지, 세로 전용 스테이지)")]
        [SerializeField] private bool lockCameraX = false;

        [Label("세로 추적 잠금")]
        [Tooltip("켜면 카메라가 위아래로 움직이지 않는다 (가로로만 진행하는 평지 스테이지)")]
        [SerializeField] private bool lockCameraY = false;

        [Label("인트로 카메라 사용")]
        [Tooltip("스테이지에 들어올 때마다 맵 전체를 한 번 보여주고 플레이 화면으로 줌인한다")]
        [SerializeField] private bool useIntroCamera = true;

        [Label("최대 축소 한계")]
        [Tooltip("인트로에서 이 크기보다 더 축소하지 않는다. 0이면 무제한 — 세로로 아주 긴 맵에서 타일이 너무 작아지면 값을 준다")]
        [SerializeField] private float introMaxSize = 0f;

        [Label("카메라 설정 덮어쓰기")]
        [Tooltip("이 스테이지만 조작감을 다르게 할 때 지정. 비우면 글로벌 CameraSettings를 쓴다")]
        [SerializeField] private CameraSettings cameraSettingsOverride;

        [Label("투명 벽 사용")]
        [Tooltip("끄면 플레이어가 화면 밖으로 나갈 수 있다")]
        [SerializeField] private bool useBoundaryWalls = true;

        [Label("벽 위치 방식")]
        [Tooltip("경계 기준: 카메라 경계에서 자동 계산 / 직접 지정: 좌우 X를 손으로 입력")]
        [SerializeField] private BoundaryWallMode wallMode = BoundaryWallMode.FromBounds;

        [Label("경계에서 벌리기")]
        [Tooltip("벽을 경계선보다 얼마나 더 바깥에 세울지. 0이면 화면 끝까지 갈 수 있다")]
        [SerializeField] private float wallOffsetFromBounds = 0f;

        [Label("왼쪽 벽 X")]
        [SerializeField] private float leftWallX = -12f;

        [Label("오른쪽 벽 X")]
        [SerializeField] private float rightWallX = 12f;

        [Label("벽 높이 여유")]
        [Tooltip("낙사선부터 위로 얼마나 높게 세울지. 세로로 긴 스테이지에서 위로 빠져나가면 키운다")]
        [SerializeField] private float wallHeadroom = 30f;

        [Label("전체 배치 수량")]
        [Tooltip("스테이지에 놓인 목표 아이템 개수. 검증 버튼이 자동으로 맞춰준다")]
        [SerializeField] private int totalGoalItemCount;

        [Label("클리어 요구 수량")]
        [Tooltip("이만큼 모으면 스테이지가 클리어된다")]
        [SerializeField] private int requiredGoalItemCount;

        [Label("클리어 보상 코인")]
        [SerializeField] private int clearRewardCoin = 50;

        [Label("사망 시 튀는 힘")]
        [Tooltip("사망 순간 위로 튀어 오르는 속도")]
        [SerializeField] private float deathBounceForce = 6f;

        [Label("목표 진행도 변경")]
        [SerializeField] private StringEventChannel onGoalProgressChanged;

        [Label("스테이지 클리어")]
        [SerializeField] private VoidEventChannel onStageCleared;

        [Label("플레이어 사망")]
        [SerializeField] private VoidEventChannel onPlayerFailed;

        [Label("체크포인트 활성화")]
        [SerializeField] private VoidEventChannel onCheckpointActivated;

        private CheckpointState _checkpoint;
        private Checkpoint _activeCheckpoint;
        private Player _player;
        private bool _isRespawning;

        // 활성화된 체크포인트. null이면 시작 지점이 부활 지점이다 (기획 §25.1).
        public Checkpoint ActiveCheckpoint => _activeCheckpoint;

        // 현재 획득 수량과 클리어 여부 (기획 §21.2, §22.3)
        public int AcquiredGoalItemCount { get; private set; }
        public bool IsStageCleared { get; private set; }
        public string GoalProgressText => $"{AcquiredGoalItemCount} / {requiredGoalItemCount}";

        // SpawnPlayer가 참조를 남기지 않는 경우(씬에 수동 배치)까지 대비해 지연 해석한다.
        private Player PlayerRef => _player != null ? _player : (_player = FindAnyObjectByType<Player>());

        public string StageId => stageId;
        public Transform StartPosition => startPosition;
        public float StageMinX => stageMinX;
        public float StageMaxX => stageMaxX;
        public float StageMinY => stageMinY;
        public float StageMaxY => stageMaxY;
        public float StageFallLimitY => stageFallLimitY;
        public float BoundsPadding => boundsPadding;
        public float CameraZoom => cameraZoom;
        public float CameraVerticalOffset => cameraVerticalOffset;
        public bool LockCameraX => lockCameraX;
        public bool LockCameraY => lockCameraY;
        public CameraSettings CameraSettingsOverride => cameraSettingsOverride;
        public bool UseIntroCamera => useIntroCamera;
        public float IntroMaxSize => introMaxSize;

        public bool UseBoundaryWalls => useBoundaryWalls;
        // 벽 안쪽 면의 X 좌표. 플레이어는 여기까지 갈 수 있다.
        public float LeftWallX => ComputeWallX(true, wallMode, stageMinX, stageMaxX, wallOffsetFromBounds, leftWallX, rightWallX);
        public float RightWallX => ComputeWallX(false, wallMode, stageMinX, stageMaxX, wallOffsetFromBounds, leftWallX, rightWallX);

        public static float ComputeWallX(bool leftSide, BoundaryWallMode mode,
            float minX, float maxX, float offset, float explicitLeft, float explicitRight)
        {
            if (mode == BoundaryWallMode.Explicit) return leftSide ? explicitLeft : explicitRight;
            return leftSide ? minX - offset : maxX + offset;
        }
        public int TotalGoalItemCount => totalGoalItemCount;
        public int RequiredGoalItemCount => requiredGoalItemCount;
        public int ClearRewardCoin => clearRewardCoin;

        // 이번 스테이지에서 주운 코인 (클리어 화면 표시용, UI 기획서 §6.2)
        public int StageCoinEarned { get; private set; }

        private void Start()
        {
            // 이전 스테이지의 클리어 화면 등을 먼저 내린다 — 시작 연출 동안 남아 있으면 안 된다
            if (Core.GameManager.Instance != null) Core.GameManager.Instance.EnterStage();

            SpawnPlayer();
            CreateBoundaryWalls();
            WarnIfBoundsExcludeTiles();

            // 인트로 카메라가 켜져 있으면 연출이 끝난 뒤에 게임을 시작한다.
            // 연출 동안에는 Ready 상태로 남아 HUD가 가려지고 일시정지도 열리지 않는다.
            if (!StageIntroCamera.TryPlay(this, StartPlaying)) StartPlaying();

            onGoalProgressChanged?.Raise(GoalProgressText);

            // 시작 지점을 기본 체크포인트로 등록 (기획 §24.1)
            SaveCheckpoint(startPosition != null ? startPosition.position : Vector3.zero);
        }

        // 경계가 타일보다 좁으면 카메라가 맵 끝을 안 비추고 투명 벽이 갈 수 있는 곳을 막는다.
        // 조용히 잘려 보이기만 해서 원인을 찾기 어려우므로 실행 시점에 알린다.
        private void WarnIfBoundsExcludeTiles()
        {
            if (!StageTiles.TryGetWorldBounds(out var tiles)) return;

            if (tiles.min.x >= stageMinX && tiles.max.x <= stageMaxX &&
                tiles.min.y >= stageMinY && tiles.max.y <= stageMaxY) return;

            Debug.LogWarning($"[Game] '{stageId}'의 스테이지 경계가 타일 범위보다 좁습니다. " +
                $"타일 X({tiles.min.x:F1}~{tiles.max.x:F1}) Y({tiles.min.y:F1}~{tiles.max.y:F1}) / " +
                $"경계 X({stageMinX:F1}~{stageMaxX:F1}) Y({stageMinY:F1}~{stageMaxY:F1}). " +
                "Stage 오브젝트의 [경계 자동 계산]을 눌러주세요.");
        }

        // 스테이지 씬 진입 = 게임 시작. 에디터에서 씬 단독 Play 시에도 동일하게 동작한다.
        private void StartPlaying()
        {
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.State != Core.GameState.Playing)
                Core.GameManager.Instance.StartGame();
        }

        // 맵 이탈 감시 (기획 §23.1). 물리로 움직이는 위치라 FixedUpdate에서 본다.
        private void FixedUpdate()
        {
            if (IsStageCleared || _isRespawning) return;

            var player = PlayerRef;
            if (player == null) return;

            if (IsFallen(player.transform.position.y, stageFallLimitY))
                RespawnPlayer();
        }

        // 사망은 낙사뿐이다 (기획 §23.1). 좌우는 투명 벽이 막고, 상단(Y+)은 세로로 긴 스테이지를 위해 자유.
        public static bool IsFallen(float y, float fallLimitY)
        {
            return y < fallLimitY;
        }

        // 좌우에 투명 벽을 세워 플레이어가 화면 밖으로 나가지 못하게 한다.
        private void CreateBoundaryWalls()
        {
            if (!useBoundaryWalls) return;

            CreateWall("LeftBoundaryWall", LeftWallX, true);
            CreateWall("RightBoundaryWall", RightWallX, false);
        }

        // innerFaceX가 플레이어가 닿는 면이 되도록 두께 절반만큼 바깥으로 밀어 배치한다.
        private void CreateWall(string wallName, float innerFaceX, bool leftSide)
        {
            const float thickness = 1f;
            float bottom = stageFallLimitY;
            float top = stageMaxY + wallHeadroom;

            var wall = new GameObject(wallName);
            wall.AddComponent<BoundaryWall>(); // 맵 크기를 잴 때 제외하기 위한 표식
            wall.transform.SetParent(transform);
            wall.transform.position = new Vector3(
                innerFaceX + (leftSide ? -thickness * 0.5f : thickness * 0.5f),
                (bottom + top) * 0.5f, 0f);

            var box = wall.AddComponent<BoxCollider2D>();
            box.size = new Vector2(thickness, top - bottom);
            box.sharedMaterial = new PhysicsMaterial2D("BoundaryWall") { friction = 0f, bounciness = 0f };
        }

        // 체크포인트 활성화 (기획 §25.2). 한 번에 하나만 활성이며, 새로 활성화하면 이전 데이터를 덮어쓴다 (§25.6).
        public void ActivateCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint == null || _activeCheckpoint == checkpoint) return; // 이미 활성화된 체크포인트는 재저장하지 않는다

            if (_activeCheckpoint != null) _activeCheckpoint.SetActivated(false);
            _activeCheckpoint = checkpoint;
            checkpoint.SetActivated(true);

            SaveCheckpoint(checkpoint.transform.position);
            Sound.Play(SoundId.CheckPoint);
            onCheckpointActivated?.Raise();
        }

        // 현재 상태를 체크포인트로 저장한다 (기획 §25.3).
        public void SaveCheckpoint(Vector3 position)
        {
            var player = PlayerRef;
            PropertyData property = null;
            if (player != null)
            {
                var playerProperty = player.GetComponent<PlayerProperty>();
                // 플레이어가 Start() 안에서 생성되므로 이 시점에 Current가 아직 null일 수 있다.
                if (playerProperty != null)
                    property = playerProperty.Current != null ? playerProperty.Current : playerProperty.DefaultProperty;
            }

            var collected = new List<GoalItem>();
            foreach (var item in FindObjectsByType<GoalItem>(FindObjectsSortMode.None))
            {
                if (item.IsCollected) collected.Add(item);
            }

            // 성질 아이템도 같은 시점 상태를 저장한다 (기획 §11.5)
            var acquiredProperties = new List<PropertyItem>();
            foreach (var item in FindObjectsByType<PropertyItem>(FindObjectsSortMode.None))
            {
                if (item.IsAcquired) acquiredProperties.Add(item);
            }

            // 코인도 같은 시점 상태를 저장한다 (인벤토리 문서 §6.5)
            var collectedCoins = new List<CoinItem>();
            foreach (var coin in FindObjectsByType<CoinItem>(FindObjectsSortMode.None))
            {
                if (coin.IsCollected) collectedCoins.Add(coin);
            }

            _checkpoint = new CheckpointState(position, property,
                collected, acquiredProperties,
                collectedCoins, CurrencyWallet.Coin, StageCoinEarned);
        }

        // 맵 이탈 부활 (기획 §23.2, §24.3). 게임 오버가 아니므로 GameState는 Playing을 유지한다.
        public void RespawnPlayer()
        {
            if (_checkpoint == null || _isRespawning) return;

            var player = PlayerRef;
            if (player == null) return;

            StartCoroutine(RespawnRoutine(player));
        }

        // 사망 시 살짝 튀어 올랐다가 중력으로 떨어지며 사망 연출(Die 애니 또는 스케일 아웃)을 보여준 뒤
        // 복구하고, 부활 팝(0→1.3→1)으로 재등장한다.
        private IEnumerator RespawnRoutine(Player player)
        {
            _isRespawning = true;

            player.SetDisabled(true); // 입력·자동 바운스 정지 + 속도 0
            // SetDisabled가 방금 0으로 만든 속도를 위로 튕겨 덮어쓴다.
            // 중력은 Disabled 상태에서도 계속 적용되므로(PlayerBounce) 이후엔 자연히 포물선으로 떨어진다.
            player.Body.linearVelocity = new Vector2(0f, deathBounceForce);
            onPlayerFailed?.Raise();
            Sound.Play(SoundId.Dead);

            var view = player.GetComponent<PlayerSpineView>();
            float deathDuration = view != null ? view.PlayDeath() : 0f;
            if (deathDuration > 0f) yield return new WaitForSeconds(deathDuration);

            // 위치 복구. Rigidbody가 Interpolate라 transform만 옮기면 순간이동 잔상이 남는다.
            var body = player.Body;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            player.transform.position = _checkpoint.Position;
            body.position = _checkpoint.Position;

            // 부착·미끄러짐 같은 성질 전용 상태는 저장하지 않고 초기화한다 (기획 §12, §6.7)
            player.GetComponent<PlayerJellyAttach>()?.Release();
            player.GetComponent<PlayerIceSlide>()?.Exit();  

            // 저장된 성질 복구 (기획 §25.5)
            var playerProperty = player.GetComponent<PlayerProperty>();
            if (playerProperty != null) playerProperty.Restore(_checkpoint.Property);

            // 성질 아이템이 접촉 즉시 획득으로 바뀌면서 E 상호작용 초기화는 필요 없어졌다.

            // RestoreItems가 목표 아이템·성질 아이템·일회성 발판을 함께 되돌린다.
            RestoreItems();
            RestoreCoins();

            // 기본은 즉시 이동. 끄면 맵을 가로질러 부드럽게 따라간다 (CameraSettings.snapOnRespawn)
            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (follow != null && follow.SnapOnRespawn) follow.Init(player.transform, this);

            if (view != null) view.PlayRespawnPop(); // 스케일 0 → 1.3 → 1 재등장 연출

            player.SetDisabled(false); // 조작·자동 바운스 재개
            _isRespawning = false;
        }

        // 부활 시 스테이지 요소를 되돌린다.
        // 아이템은 체크포인트 저장 시점 기준으로 (목표 아이템 §2.4, 성질 아이템 §11.5),
        // 일회성 발판은 체크포인트와 무관하게 전부 초기 활성 상태로 복구한다 —
        // 밟아 없앤 발판이 그대로 남으면 부활 지점에서 진행이 막힐 수 있다.
        private void RestoreItems()
        {
            foreach (var item in FindObjectsByType<GoalItem>(FindObjectsSortMode.None))
            {
                if (!_checkpoint.CollectedGoals.Contains(item)) item.Restore();
            }

            foreach (var item in FindObjectsByType<PropertyItem>(FindObjectsSortMode.None))
            {
                if (!_checkpoint.AcquiredPropertyItems.Contains(item)) item.Restore();
            }

            foreach (var platform in FindObjectsByType<DisposablePlatform>(FindObjectsSortMode.None))
            {
                platform.ResetPlatform();
            }

            AcquiredGoalItemCount = _checkpoint.AcquiredGoalItemCount;
            onGoalProgressChanged?.Raise(GoalProgressText);
        }

        // 코인도 저장 시점 상태로 되돌린다 — 안 그러면 같은 코인을 반복 획득할 수 있다 (인벤토리 문서 §6.5)
        private void RestoreCoins()
        {
            foreach (var coin in FindObjectsByType<CoinItem>(FindObjectsSortMode.None))
            {
                if (!_checkpoint.CollectedCoins.Contains(coin)) coin.Restore();
            }

            CurrencyWallet.RestoreTo(_checkpoint.CoinBalance);
            StageCoinEarned = _checkpoint.StageCoinEarned;
        }

        // 코인 획득 집계 (클리어 화면에서 "스테이지 획득 골드"로 표시)
        public void NotifyCoinCollected(int amount)
        {
            StageCoinEarned += amount;
        }

        // 목표 아이템이 획득될 때 호출된다 (기획 §21.3)
        public void NotifyGoalCollected()
        {
            if (IsStageCleared) return;

            AcquiredGoalItemCount++;
            onGoalProgressChanged?.Raise(GoalProgressText);

            if (AcquiredGoalItemCount >= requiredGoalItemCount) ClearStage();
        }

        // 클리어 처리 (기획 §22.2). IsStageCleared로 중복 처리를 막는다 (§22.3).
        private void ClearStage()
        {
            if (IsStageCleared) return;
            IsStageCleared = true;

            // 입력·자동 바운스 정지 + 속도/중력 제거 → 공중에 멈춘 채 띄워 둔다.
            // GameManager.StageClear가 timeScale을 0으로 두므로 물리도 함께 멈춘다.
            var player = PlayerRef;
            if (player != null)
            {
                player.SetDisabled(true);
                player.Body.gravityScale = 0f;
            }

            StageProgress.SetCleared(SceneManager.GetActiveScene().name);

            // 클리어 보상 지급 (인벤토리 문서 §6.2)
            CurrencyWallet.Add(clearRewardCoin);

            onStageCleared?.Raise();

            if (Core.GameManager.Instance != null) Core.GameManager.Instance.StageClear();
        }

        // Resources/Player.prefab을 시작 위치에 스폰하고 카메라를 연결한다.
        // 씬에 플레이어를 수동 배치할 필요가 없다.
        private void SpawnPlayer()
        {
            if (PlayerRef != null) return;

            var prefab = Resources.Load<GameObject>("Player");
            if (prefab == null)
            {
                Debug.LogWarning("[Game] Resources/Player.prefab이 없습니다. 삭제됐다면 git으로 복구하세요.");
                return;
            }

            var spawnAt = startPosition != null ? startPosition.position : Vector3.zero;
            var player = Instantiate(prefab, spawnAt, Quaternion.identity);
            _player = player.GetComponent<Player>();

            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<CameraFollow>();
                if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
                follow.Init(player.transform, this);
            }
        }

        public void SetBounds(float minX, float maxX, float minY, float maxY, float fallLimitY)
        {
            stageMinX = minX;
            stageMaxX = maxX;
            stageMinY = minY;
            stageMaxY = maxY;
            stageFallLimitY = fallLimitY;
        }

        public void SetStartPosition(Transform value) => startPosition = value;

        public void SetGoalCounts(int total, int required)
        {
            totalGoalItemCount = total;
            requiredGoalItemCount = required;
        }

        private void OnDrawGizmos()
        {
            // 카메라 경계
            Gizmos.color = Color.cyan;
            var center = new Vector3((stageMinX + stageMaxX) * 0.5f, (stageMinY + stageMaxY) * 0.5f);
            var size = new Vector3(stageMaxX - stageMinX, stageMaxY - stageMinY);
            Gizmos.DrawWireCube(center, size);

            // 투명 벽 (플레이어가 닿는 면)
            if (useBoundaryWalls)
            {
                Gizmos.color = Color.magenta;
                float wallTop = stageMaxY + wallHeadroom;
                Gizmos.DrawLine(new Vector3(LeftWallX, stageFallLimitY), new Vector3(LeftWallX, wallTop));
                Gizmos.DrawLine(new Vector3(RightWallX, stageFallLimitY), new Vector3(RightWallX, wallTop));
            }

            // 낙사선
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(stageMinX - 2f, stageFallLimitY),
                new Vector3(stageMaxX + 2f, stageFallLimitY));

            // 시작 위치
            if (startPosition != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(startPosition.position, 0.5f);
            }
        }
    }
}
