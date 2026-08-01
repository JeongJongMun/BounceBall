using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    // 얼음 미끄러짐 (기획 §6). 얼음 성질로 얼음 타일 상단에 착지하면 진입하며,
    // 일반 좌우 이동을 대체하는 관성 기반 수평 이동을 적용한다.
    // 얼음 타일 위에서는 자동 점프가 발생하지 않고 바닥에 붙어 미끄러진다 (기획 §2.3) —
    // 점프를 막는 쪽은 PlayerBounce, 수평 거동은 이 컴포넌트가 맡는다.
    [RequireComponent(typeof(Player))]
    public class PlayerIceSlide : MonoBehaviour
    {
        [Tooltip("입력이 없어도 바라보는 방향으로 유지되는 최소 속도 (기획 §6.2)")]
        [SerializeField] private float minimumSlideSpeed = 3f;
        [Tooltip("입력 방향으로 붙는 가속도 (기획 §6.3)")]
        [SerializeField] private float slideAcceleration = 12f;
        [Tooltip("반대 방향 입력 시 기존 속도를 줄이는 가속도. 낮을수록 방향 전환이 굼뜨다 (기획 §6.4)")]
        [SerializeField] private float slideCounterAcceleration = 6f;
        [SerializeField] private float maximumSlideSpeed = 14f;
        [Tooltip("끄면 키보드를 읽지 않는다 (테스트/컷씬용)")]
        [SerializeField] private bool readKeyboard = true;

        private Player _player;
        private float _input;

        private Player PlayerRef => _player != null ? _player : (_player = GetComponent<Player>());

        public bool IsSliding { get; private set; }
        public float CurrentSlideSpeed => PlayerRef.Body.linearVelocity.x;
        public float MaximumSlideSpeed => maximumSlideSpeed;

        // 입력이 없으면 이 속도 아래로는 떨어지지 않는다 — 테스트에서 "가장 느린 상태"를 잡을 때 쓴다.
        public float MinimumSlideSpeedForTest => minimumSlideSpeed;

        // 입력에 따라 매 프레임 갱신되는 부호 있는 가속도. 목표 속도가 아니라
        // 이 값을 속도에 직접 누적한다 (기획 §6.3 SlideAcceleration, §6.4 SlideCounterAcceleration).
        public float CurrentAcceleration { get; private set; }

        public bool ReadKeyboard
        {
            get => readKeyboard;
            set => readKeyboard = value;
        }

        // 테스트 및 외부 제어용. readKeyboard가 꺼져 있을 때 사용.
        public void SetInput(float value) => _input = Mathf.Clamp(value, -1f, 1f);

        private void Update()
        {
            if (!readKeyboard || Keyboard.current == null) return;
            bool a = Keyboard.current.aKey.isPressed;
            bool d = Keyboard.current.dKey.isPressed;
            _input = a == d ? 0f : (a ? -1f : 1f); // 동시 입력 = 0 (기획 §8.2)
        }

        // 얼음 성질 × 얼음 타일 착지에서 PlayerBounce가 호출한다 (기획 §6.1).
        public void Enter()
        {
            if (PlayerRef.PropertyType != PlayerPropertyType.Ice) return;
            if (PlayerRef.State == PlayerState.Disabled) return;
            if (IsSliding) return;

            IsSliding = true;

            // 진입 시 기존 수평 속도가 있으면 그 방향과 속도를 우선 유지하고,
            // 없거나 최소 속도보다 낮으면 바라보는 방향으로 최소 속도를 준다 (기획 §6.2).
            var body = PlayerRef.Body;
            float vx = body.linearVelocity.x;
            if (Mathf.Abs(vx) < minimumSlideSpeed)
                body.linearVelocity = new Vector2(PlayerRef.FacingDirection * minimumSlideSpeed, body.linearVelocity.y);
            else
                PlayerRef.FacingDirection = Mathf.Sign(vx);

            GetComponent<PlayerSpineView>()?.SetSliding(true);
        }

        // 미끄러짐 해제 (기획 §6.7). 수평 속도는 즉시 0으로 만들지 않는다 —
        // 다음 타일의 일반 이동 감속 규칙에 넘긴다. 사망·부활의 속도 초기화는 StageController가 한다.
        public void Exit()
        {
            if (!IsSliding) return;
            IsSliding = false;
            GetComponent<PlayerSpineView>()?.SetSliding(false);
        }

        private void FixedUpdate()
        {
            if (!IsSliding) return;

            // 성질 변경(기획 §7.2) · 사망/부활(§12) · 부착 상태는 즉시 해제
            if (PlayerRef.PropertyType != PlayerPropertyType.Ice
                || PlayerRef.State == PlayerState.Disabled
                || PlayerRef.State == PlayerState.Attached)
            {
                Exit();
                return;
            }

            var body = PlayerRef.Body;
            float vx = body.linearVelocity.x;

            if (Mathf.Approximately(_input, 0f))
            {
                // 입력이 없으면 가속하지 않는다. 완전히 정지하지도 않는다 (기획 §6.2)
                CurrentAcceleration = 0f;
                if (Mathf.Abs(vx) < minimumSlideSpeed)
                    vx = PlayerRef.FacingDirection * minimumSlideSpeed;
            }
            else
            {
                // 입력 방향이 현재 속도와 반대면 감속용 가속도를 쓴다 (기획 §6.4).
                // 부호가 넘어가는 순간부터는 sign(vx)가 input과 같아져 자연히 가속 쪽으로 전환된다.
                bool counter = Mathf.Abs(vx) > 0.01f && Mathf.Sign(_input) != Mathf.Sign(vx);
                CurrentAcceleration = _input * (counter ? slideCounterAcceleration : slideAcceleration);
                vx += CurrentAcceleration * Time.fixedDeltaTime;
            }

            vx = Mathf.Clamp(vx, -maximumSlideSpeed, maximumSlideSpeed);
            body.linearVelocity = new Vector2(vx, body.linearVelocity.y);

            // 실제 수평 이동 방향이 바뀐 시점에만 바라보는 방향을 갱신한다 (기획 §6.5)
            if (Mathf.Abs(vx) > 0.01f) PlayerRef.FacingDirection = Mathf.Sign(vx);
        }
    }
}
