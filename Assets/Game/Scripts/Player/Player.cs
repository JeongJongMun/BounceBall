using UnityEngine;

namespace Game
{
    public enum PlayerState { Airborne, GroundContact, Disabled }

    // 플레이어 상태 관리 + 컴포넌트 허브 (기획 §6 초기 범위: Airborne/GroundContact/Disabled).
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerStats))]
    public class Player : MonoBehaviour
    {
        public PlayerState State { get; private set; } = PlayerState.Airborne;
        public PlayerStats Stats { get; private set; }
        public Rigidbody2D Body { get; private set; }

        private void Awake()
        {
            Stats = GetComponent<PlayerStats>();
            Body = GetComponent<Rigidbody2D>();
        }

        public void SetGrounded(bool grounded)
        {
            if (State == PlayerState.Disabled) return;
            State = grounded ? PlayerState.GroundContact : PlayerState.Airborne;
        }

        // 낙사·클리어 연출 등에서 조작 제한 (기획 §22.2, §23.2)
        public void SetDisabled(bool disabled)
        {
            if (disabled)
            {
                State = PlayerState.Disabled;
                Body.linearVelocity = Vector2.zero;
            }
            else
            {
                State = PlayerState.Airborne;
            }
        }
    }
}
