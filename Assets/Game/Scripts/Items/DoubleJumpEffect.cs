using UnityEngine;

namespace Game
{
    // 사용 즉시 위로 점프한다 (상점 소비형 문서 §3). 값은 문서 §3.9의 필요 데이터.
    [CreateAssetMenu(menuName = "Game/Item Effect/더블 점프", fileName = "Effect_DoubleJump")]
    public class DoubleJumpEffect : ItemEffect
    {
        [Label("점프력")]
        [Tooltip("일반 점프력(12)의 0.8~1.0배 권장 (문서 §3.7). 일반·슈퍼 점프와 별도로 관리한다")]
        [SerializeField] private float doubleJumpForce = 10.8f;

        [Label("중복 입력 방지 시간")]
        [Tooltip("한 번의 입력으로 여러 개가 소모되지 않게 하는 최소 간격(초). 재사용 대기시간이 아니다 (문서 §3.8)")]
        [SerializeField] private float useInterval = 0.2f;

        [Label("수평 속도 유지")]
        [Tooltip("끄면 점프 순간 수평 속도가 0이 된다. 미끄러짐 관성 유지를 위해 기본은 켬 (문서 §3.6)")]
        [SerializeField] private bool preserveHorizontalVelocity = true;

        public override bool TryApply(Player player)
        {
            // 능력 컴포넌트는 처음 쓸 때 붙인다 — 프리팹을 수정하지 않아도 된다
            var jump = player.GetComponent<PlayerDoubleJump>();
            if (jump == null) jump = player.gameObject.AddComponent<PlayerDoubleJump>();

            return jump.TryJump(doubleJumpForce, useInterval, preserveHorizontalVelocity);
        }
    }
}
