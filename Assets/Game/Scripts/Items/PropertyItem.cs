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

        // Awake 타이밍에 의존하지 않도록 지연 해석한다 (EditMode 테스트에서는 Awake가 돌지 않는다).
        private Collider2D ColliderRef => _collider != null ? _collider : (_collider = GetComponent<Collider2D>());
        private SpriteRenderer RendererRef => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());

        // 기획 §11.6. IsActive는 현재 획득 가능한 상태, IsAcquired는 이번 플레이에서 획득했는지.
        public bool IsAcquired { get; private set; }
        public bool IsActive => !IsAcquired;
        public PropertyData PropertyData => propertyData;

        // 에디터 툴링/테스트에서 지급할 성질을 지정할 때 사용.
        public void SetData(PropertyData data) => propertyData = data;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var property = other.GetComponent<PlayerProperty>();
            if (property != null) Acquire(property);
        }

        // 기획 §11.4 순서: 접촉 → 활성 확인 → 성질 비교·적용 → 연출 → 충돌 차단 → 외형 숨김 → 획득 완료
        public void Acquire(PlayerProperty playerProperty)
        {
            if (IsAcquired || propertyData == null || playerProperty == null) return;

            // 클리어·낙사 연출 중에는 획득하지 않는다 (기획 §11.3)
            var player = playerProperty.GetComponent<Player>();
            if (player != null && player.State == PlayerState.Disabled) return;

            // 혀로 먹는 연출 — 동일 성질 재획득이라도 아이템은 먹으므로 항상 재생 (기획 §7.3)
            playerProperty.GetComponent<PlayerSpineView>()?.PlayEat();

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

            if (ColliderRef != null) ColliderRef.enabled = true;
            if (RendererRef != null) RendererRef.enabled = true;
        }
    }
}
