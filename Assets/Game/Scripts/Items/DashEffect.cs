using UnityEngine;

namespace Game
{
    // 사용 즉시 좌우 한 방향으로 빠르게 이동한다 (상점 소비형 문서 §4). 값은 문서 §4.10.
    [CreateAssetMenu(menuName = "Game/Item Effect/대시", fileName = "Effect_Dash")]
    public class DashEffect : ItemEffect
    {
        [Label("대시 속도")]
        [Tooltip("대시 동안 고정되는 수평 속도. 일반 이동 속도(7)·미끄러짐 최고 속도와 별도로 관리한다")]
        [SerializeField] private float dashSpeed = 16f;

        [Label("지속 시간")]
        [Tooltip("대시 상태가 유지되는 시간(초)")]
        [SerializeField] private float dashDuration = 0.2f;

        [Label("재사용 간격")]
        [Tooltip("대시가 끝난 뒤 다음 대시까지의 최소 간격(초). 입력 중복 방지용 (문서 §4.9)")]
        [SerializeField] private float reuseDelay = 0.15f;

        [Label("벽 충돌 시 종료")]
        [Tooltip("진행 방향이 벽에 막히면 대시를 즉시 끝낸다 (문서 §4.8)")]
        [SerializeField] private bool stopOnWallCollision = true;

        public override bool TryApply(Player player)
        {
            var dash = player.GetComponent<PlayerDash>();
            if (dash == null) dash = player.gameObject.AddComponent<PlayerDash>();

            return dash.TryDash(dashSpeed, dashDuration, reuseDelay, stopOnWallCollision);
        }
    }
}
