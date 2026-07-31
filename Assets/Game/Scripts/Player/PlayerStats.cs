using UnityEngine;

namespace Game
{
    // 플레이어 기본 물리값과 성질 배율을 분리 관리한다 (기획 §9).
    // 최종값 = 기본값 × 배율. 배율은 Phase C에서 성질(PropertyData)이 설정한다.
    public class PlayerStats : MonoBehaviour
    {
        [Header("바운스 (기획 §7.4)")]
        [SerializeField] private float baseJumpForce = 12f;
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

        private float _jumpForceMultiplier = 1f;
        private float _moveSpeedMultiplier = 1f;
        private float _gravityMultiplier = 1f;
        private float _airControlMultiplier = 1f;

        public float JumpForce => baseJumpForce * _jumpForceMultiplier;
        public float MoveSpeed => moveSpeed * _moveSpeedMultiplier;
        public float GravityScale => gravityScale * _gravityMultiplier;
        public float AirControl => airControl * _airControlMultiplier;
        public float MaxFallSpeed => maxFallSpeed;
        public float LandingVelocityThreshold => landingVelocityThreshold;
        public float BounceCooldown => bounceCooldown;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float DirectionChangePower => directionChangePower;

        // Phase C: 성질 변경 시 호출. 기본값은 건드리지 않는다.
        public void SetMultipliers(float jump, float move, float gravity, float airControlMult)
        {
            _jumpForceMultiplier = jump;
            _moveSpeedMultiplier = move;
            _gravityMultiplier = gravity;
            _airControlMultiplier = airControlMult;
        }

        public void ResetMultipliers() => SetMultipliers(1f, 1f, 1f, 1f);
    }
}
