using System.Collections;
using UnityEngine;

namespace Game
{
    // 체크포인트 (기획 §25). 접촉 즉시 활성화되며, 가장 최근에 활성화된 것이 부활 지점이 된다.
    // 아이템과 마찬가지로 혀를 뻗어 깃발을 핥고, 혀가 닿는 순간 깃발이 올라간다.
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        private CheckpointSpineView _view;
        private StageController _stage;
        private Coroutine _pendingRaise;
        private float _raiseDelay;

        // 팔레트 브러시가 Gimmicks 아래에 스폰하므로 인스펙터 배선이 불가능하다 — 런타임에 지연 해석한다.
        private StageController StageRef => _stage != null ? _stage : (_stage = FindAnyObjectByType<StageController>());

        // Awake 타이밍에 의존하지 않도록 지연 해석한다 (EditMode 테스트에서는 Awake가 돌지 않는다).
        private CheckpointSpineView ViewRef => _view != null ? _view : (_view = GetComponentInChildren<CheckpointSpineView>(true));

        public bool IsActivated { get; private set; }

        // 별도 입력 없이 접촉 즉시 활성화 (기획 §25.2)
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<Player>();
            if (player == null) return;

            // 클리어·낙사 연출 중에는 활성화하지 않는다 (PropertyItem과 동일)
            if (player.State == PlayerState.Disabled) return;

            // 이미 활성화된 체크포인트는 다시 핥지 않는다. 다른 걸 먹는 중이어도 건너뛴다.
            _raiseDelay = !IsActivated && !player.IsEating
                ? other.GetComponent<PlayerSpineView>()?.PlayEat(transform.position) ?? 0f
                : 0f;

            // 저장은 연출과 무관하게 즉시 한다 — 핥는 도중 죽어도 이 지점이 남아야 한다.
            var stage = StageRef;
            if (stage != null) stage.ActivateCheckpoint(this);
        }

        // 활성 상태 전환은 StageController가 관리한다 (한 번에 하나만 활성, 기획 §25.6).
        public void SetActivated(bool activated)
        {
            IsActivated = activated;

            if (_pendingRaise != null)
            {
                StopCoroutine(_pendingRaise);
                _pendingRaise = null;
            }

            float delay = _raiseDelay;
            _raiseDelay = 0f;

            // 혀가 깃발에 닿는 순간 올라가도록 맞춘다.
            if (activated && delay > 0f && isActiveAndEnabled)
            {
                _pendingRaise = StartCoroutine(RaiseAfter(delay));
                return;
            }

            if (ViewRef != null) ViewRef.SetActivated(activated);
        }

        private IEnumerator RaiseAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            _pendingRaise = null;
            if (ViewRef != null) ViewRef.SetActivated(true);
        }
    }
}
