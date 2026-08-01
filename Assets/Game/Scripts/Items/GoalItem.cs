using System.Collections;
using Core.Events;
using UnityEngine;

namespace Game
{
    // 목표 아이템 (기획 §21). 접촉 즉시 자동 획득하며, 한 번만 획득할 수 있다.
    // 아이템이 사라지고 수량이 오르는 시점은 혀 끝이 닿는 순간이다.
    [RequireComponent(typeof(Collider2D))]
    public class GoalItem : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel onCollected;

        private Collider2D _collider;
        private SpriteRenderer _renderer;
        private StageController _stage;
        private Coroutine _pendingGrab;

        // 팔레트 브러시가 Gimmicks 아래에 스폰하므로 인스펙터 배선이 불가능하다 — 런타임에 지연 해석한다.
        private StageController StageRef => _stage != null ? _stage : (_stage = FindAnyObjectByType<StageController>());

        // Awake 타이밍에 의존하지 않도록 지연 해석한다 (EditMode 테스트에서는 Awake가 돌지 않는다).
        private Collider2D ColliderRef => _collider != null ? _collider : (_collider = GetComponent<Collider2D>());
        private SpriteRenderer RendererRef => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());

        public bool IsCollected { get; private set; }

        // 별도 입력 없이 접촉으로 획득 (기획 §20)
        private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

        // 다른 아이템을 먹는 중이라 거절됐을 수 있으므로, 범위 안에 머무는 동안 계속 재시도한다.
        private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        private void TryCollect(Collider2D other)
        {
            if (IsCollected) return;
            var player = other.GetComponent<Player>();
            if (player == null) return;

            // 다른 아이템을 먹는 중이면 연출이 끝날 때까지 기다린다
            if (player.IsEating) return;

            float grabDelay = other.GetComponent<PlayerSpineView>()?.PlayEat(transform.position) ?? 0f;

            // 재획득만 즉시 막고, 외형은 혀가 닿을 때까지 남겨둔다.
            IsCollected = true;
            if (ColliderRef != null) ColliderRef.enabled = false;

            if (grabDelay > 0f && isActiveAndEnabled)
                _pendingGrab = StartCoroutine(CompleteAfter(grabDelay));
            else
                Complete();
        }

        // 연출 없이 즉시 획득 처리한다 (테스트·외부 호출용).
        public void Collect()
        {
            if (IsCollected) return;
            IsCollected = true;
            if (ColliderRef != null) ColliderRef.enabled = false;
            Complete();
        }

        private IEnumerator CompleteAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            _pendingGrab = null;
            Complete();
        }

        private void Complete()
        {
            // SetActive가 아니라 컴포넌트만 끈다 (PropertyItem과 동일 — 코루틴/참조를 살려둔다)
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

            // 먹는 도중 죽었다면 대기 중인 집계는 취소한다 — 아이템이 되살아나므로 수량도 원래대로 둔다.
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
