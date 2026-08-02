using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    // A/D 좌우 이동 (기획 §8). 가속/감속/공중조작/방향전환 보정.
    [RequireComponent(typeof(Player))]
    public class PlayerMovement : MonoBehaviour
    {
        [Tooltip("끄면 키보드를 읽지 않는다 (테스트/컷씬용)")]
        [SerializeField] private bool readKeyboard = true;

        private Player _player;
        private PlayerIceSlide _slide;
        private PlayerDash _dash;
        private float _input;

        // 대시 방향 결정에 쓴다 (상점 소비형 문서 §4.2)
        public float CurrentInput => _input;

        public bool ReadKeyboard
        {
            get => readKeyboard;
            set => readKeyboard = value;
        }

        private void Awake()
        {
            _player = GetComponent<Player>();
            _slide = GetComponent<PlayerIceSlide>();
        }

        // 테스트 및 외부 제어용. readKeyboard가 꺼져 있을 때 사용.
        public void SetInput(float value) => _input = Mathf.Clamp(value, -1f, 1f);

        private void Update()
        {
            if (!readKeyboard || Keyboard.current == null) return;
            bool left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
            bool right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
            _input = left == right ? 0f : (left ? -1f : 1f); // 동시 입력 = 0 (기획 §8.2)
        }

        private void FixedUpdate()
        {
            if (_player.State == PlayerState.Disabled) return;
            // 부착 중 표면 이동은 PlayerJellyAttach가 담당한다 (기획 §5.4)
            if (_player.State == PlayerState.Attached) return;
            // 미끄러짐 중에는 얼음 전용 이동이 일반 좌우 이동을 대체한다 (기획 §6.1)
            if (_slide != null && _slide.IsSliding) return;
            // 대시 중에는 수평 속도를 대시가 소유한다 (상점 소비형 문서 §4.4-5)
            if (_dash == null) _dash = GetComponent<PlayerDash>();
            if (_dash != null && _dash.IsDashing) return;

            // 일반 이동에서는 마지막 입력 방향이 바라보는 방향이 된다 (기획 §6.5)
            if (!Mathf.Approximately(_input, 0f)) _player.FacingDirection = Mathf.Sign(_input);

            var stats = _player.Stats;
            var body = _player.Body;
            float current = body.linearVelocity.x;
            float target = _input * stats.MoveSpeed;

            float accel;
            if (Mathf.Approximately(_input, 0f))
            {
                accel = stats.Deceleration;
            }
            else
            {
                accel = stats.Acceleration;
                // 반대 방향 입력 시 빠른 전환 (기획 §8.3 DirectionChangePower)
                if (!Mathf.Approximately(current, 0f) && Mathf.Sign(_input) != Mathf.Sign(current))
                    accel *= stats.DirectionChangePower;
            }

            // 공중에서는 AirControl 비율 적용
            if (_player.State == PlayerState.Airborne)
                accel *= stats.AirControl;

            float next = Mathf.MoveTowards(current, target, accel * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(next, body.linearVelocity.y);
        }
    }
}
