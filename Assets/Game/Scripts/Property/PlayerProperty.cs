using Core.Events;
using UnityEngine;

namespace Game
{
    // 현재 성질 보유/적용 (기획 §10). 이동/상호작용과 분리된 성질 전담 컴포넌트.
    [RequireComponent(typeof(Player))]
    public class PlayerProperty : MonoBehaviour
    {
        [SerializeField] private PropertyData defaultProperty;
        [SerializeField] private StringEventChannel onPropertyChanged;

        private Player _player;
        private SpriteRenderer _renderer;

        // GetComponent 시점을 Awake 타이밍에 의존하지 않도록 지연 해석한다 (EditMode 테스트에서 Awake 전에 Apply를 호출할 수 있음).
        private Player PlayerRef => _player != null ? _player : (_player = GetComponent<Player>());
        private SpriteRenderer RendererRef => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());

        public PropertyData Current { get; private set; }
        public PropertyData DefaultProperty => defaultProperty;

        // 에디터 툴링/테스트에서 시작 성질을 지정할 때 사용.
        public void SetDefaultProperty(PropertyData data) => defaultProperty = data;

        // 게임 시작 시 기본 성질로 시작 (기획 §18)
        private void Start()
        {
            if (defaultProperty != null) Apply(defaultProperty);
        }

        public void Apply(PropertyData data)
        {
            if (data == null) return;
            // 동일 성질 재획득 시 변화 없음 (기획 §7.3)
            if (Current != null && Current.PropertyType == data.PropertyType) return;

            Current = data;
            PlayerRef.PropertyType = data.PropertyType;

            if (RendererRef != null) RendererRef.color = data.CharacterColor;

            onPropertyChanged?.Raise(data.PropertyType.ToString());
        }

        // 체크포인트 복구용 강제 적용 (기획 §25.5). Apply는 같은 성질이면 no-op이라 별도 경로가 필요하다.
        public void Restore(PropertyData data)
        {
            Current = null;
            Apply(data != null ? data : defaultProperty);
        }
    }
}
