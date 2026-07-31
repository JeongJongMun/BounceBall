using Core.Events;
using UnityEngine;

namespace Game
{
    // 자동 바운스 (기획 §7). 하단 착지 시 자동 점프, 중복 방지, 낙하 속도 제한.
    [RequireComponent(typeof(Player))]
    public class PlayerBounce : MonoBehaviour
    {
        [Tooltip("점프 가능한 발판 레이어")]
        [SerializeField] private LayerMask bounceableLayers = ~0;
        [SerializeField] private VoidEventChannel onPlayerBounced;
        [SerializeField] private VoidEventChannel onPlayerLanded;

        private Player _player;
        private float _lastBounceTime = -999f;

        private void Awake() => _player = GetComponent<Player>();

        private void Start()
        {
            _player.Body.gravityScale = _player.Stats.GravityScale;
        }

        private void FixedUpdate()
        {
            // 성질 변경으로 중력 배율이 바뀔 수 있어 매 프레임 동기화
            _player.Body.gravityScale = _player.Stats.GravityScale;

            // 최대 낙하 속도 제한 (기획 §7.4 MaxFallSpeed)
            var velocity = _player.Body.linearVelocity;
            if (velocity.y < -_player.Stats.MaxFallSpeed)
                _player.Body.linearVelocity = new Vector2(velocity.x, -_player.Stats.MaxFallSpeed);
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryBounce(collision);

        // 쿨타임 중 착지해 바닥에 머무는 경우 대비 (기획 §7.3)
        private void OnCollisionStay2D(Collision2D collision) => TryBounce(collision);

        private void OnCollisionExit2D(Collision2D collision)
        {
            _player.SetGrounded(false);
        }

        private void TryBounce(Collision2D collision)
        {
            if (_player.State == PlayerState.Disabled) return;
            if (((1 << collision.gameObject.layer) & bounceableLayers) == 0) return;
            if (Time.time - _lastBounceTime < _player.Stats.BounceCooldown) return;

            // 하단 접촉인지 확인 (기획 §7.2: 벽/천장 충돌 시 바운스 금지)
            bool bottomContact = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    bottomContact = true;
                    break;
                }
            }
            if (!bottomContact) return;

            // 상승 중이면 착지가 아님 (기획 §7.2: 하강 중일 것)
            if (_player.Body.linearVelocity.y > _player.Stats.LandingVelocityThreshold) return;

            _player.SetGrounded(true);
            onPlayerLanded?.Raise();

            var velocity = _player.Body.linearVelocity;
            _player.Body.linearVelocity = new Vector2(velocity.x, _player.Stats.JumpForce);
            _lastBounceTime = Time.time;
            _player.SetGrounded(false);
            onPlayerBounced?.Raise();
        }
    }
}
