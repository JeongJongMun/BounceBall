using UnityEngine;

namespace Game
{
    // 슈퍼 점프 발판 (기믹 문서 §3.2). 이 발판에서 자동 점프하면 일반 점프값에 배율을 곱한다.
    // 성질과 무관하게 모든 플레이어에게 동일한 결과를 준다 — 실제 적용은 PlayerBounce가 한다.
    [RequireComponent(typeof(Collider2D))]
    public class SuperJumpPlatform : MonoBehaviour
    {
        [Tooltip("일반 점프 대비 배율")]
        [SerializeField] private float jumpMultiplier = 1.8f;
        [Tooltip("끄면 튕겨 오를 때 수평 속도를 버린다")]
        [SerializeField] private bool preserveHorizontalVelocity = true;

        public float JumpMultiplier => jumpMultiplier;
        public bool PreserveHorizontalVelocity => preserveHorizontalVelocity;

        // 에디터 툴링/테스트에서 수치를 지정할 때 사용.
        public void SetData(float multiplier, bool preserveHorizontal = true)
        {
            jumpMultiplier = multiplier;
            preserveHorizontalVelocity = preserveHorizontal;
        }
    }
}
