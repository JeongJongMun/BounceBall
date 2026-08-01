using Spine.Unity;
using UnityEngine;

namespace Game
{
    // 성질별 스파인 스켈레톤 전환과 애니메이션 재생.
    // 아트가 성질별 별도 스켈레톤(Default/Jelly)으로 제작되어 스킨이 아니라 오브젝트 전환을 쓴다.
    // 얼음 에셋은 아직 없어 Default 스켈레톤 + 틴트로 대체한다.
    public class PlayerSpineView : MonoBehaviour
    {
        [System.Serializable]
        public class ViewSet
        {
            public SkeletonAnimation skeleton;
            public string idle;
            public string jump;
            public string eat;
            [Tooltip("부착 이동 루프. 없으면 빈 문자열")]
            public string crawl;
        }

        [SerializeField] private ViewSet defaultView;
        [SerializeField] private ViewSet jellyView;

        private ViewSet _active;
        private bool _crawling;

        public void SetProperty(PlayerPropertyType type, Color tint)
        {
            var next = type == PlayerPropertyType.Jelly ? jellyView : defaultView;
            bool useTint = type == PlayerPropertyType.Ice; // 얼음 에셋 도착 전 임시 표현

            SetViewActive(defaultView, next == defaultView);
            SetViewActive(jellyView, next == jellyView);
            _active = next;

            if (_active?.skeleton != null && _active.skeleton.Skeleton != null)
            {
                var color = useTint ? tint : Color.white;
                _active.skeleton.Skeleton.SetColor(color);
                _crawling = false;
                PlayIdle();
            }
        }

        // 자동 바운스 순간 재생, 끝나면 Idle 복귀
        public void PlayJump()
        {
            if (!HasState || _crawling || string.IsNullOrEmpty(_active.jump)) return;
            var state = _active.skeleton.AnimationState;
            state.SetAnimation(0, _active.jump, false);
            state.AddAnimation(0, _active.idle, true, 0f);
        }

        // 아이템 먹기 (혀) — 이동 애니메이션 위에 겹쳐 재생 (트랙 1)
        public void PlayEat()
        {
            if (!HasState || string.IsNullOrEmpty(_active.eat)) return;
            var state = _active.skeleton.AnimationState;
            state.SetAnimation(1, _active.eat, false);
            state.AddEmptyAnimation(1, 0.2f, 0f);
        }

        // 젤리 부착 기어다니기 루프
        public void SetCrawling(bool crawling)
        {
            _crawling = crawling;
            if (!HasState) return;
            if (crawling && !string.IsNullOrEmpty(_active.crawl))
                _active.skeleton.AnimationState.SetAnimation(0, _active.crawl, true);
            else
                PlayIdle();
        }

        private void PlayIdle()
        {
            if (!HasState || string.IsNullOrEmpty(_active.idle)) return;
            _active.skeleton.AnimationState.SetAnimation(0, _active.idle, true);
        }

        private bool HasState =>
            _active?.skeleton != null && _active.skeleton.AnimationState != null;

        private static void SetViewActive(ViewSet set, bool active)
        {
            if (set?.skeleton != null) set.skeleton.gameObject.SetActive(active);
        }
    }
}
