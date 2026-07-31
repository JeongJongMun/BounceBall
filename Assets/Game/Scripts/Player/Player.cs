using UnityEngine;

namespace Game
{
    public enum PlayerState { Airborne, GroundContact, Disabled, Attached }

    // 플레이어 상태 관리 + 컴포넌트 허브 (기획 §6 초기 범위: Airborne/GroundContact/Disabled).
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerStats))]
    public class Player : MonoBehaviour
    {
        public PlayerState State { get; private set; } = PlayerState.Airborne;

        private PlayerStats _stats;
        private Rigidbody2D _body;

        // GetComponent 시점을 Awake 타이밍에 의존하지 않도록 지연 해석한다 (EditMode 테스트에서 Awake 전에 접근할 수 있음).
        public PlayerStats Stats => _stats != null ? _stats : (_stats = GetComponent<PlayerStats>());
        public Rigidbody2D Body => _body != null ? _body : (_body = GetComponent<Rigidbody2D>());

        // 현재 성질. 성질 시스템(PlayerProperty)이 설정하며, 타일 상호작용 조회에 쓰인다 (기획 §10.5).
        public PlayerPropertyType PropertyType { get; set; } = PlayerPropertyType.Default;

        public void SetGrounded(bool grounded)
        {
            // 부착·조작 제한 중에는 접지 판정이 상태를 덮어쓰지 않는다.
            if (State == PlayerState.Disabled || State == PlayerState.Attached) return;
            State = grounded ? PlayerState.GroundContact : PlayerState.Airborne;
        }

        // 젤리 표면 부착 (기획 §5). 해제 시 공중 상태로 돌아간다 — 자동 점프는 발생하지 않는다.
        public void SetAttached(bool attached)
        {
            if (attached)
            {
                if (State == PlayerState.Disabled) return;
                State = PlayerState.Attached;
            }
            else if (State == PlayerState.Attached)
            {
                State = PlayerState.Airborne;
            }
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
