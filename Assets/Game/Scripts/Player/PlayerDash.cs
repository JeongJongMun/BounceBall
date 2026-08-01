using UnityEngine;

namespace Game
{
    // 대시 아이템의 실제 동작 (상점 소비형 문서 §4).
    // 지속시간 동안 수평 속도를 대시 속도로 고정하고, 수직 속도와 중력은 그대로 둔다 (§4.5).
    // 효과 에셋(DashEffect)이 필요할 때 붙여 주므로 프리팹에 직접 배치하지 않는다.
    [RequireComponent(typeof(Player))]
    public class PlayerDash : MonoBehaviour
    {
        private Player _player;
        private float _direction;
        private float _speed;
        private float _endTime;
        private bool _stopOnWall;
        private float _lastEndTime = float.NegativeInfinity;

        public bool IsDashing { get; private set; }

        private Player PlayerRef => _player != null ? _player : (_player = GetComponent<Player>());

        // 문서 §4.4 순서: 상태 확인 → 방향 결정 → 부착 해제 → 속도 적용.
        public bool TryDash(float speed, float duration, float reuseDelay, bool stopOnWall)
        {
            // 대시 중 추가 대시는 허용하지 않는다 (문서 §4.9)
            if (IsDashing) return false;
            if (Time.time - _lastEndTime < reuseDelay) return false;

            var player = PlayerRef;
            if (player.State == PlayerState.Disabled) return false;

            // 방향: 현재 입력 → 없으면 바라보는 방향 (문서 §4.2)
            var movement = GetComponent<PlayerMovement>();
            _direction = ResolveDirection(movement != null ? movement.CurrentInput : 0f, player.FacingDirection);

            // 젤리 부착 중이면 해제하고 대시한다 (문서 §4.6). 재부착 방지는 Release()가 처리.
            GetComponent<PlayerJellyAttach>()?.Release();

            _speed = speed;
            _endTime = Time.time + duration;
            _stopOnWall = stopOnWall;
            IsDashing = true;

            player.FacingDirection = _direction;
            ApplyDashVelocity();
            return true;
        }

        // 입력이 있으면 입력 방향, 없거나 동시 입력(합 0)이면 바라보는 방향 (문서 §4.2).
        public static float ResolveDirection(float input, float facingDirection)
        {
            if (!Mathf.Approximately(input, 0f)) return Mathf.Sign(input);
            return facingDirection >= 0f ? 1f : -1f;
        }

        private void FixedUpdate()
        {
            if (!IsDashing) return;

            // 사망·클리어 연출은 Disabled로 들어온다 — 즉시 종료 (문서 §4.8)
            if (PlayerRef.State == PlayerState.Disabled) { End(false); return; }

            if (Time.time >= _endTime) { End(false); return; }

            // 매 물리 스텝 수평 속도를 고정한다. 수직은 중력에 맡긴다 (문서 §4.5)
            ApplyDashVelocity();
        }

        private void ApplyDashVelocity()
        {
            var body = PlayerRef.Body;
            body.linearVelocity = new Vector2(_direction * _speed, body.linearVelocity.y);
        }

        private void OnCollisionEnter2D(Collision2D collision) => CheckWall(collision);
        private void OnCollisionStay2D(Collision2D collision) => CheckWall(collision);

        // 진행 방향을 막는 면(법선이 대시 반대 방향)에 닿으면 종료한다 (문서 §4.8)
        private void CheckWall(Collision2D collision)
        {
            if (!IsDashing || !_stopOnWall) return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (!IsBlockingWall(collision.GetContact(i).normal, _direction)) continue;
                End(true);
                return;
            }
        }

        public static bool IsBlockingWall(Vector2 normal, float dashDirection)
        {
            return normal.x * dashDirection < -0.5f;
        }

        // 벽 충돌 종료면 수평 속도를 끊는다. 시간 만료면 현재 속도를 유지한 채
        // 일반 이동으로 복귀한다 — 미끄러짐이 이 속도를 이어받는다 (문서 §4.7)
        private void End(bool hitWall)
        {
            IsDashing = false;
            _lastEndTime = Time.time;

            if (hitWall)
            {
                var body = PlayerRef.Body;
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
        }
    }
}
