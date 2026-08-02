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

        // 바라보는 방향 (-1 / +1). 얼음 미끄러짐에서 입력이 없을 때의 이동 방향이 된다 (기획 §6.5, §10.5).
        // 일반 이동에서는 마지막 입력 방향, 미끄러짐 중에는 실제 수평 이동 방향으로 갱신한다.
        public float FacingDirection { get; set; } = 1f;

        private float _eatEndTime = -1f;

        // 아이템 먹는 연출이 끝날 때까지는 다른 아이템을 먹지 못한다 (기획 §11.3).
        // 이동·자동 바운스는 막지 않는다 — 연출 중에도 조작감을 유지한다.
        public bool IsEating => Time.time < _eatEndTime;

        public void BeginEat(float duration)
        {
            if (duration <= 0f) return;
            _eatEndTime = Time.time + duration;
        }

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
                // 연출 동안 바닥에서 속도 0으로 안착하면 Rigidbody가 sleep한다.
                // sleep 중엔 OnCollisionStay2D가 안 와서 자동 바운스가 멈추므로, 재개 시 깨운다.
                Body.WakeUp();
            }
        }
    }
}
