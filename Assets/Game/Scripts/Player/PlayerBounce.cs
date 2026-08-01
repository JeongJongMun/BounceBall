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
        private PlayerSpineView _view;
        private PlayerIceSlide _slide;
        private JumpDustEffect _dust;
        private float _lastBounceTime = -999f;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _view = GetComponent<PlayerSpineView>();
            _slide = GetComponent<PlayerIceSlide>();
            _dust = GetComponent<JumpDustEffect>();
        }

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

            // 사망 발판 중 표면 효과를 끈 것은 부착·미끄러짐 대신 일반 점프로 처리한다 (기믹 문서 §4.3, §4.4).
            // 여기 도달했다는 것은 이미 사망 면제 성질이라는 뜻이다 — 아니면 PlayerHazardContact가 먼저 잡는다.
            if (tile != null && !tile.AppliesSurfaceEffectFor(_player.PropertyType))
                interaction = PropertyInteractionType.NormalJump;

            // 부착 조합에서는 자동 점프만 하지 않는다 (기획 §8 Attach).
            // 속도 0·중력 해제·표면 밀착 같은 실제 부착 처리는 PlayerJellyAttach가 담당한다 —
            // 여기서 속도를 건드리면 부착 해제 후에도 낙하하지 못한다.
            if (interaction == PropertyInteractionType.Attach) return;

            // 얼음 타일에 착지하면 미끄러짐 진입, 다른 타일에 착지하면 해제 (기획 §6.1, §6.7)
            if (_slide != null)
            {
                if (interaction == PropertyInteractionType.Slide) _slide.Enter();
                else _slide.Exit();
            }

            // Slide 조합에서는 자동 점프하지 않는다 — 바닥에 붙어 미끄러진다 (기획 §2.3 "미끄러짐, 자동 점프 없음").
            // 수직 속도만 죽여 표면에 얹어두고, 수평 이동은 PlayerIceSlide가 맡는다.
            if (interaction == PropertyInteractionType.Slide)
            {
                var sliding = _player.Body.linearVelocity;
                _player.Body.linearVelocity = new Vector2(sliding.x, 0f);
                return;
            }

            float jumpForce = _player.Stats.GetJumpForce(interaction);

            var velocity = _player.Body.linearVelocity;

            // 슈퍼 점프 발판은 성질과 무관하게 같은 높이로 띄운다 (기믹 문서 §3.2).
            // 성질별로 갈린 jumpForce를 곱하지 않고 기본 점프력에 배율을 걸어 덮어쓴다 —
            // 곱하면 얼음/젤리가 각각 낮고 높게 튀어 스테이지 높이 설계가 성질에 따라 무너진다.
            var superJump = collision.gameObject.GetComponent<SuperJumpPlatform>();
            if (superJump != null)
            {
                jumpForce = _player.Stats.NormalJumpForce * superJump.JumpMultiplier;
                if (!superJump.PreserveHorizontalVelocity) velocity.x = 0f;
                superJump.PlayLaunch();
            }

            _player.Body.linearVelocity = new Vector2(velocity.x, jumpForce);
            _lastBounceTime = Time.time;
            _player.SetGrounded(false);
            if (_view != null) _view.PlayJump();
            // 슈퍼 점프 발판은 전용 먼지를 쓴다 (기믹 문서 §3.2)
            if (_dust != null)
            {
                if (superJump != null) _dust.PlaySuper();
                else _dust.Play();
            }

            // 슈퍼 점프는 전용 사운드로 대체한다 (사운드 기획: 기믹 SuperJump)
            Sound.Play(superJump != null ? SoundId.SuperJump : BounceSound(_player.PropertyType));
            onPlayerBounced?.Raise();
        }

        // 성질별 바운스 사운드 (사운드 기획: 플레이어)
        private static SoundId BounceSound(PlayerPropertyType property)
        {
            switch (property)
            {
                case PlayerPropertyType.Jelly: return SoundId.Bounce_Jelly;
                case PlayerPropertyType.Ice: return SoundId.Bounce_Ice;
                default: return SoundId.Bounce_Normal;
            }
        }
    }
}
