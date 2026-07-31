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
            // 부착 중에는 중력을 0으로 잡아두므로 동기화를 건너뛴다 (기획 §5.2)
            if (_player.State == PlayerState.Attached) return;

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
            if (_player.State == PlayerState.Attached) return; // 부착 중에는 바운스하지 않는다 (기획 §5.2)
            if (((1 << collision.gameObject.layer) & bounceableLayers) == 0) return;
            if (Time.time - _lastBounceTime < _player.Stats.BounceCooldown) return;

            // 하단 접촉인지 확인 (기획 §7.2: 벽/천장 충돌 시 바운스 금지)
            bool bottomContact = false;
            Vector2 contactPoint = default, contactNormal = default;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                if (contact.normal.y > 0.5f)
                {
                    bottomContact = true;
                    contactPoint = contact.point;
                    contactNormal = contact.normal;
                    break;
                }
            }
            if (!bottomContact) return;

            // 상승 중이면 착지가 아님 (기획 §7.2: 하강 중일 것)
            if (_player.Body.linearVelocity.y > _player.Stats.LandingVelocityThreshold) return;

            _player.SetGrounded(true);
            onPlayerLanded?.Raise();

            // 성질 × 타일 조합으로 결과를 조회한다 (기획 §3.1, §9)
            var tile = StageTiles.GetSpecialTileAt(contactPoint, contactNormal);
            var tileProperty = tile != null ? tile.TileProperty : TilePropertyType.Default;
            var interaction = PropertyInteractionTable.Resolve(_player.PropertyType, tileProperty);

            // 부착 조합에서는 자동 점프만 하지 않는다 (기획 §8 Attach).
            // 속도 0·중력 해제·표면 밀착 같은 실제 부착 처리는 PlayerJellyAttach가 담당한다 —
            // 여기서 속도를 건드리면 부착 해제 후에도 낙하하지 못한다.
            if (interaction == PropertyInteractionType.Attach) return;

            // Slide는 일반 점프력을 쓰고 수평 속도를 유지한다 (기획 §8 Slide).
            // 미끄러짐 이동 자체는 얼음 작업에서 구현한다.
            float jumpForce = _player.Stats.GetJumpForce(interaction);

            var velocity = _player.Body.linearVelocity;
            _player.Body.linearVelocity = new Vector2(velocity.x, jumpForce);
            _lastBounceTime = Time.time;
            _player.SetGrounded(false);
            onPlayerBounced?.Raise();
        }
    }
}
