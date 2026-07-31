using UnityEngine;

namespace Game
{
    // 스테이지 기본 데이터 보유. 클리어/낙사 판정 로직은 Phase C에서 구현한다.
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
        }

        // Resources/Player.prefab을 시작 위치에 스폰하고 카메라를 연결한다.
        // 씬에 플레이어를 수동 배치할 필요가 없다.
        private void SpawnPlayer()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Player>() != null) return;

            var prefab = Resources.Load<GameObject>("Player");
            if (prefab == null)
            {
                Debug.LogWarning("[Game] Resources/Player.prefab이 없습니다. Game > Setup Stage Tooling을 실행하세요.");
                return;
            }

            var spawnAt = startPosition != null ? startPosition.position : Vector3.zero;
            var player = Instantiate(prefab, spawnAt, Quaternion.identity);

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
