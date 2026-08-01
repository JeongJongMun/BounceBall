using UnityEngine;

namespace Game
{
    // 플레이어 물리값 (기획 §4, §8.4).
    // 점프력은 성질별 배율이 아니라 공용 3값을 쓴다 — 어떤 값을 쓸지는
    // PropertyInteractionTable이 성질 × 타일 조합으로 결정한다.
    public class PlayerStats : MonoBehaviour
    {
        [Header("공용 점프 값 (기획 §4: 감소 < 일반 < 증가)")]
        [SerializeField] private float normalJumpForce = 12f;
        [SerializeField] private float lowJumpForce = 8f;
        [SerializeField] private float highJumpForce = 17f;

        [Header("바운스 (기획 §7.4)")]
        [SerializeField] private float gravityScale = 3f;
        [SerializeField] private float maxFallSpeed = 20f;
        [SerializeField] private float landingVelocityThreshold = 0.5f;
        [SerializeField] private float bounceCooldown = 0.1f;

        [Header("이동 (기획 §8.4)")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 40f;
        [SerializeField] private float airControl = 0.8f;
        [SerializeField] private float directionChangePower = 2f;

        public float NormalJumpForce => normalJumpForce;
        public float LowJumpForce => lowJumpForce;
        public float HighJumpForce => highJumpForce;
        public float MoveSpeed => moveSpeed;
        public float GravityScale => gravityScale;
        public float AirControl => airControl;
        public float MaxFallSpeed => maxFallSpeed;
        public float LandingVelocityThreshold => landingVelocityThreshold;
        public float BounceCooldown => bounceCooldown;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float DirectionChangePower => directionChangePower;

        // 상호작용 결과에 대응하는 공용 점프력.
        // Attach와 Slide는 자동 점프 자체가 없으므로 이 값을 쓰지 않는다 (기획 §2.2, §2.3).
        public float GetJumpForce(PropertyInteractionType interaction)
        {
            switch (interaction)
            {
                case PropertyInteractionType.LowJump: return lowJumpForce;
                case PropertyInteractionType.HighJump: return highJumpForce;
                default: return normalJumpForce;
            }
        }
    }
}
