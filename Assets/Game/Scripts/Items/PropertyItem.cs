using System.Collections;
using Core.Events;
using UnityEngine;

namespace Game
{
    // 성질 아이템 (기획 §11). 지상·공중 구분 없이 접촉 즉시 획득하며, 별도 입력을 쓰지 않는다 (기획 §4).
    // 시간 기반 재생성은 하지 않는다 — 획득한 아이템은 체크포인트 부활·스테이지 재시작에서만 되살아난다 (기획 §11.5).
    [RequireComponent(typeof(Collider2D))]
    public class PropertyItem : MonoBehaviour
    {
        [SerializeField] private PropertyData propertyData;
        [SerializeField] private VoidEventChannel onAcquired;

        private Collider2D _collider;
        private SpriteRenderer _renderer;
        private Coroutine _pendingGrab;

        // Awake 타이밍에 의존하지 않도록 지연 해석한다 (EditMode 테스트에서는 Awake가 돌지 않는다).
        private Collider2D ColliderRef => _collider != null ? _collider : (_collider = GetComponent<Collider2D>());
        private SpriteRenderer RendererRef => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());

        // 기획 §11.6. IsActive는 현재 획득 가능한 상태, IsAcquired는 이번 플레이에서 획득했는지.
        public bool IsAcquired { get; private set; }
        public bool IsActive => !IsAcquired;
        public PropertyData PropertyData => propertyData;

        // 에디터 툴링/테스트에서 지급할 성질을 지정할 때 사용.
        public void SetData(PropertyData data) => propertyData = data;

        private void OnTriggerEnter2D(Collider2D other) => TryAcquire(other);

        // 다른 아이템을 먹는 중이라 거절됐을 수 있으므로, 범위 안에 머무는 동안 계속 재시도한다.
        private void OnTriggerStay2D(Collider2D other) => TryAcquire(other);

        private void TryAcquire(Collider2D other)
        {
            if (IsAcquired) return;
            var property = other.GetComponent<PlayerProperty>();
            if (property != null) Acquire(property);
        }

        // 기획 §11.4 순서: 접촉 → 활성 확인 → 연출 시작 → (혀가 닿는 순간) 성질 적용 → 획득 완료.
        // 접촉 즉시 사라지면 혀가 빈 곳을 찍으므로, 아이템은 혀 끝이 닿을 때 없어진다.
        public void Acquire(PlayerProperty playerProperty)
        {
            if (IsAcquired || propertyData == null || playerProperty == null) return;

            // 클리어·낙사 연출 중에는 획득하지 않는다 (기획 §11.3)
            var player = playerProperty.GetComponent<Player>();
            if (player != null && player.State == PlayerState.Disabled) return;

            // 다른 아이템을 먹는 중이면 연출이 끝날 때까지 기다린다
            // (기획 §11.3 "다른 성질 변경 처리가 실행 중이지 않다")
            if (player != null && player.IsEating) return;

            // 혀로 먹는 연출 — 동일 성질 재획득이라도 아이템은 먹으므로 항상 재생 (기획 §7.3)
            float grabDelay = playerProperty.GetComponent<PlayerSpineView>()?.PlayEat(transform.position) ?? 0f;

            // 재획득만 즉시 막고, 외형은 혀가 닿을 때까지 남겨둔다.
            IsAcquired = true;
            if (ColliderRef != null) ColliderRef.enabled = false;

            if (grabDelay > 0f && isActiveAndEnabled)
                _pendingGrab = StartCoroutine(CompleteAfter(grabDelay, playerProperty));
            else
                Complete(playerProperty);
        }

        private IEnumerator CompleteAfter(float delay, PlayerProperty playerProperty)
        {
            yield return new WaitForSeconds(delay);
            _pendingGrab = null;
            Complete(playerProperty);
        }

        private void Complete(PlayerProperty playerProperty)
        {
            if (RendererRef != null) RendererRef.enabled = false;
            if (playerProperty == null) return;

            // 동일 성질이면 Apply가 no-op이지만 아이템은 정상적으로 소모된다 (기획 §11.4, §7.3)
            playerProperty.Apply(propertyData);
            Sound.Play(SoundId.Property_Change);

            IsAcquired = true;
            if (ColliderRef != null) ColliderRef.enabled = false;
            if (RendererRef != null) RendererRef.enabled = false;

            onAcquired?.Raise();
        }

        // 체크포인트 부활 시 되살린다 (기획 §11.5). 저장 시점에 이미 획득돼 있던 것은 그대로 둔다.
        public void Restore()
        {
            if (!IsAcquired) return;
            IsAcquired = false;

            // 먹는 도중 죽었다면 대기 중인 성질 적용은 취소한다 —
            // 부활은 체크포인트에 저장된 성질로 되돌리므로 뒤늦게 덮어쓰면 안 된다.
            if (_pendingGrab != null)
            {
                StopCoroutine(_pendingGrab);
                _pendingGrab = null;
            }

            if (ColliderRef != null) ColliderRef.enabled = true;
            if (RendererRef != null) RendererRef.enabled = true;
        }
    }
}
