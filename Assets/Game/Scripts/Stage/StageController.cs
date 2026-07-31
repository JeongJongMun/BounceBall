using System.Collections.Generic;
using Core.Events;
using UnityEngine;

namespace Game
{
    // 스테이지 기본 데이터 + 목표 아이템 집계, 클리어 판정, 맵 이탈 판정과 체크포인트 부활
    // (기획 §21~§24). 체크포인트 컴포넌트는 SaveCheckpoint를 호출해 이 계층에 얹힌다.
    public class StageController : MonoBehaviour
    {
        [Header("스테이지")]
        [SerializeField] private string stageId = "Stage01";
        [SerializeField] private Transform startPosition;

        [Header("카메라 경계")]
        [SerializeField] private float stageMinX = -10f;
        [SerializeField] private float stageMaxX = 10f;
        [SerializeField] private float stageMinY = -6f;
        [SerializeField] private float stageMaxY = 6f;

        [Header("낙사")]
        [SerializeField] private float stageFallLimitY = -8f;

        [Header("목표 아이템")]
        [SerializeField] private int totalGoalItemCount;
        [SerializeField] private int requiredGoalItemCount;

        [Header("이벤트")]
        [SerializeField] private StringEventChannel onGoalProgressChanged;
        [SerializeField] private VoidEventChannel onStageCleared;
        [SerializeField] private VoidEventChannel onPlayerFailed;
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
        public int TotalGoalItemCount => totalGoalItemCount;
        public int RequiredGoalItemCount => requiredGoalItemCount;

        private void Start()
        {
            SpawnPlayer();

            // 스테이지 씬 진입 = 게임 시작. 에디터에서 씬 단독 Play 시에도 동일하게 동작한다.
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.State != Core.GameState.Playing)
                Core.GameManager.Instance.StartGame();

            onGoalProgressChanged?.Raise(GoalProgressText);

            // 시작 지점을 기본 체크포인트로 등록 (기획 §24.1)
            SaveCheckpoint(startPosition != null ? startPosition.position : Vector3.zero);
        }

        // 맵 이탈 감시 (기획 §23.1). 물리로 움직이는 위치라 FixedUpdate에서 본다.
        private void FixedUpdate()
        {
            if (IsStageCleared || _isRespawning) return;

            var player = PlayerRef;
            if (player == null) return;

            if (IsOutOfBounds(player.transform.position, stageMinX, stageMaxX, stageFallLimitY))
                RespawnPlayer();
        }

        // 좌·우 경계와 낙사선만 판정한다. 상단(Y+)은 세로로 긴 스테이지를 위해 제외 (기획 §23.1).
        public static bool IsOutOfBounds(Vector2 position, float minX, float maxX, float fallLimitY)
        {
            return position.x < minX || position.x > maxX || position.y < fallLimitY;
        }

        // 체크포인트 활성화 (기획 §25.2). 한 번에 하나만 활성이며, 새로 활성화하면 이전 데이터를 덮어쓴다 (§25.6).
        public void ActivateCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint == null || _activeCheckpoint == checkpoint) return; // 이미 활성화된 체크포인트는 재저장하지 않는다

            if (_activeCheckpoint != null) _activeCheckpoint.SetActivated(false);
            _activeCheckpoint = checkpoint;
            checkpoint.SetActivated(true);

            SaveCheckpoint(checkpoint.transform.position);
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

            _checkpoint = new CheckpointState(position, property, collected);
        }

        // 맵 이탈 부활 (기획 §23.2, §24.3). 게임 오버가 아니므로 GameState는 Playing을 유지한다.
        public void RespawnPlayer()
        {
            if (_checkpoint == null || _isRespawning) return;

            var player = PlayerRef;
            if (player == null) return;

            _isRespawning = true;

            player.SetDisabled(true); // 입력·자동 바운스 정지 + 속도 0
            onPlayerFailed?.Raise();

            // 위치 복구. Rigidbody가 Interpolate라 transform만 옮기면 순간이동 잔상이 남는다.
            var body = player.Body;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            player.transform.position = _checkpoint.Position;
            body.position = _checkpoint.Position;

            // 저장된 성질 복구 (기획 §25.5)
            var playerProperty = player.GetComponent<PlayerProperty>();
            if (playerProperty != null) playerProperty.Restore(_checkpoint.Property);

            // 상호작용 대상·E UI 초기화 (기획 §24.3)
            var interaction = player.GetComponent<PlayerInteraction>();
            if (interaction != null) interaction.ClearRange();

            RestoreGoalItems();

            // 카메라를 즉시 이동시킨다. 안 하면 0.15초 동안 맵을 가로질러 스윕한다.
            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (follow != null) follow.Init(player.transform, this);

            player.SetDisabled(false); // 조작·자동 바운스 재개
            _isRespawning = false;
        }

        // 체크포인트 저장 이후 획득한 목표 아이템을 되살린다 (기획 §2.4)
        private void RestoreGoalItems()
        {
            foreach (var item in FindObjectsByType<GoalItem>(FindObjectsSortMode.None))
            {
                if (!_checkpoint.CollectedGoals.Contains(item)) item.Restore();
            }

            AcquiredGoalItemCount = _checkpoint.AcquiredGoalItemCount;
            onGoalProgressChanged?.Raise(GoalProgressText);
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

            // 입력·자동 바운스 정지 + 이동 속도 제거
            var player = PlayerRef;
            if (player != null) player.SetDisabled(true);

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
                Debug.LogWarning("[Game] Resources/Player.prefab이 없습니다. Game > Setup Stage Tooling을 실행하세요.");
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
