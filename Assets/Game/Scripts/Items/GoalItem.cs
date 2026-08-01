using Core.Events;
using UnityEngine;

namespace Game
{
    // 목표 아이템 (기획 §21). 접촉 즉시 자동 획득하며, 한 번만 획득할 수 있다.
    [RequireComponent(typeof(Collider2D))]
    public class GoalItem : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel onCollected;

        private Collider2D _collider;
        private SpriteRenderer _renderer;
        private StageController _stage;

        // 팔레트 브러시가 Gimmicks 아래에 스폰하므로 인스펙터 배선이 불가능하다 — 런타임에 지연 해석한다.
        private StageController StageRef => _stage != null ? _stage : (_stage = FindAnyObjectByType<StageController>());

        // Awake 타이밍에 의존하지 않도록 지연 해석한다 (EditMode 테스트에서는 Awake가 돌지 않는다).
        private Collider2D ColliderRef => _collider != null ? _collider : (_collider = GetComponent<Collider2D>());
        private SpriteRenderer RendererRef => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());

        public bool IsCollected { get; private set; }

        // 별도 입력 없이 접촉으로 획득 (기획 §20)
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsCollected) return;
            if (other.GetComponent<Player>() == null) return;
            other.GetComponent<PlayerSpineView>()?.PlayEat();
            Collect();
        }

        public void Collect()
        {
            if (IsCollected) return;
            IsCollected = true;

            // SetActive가 아니라 컴포넌트만 끈다 (PropertyItem과 동일 — 코루틴/참조를 살려둔다)
            if (ColliderRef != null) ColliderRef.enabled = false;
            if (RendererRef != null) RendererRef.enabled = false;

            Sound.Play(SoundId.Goal_Item);
            onCollected?.Raise();

            var stage = StageRef;
            if (stage != null) stage.NotifyGoalCollected();
        }

        // 체크포인트 부활 시 되살린다 (기획 §2.4 — 체크포인트 저장 이후 획득분은 다시 나타난다).
        public void Restore()
        {
            if (!IsCollected) return;
            IsCollected = false;

            if (ColliderRef != null) ColliderRef.enabled = true;
            if (RendererRef != null) RendererRef.enabled = true;
        }
    }
}
