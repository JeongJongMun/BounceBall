using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    // 젤리 부착 (기획 §5). 젤리 성질일 때 젤리 타일의 바닥·벽·천장에 붙고,
    // A/D로 표면을 따라 이동하며, 모서리를 돌고, 조건이 되면 떨어진다.
    [RequireComponent(typeof(Player))]
    public class PlayerJellyAttach : MonoBehaviour
    {
        [SerializeField] private float jellyMoveSpeed = 4f;
        [Tooltip("표면이 끊기는 지점에서 이어진 젤리 면으로 넘어갈지 (기획 §5.5)")]
        [SerializeField] private bool canFollowJellyCorner = true;
        [Tooltip("표면 탐침에 쓸 레이어")]
        [SerializeField] private LayerMask surfaceLayers = ~0;
        [Tooltip("해제 직후 재부착을 막는 시간. 타일 끝에서 떨어질 때 모서리에 남은 접촉으로 다시 붙는 것을 방지한다")]
        [SerializeField] private float reattachCooldown = 0.15f;
        [Tooltip("끄면 키보드를 읽지 않는다 (테스트/컷씬용)")]
        [SerializeField] private bool readKeyboard = true;

        private Player _player;
        private CircleCollider2D _collider;
        private float _input;
        private float _cachedGravityScale = 1f;
        private float _lastReleaseTime = -999f;
        private readonly List<RaycastHit2D> _hitBuffer = new();

        public JellyAttachDirection AttachDirection { get; private set; } = JellyAttachDirection.None;
        public bool IsAttached => AttachDirection != JellyAttachDirection.None;

        public bool ReadKeyboard
        {
            get => readKeyboard;
            set => readKeyboard = value;
        }

        private Player PlayerRef => _player != null ? _player : (_player = GetComponent<Player>());
        private CircleCollider2D ColliderRef => _collider != null ? _collider : (_collider = GetComponent<CircleCollider2D>());

        private float Radius => ColliderRef != null ? ColliderRef.radius : 0.35f;

        // 테스트 및 외부 제어용. readKeyboard가 꺼져 있을 때 사용.
        public void SetInput(float value) => _input = Mathf.Clamp(value, -1f, 1f);

        private void Update()
        {
            if (!readKeyboard || Keyboard.current == null) return;
            bool a = Keyboard.current.aKey.isPressed;
            bool d = Keyboard.current.dKey.isPressed;
            _input = a == d ? 0f : (a ? -1f : 1f);
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryAttach(collision);
        private void OnCollisionStay2D(Collision2D collision) => TryAttach(collision);

        // 스치기만 해도 접촉 방향이 유효하면 부착한다 (기획 §5.1)
        private void TryAttach(Collision2D collision)
        {
            if (IsAttached) return;
            if (PlayerRef.State == PlayerState.Disabled) return;
            if (PlayerRef.PropertyType != PlayerPropertyType.Jelly) return;
            // 방금 표면을 벗어났다면 모서리에 남은 접촉으로 다시 붙지 않도록 잠시 무시한다
            if (Time.time - _lastReleaseTime < reattachCooldown) return;

            // 접촉이 여럿이면 법선이 가장 축에 가까운 것을 고른다 (기획 §5.1)
            float best = -1f;
            Vector2 bestNormal = Vector2.zero;
            bool found = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                var tile = StageTiles.GetSpecialTileAt(contact.point, contact.normal);
                if (tile == null || tile.TileProperty != TilePropertyType.Jelly) continue;

                float axisAlignment = Mathf.Max(Mathf.Abs(contact.normal.x), Mathf.Abs(contact.normal.y));
                if (axisAlignment <= best) continue;
                best = axisAlignment;
                bestNormal = contact.normal;
                found = true;
            }

            if (found) Attach(JellySurface.FromNormal(bestNormal));
        }

        // 기획 §5.2 순서: 속도 초기화 → 바운스 중지 → 중력 제한 → 표면 밀착 → 방향 저장 → 상태 활성화
        private void Attach(JellyAttachDirection direction)
        {
            if (direction == JellyAttachDirection.None) return;

            var body = PlayerRef.Body;
            if (!IsAttached) _cachedGravityScale = body.gravityScale;

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 0f;

            AttachDirection = direction;
            PlayerRef.SetAttached(true);
            GetComponent<PlayerSpineView>()?.SetCrawling(true);

            SnapToSurface();
        }

        // 기획 §5.6. 해제 순간에는 자동 점프를 발생시키지 않는다 — 다음 착지에서 정상 바운스한다.
        public void Release()
        {
            if (!IsAttached) return;

            AttachDirection = JellyAttachDirection.None;
            _lastReleaseTime = Time.time;
            PlayerRef.Body.gravityScale = _cachedGravityScale;
            PlayerRef.SetAttached(false);
            GetComponent<PlayerSpineView>()?.SetCrawling(false);
        }

        private void FixedUpdate()
        {
            if (!IsAttached) return;

            // 성질 변경(기획 §7.2) · 사망/부활(§12)은 즉시 해제
            if (PlayerRef.PropertyType != PlayerPropertyType.Jelly || PlayerRef.State == PlayerState.Disabled)
            {
                Release();
                return;
            }

            // 붙어 있는 면이 아직 젤리인지 확인. 사라졌거나 젤리가 아니면 해제 (기획 §5.6)
            if (!ProbeSurface(PlayerRef.Body.position, JellySurface.NormalOf(AttachDirection), out var hit))
            {
                Release();
                return;
            }

            SnapTo(hit);
            MoveAlongSurface();
        }

        private void MoveAlongSurface()
        {
            var body = PlayerRef.Body;
            body.linearVelocity = Vector2.zero; // 부착 중에는 물리 속도가 아니라 위치로 움직인다

            var move = JellySurface.MoveDirection(AttachDirection, _input);
            if (move.sqrMagnitude < 0.0001f) return;

            float step = jellyMoveSpeed * Time.fixedDeltaTime;
            var normal = JellySurface.NormalOf(AttachDirection);
            var next = body.position + move * step;

            // 진행 방향이 젤리 면으로 막혀 있으면 오목 모서리 — 막은 면으로 올라탄다 (기획 §5.5)
            if (ProbeJelly(body.position, move, out _))
            {
                if (!canFollowJellyCorner) return; // 모서리 이동이 꺼져 있으면 그 자리에 머문다

                JellySurface.TurnConcaveCorner(normal, move, out var newNormal, out _);
                var newDirection = JellySurface.FromNormal(newNormal);

                if (ProbeSurface(body.position, JellySurface.NormalOf(newDirection), out var cornerHit))
                {
                    AttachDirection = newDirection;
                    SnapTo(cornerHit);
                }
                return;
            }

            // 앞에 이어지는 젤리 면이 없으면 타일의 끝 — 감아 돌지 않고 해제해 낙하시킨다 (기획 §5.6)
            if (!ProbeSurface(next, normal, out var aheadHit))
            {
                Release();
                // 부착 이동은 속도가 아니라 위치로 옮기므로 해제 시 속도가 0이다.
                // 그대로 두면 타일 모서리에 얹혀 멈추므로 진행하던 속도를 넘겨 자연스럽게 떨어지게 한다.
                body.linearVelocity = move * jellyMoveSpeed;
                return;
            }

            body.position = next;
            SnapTo(aheadHit);
        }

        // 중심에서 법선 반대 방향(표면 쪽)으로 쏴서 붙어 있을 면을 찾는다.
        private bool ProbeSurface(Vector2 origin, Vector2 normal, out RaycastHit2D hit)
            => ProbeJelly(origin, -normal, out hit);

        // origin에서 direction 방향으로 젤리 표면을 찾는다.
        // 레이가 플레이어 중심에서 출발해 자기 콜라이더에 먼저 걸리므로(queriesStartInColliders 기본 true)
        // 자기 자신은 반드시 건너뛴다.
        private bool ProbeJelly(Vector2 origin, Vector2 direction, out RaycastHit2D hit)
        {
            hit = default;
            if (direction.sqrMagnitude < 0.0001f) return false;

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(surfaceLayers);

            int count = Physics2D.Raycast(origin, direction.normalized, filter, _hitBuffer, Radius + 0.2f);
            for (int i = 0; i < count; i++)
            {
                var candidate = _hitBuffer[i];
                if (candidate.collider == null) continue;
                if (candidate.collider.gameObject == gameObject) continue;
                if (!IsJellyAt(candidate)) continue;

                hit = candidate;
                return true;
            }
            return false;
        }

        private static bool IsJellyAt(RaycastHit2D hit)
        {
            var tile = StageTiles.GetSpecialTileAt(hit.point, hit.normal);
            return tile != null && tile.TileProperty == TilePropertyType.Jelly;
        }

        // 표면에서 떨어지지 않도록 위치를 지속 보정한다 (기획 §5.3)
        private void SnapToSurface()
        {
            if (ProbeSurface(PlayerRef.Body.position, JellySurface.NormalOf(AttachDirection), out var hit))
                SnapTo(hit);
        }

        private void SnapTo(RaycastHit2D hit)
        {
            var body = PlayerRef.Body;
            var target = hit.point + hit.normal * Radius;
            body.position = target;
            transform.position = target;
        }
    }
}
