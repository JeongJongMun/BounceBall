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
        private float _input;

        public bool ReadKeyboard
        {
            get => readKeyboard;
            set => readKeyboard = value;
        }

        private void Awake() => _player = GetComponent<Player>();

        // 테스트 및 외부 제어용. readKeyboard가 꺼져 있을 때 사용.
        public void SetInput(float value) => _input = Mathf.Clamp(value, -1f, 1f);

        private void Update()
        {
            if (!readKeyboard || Keyboard.current == null) return;
            bool a = Keyboard.current.aKey.isPressed;
            bool d = Keyboard.current.dKey.isPressed;
            _input = a == d ? 0f : (a ? -1f : 1f); // 동시 입력 = 0 (기획 §8.2)
        }

        private void FixedUpdate()
        {
            if (_player.State == PlayerState.Disabled) return;

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
