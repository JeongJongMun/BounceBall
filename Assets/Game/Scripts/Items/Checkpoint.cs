using UnityEngine;

namespace Game
{
    // 체크포인트 (기획 §25). 접촉 즉시 활성화되며, 가장 최근에 활성화된 것이 부활 지점이 된다.
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Color inactiveColor = new(1f, 0.4f, 0.75f);
        [SerializeField] private Color activatedColor = Color.white;

        private SpriteRenderer _renderer;
        private StageController _stage;

        // 팔레트 브러시가 Gimmicks 아래에 스폰하므로 인스펙터 배선이 불가능하다 — 런타임에 지연 해석한다.
        private StageController StageRef => _stage != null ? _stage : (_stage = FindAnyObjectByType<StageController>());

        // Awake 타이밍에 의존하지 않도록 지연 해석한다 (EditMode 테스트에서는 Awake가 돌지 않는다).
        private SpriteRenderer RendererRef => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());

        public bool IsActivated { get; private set; }

        // 별도 입력 없이 접촉 즉시 활성화 (기획 §25.2)
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Player>() == null) return;

            var stage = StageRef;
            if (stage != null) stage.ActivateCheckpoint(this);
        }

        // 활성 상태 전환은 StageController가 관리한다 (한 번에 하나만 활성, 기획 §25.6).
        public void SetActivated(bool activated)
        {
            IsActivated = activated;
            if (RendererRef != null) RendererRef.color = activated ? activatedColor : inactiveColor;
        }
    }
}
