using DG.Tweening;
using Spine.Unity;
using UnityEngine;

namespace Game
{
    // 성질별 스파인 스켈레톤 전환과 애니메이션 재생.
    // 아트가 성질별 별도 스켈레톤(Default/Jelly/Ice)으로 제작되어 스킨이 아니라 오브젝트 전환을 쓴다.
    public class PlayerSpineView : MonoBehaviour
    {
        [System.Serializable]
        public class ViewSet
        {
            public SkeletonAnimation skeleton;
            public string idle;
            public string jump;
            public string eat;
            [Tooltip("부착 이동 루프(젤리). 없으면 빈 문자열")]
            public string crawl;
            [Tooltip("미끄러짐 이동 루프(얼음). 없으면 빈 문자열")]
            public string slide;
            [Tooltip("사망. 없으면 빈 문자열 (스케일 아웃으로 대체)")]
            public string die;
        }

        [SerializeField] private ViewSet defaultView;
        [SerializeField] private ViewSet jellyView;
        [SerializeField] private ViewSet iceView;

        private ViewSet _active;
        private Player _player;
        private bool _crawling;
        private bool _sliding;
        private Vector3 _defaultBaseScale = Vector3.one;
        private Vector3 _jellyBaseScale = Vector3.one;
        private Vector3 _iceBaseScale = Vector3.one;

        private Player PlayerRef => _player != null ? _player : (_player = GetComponent<Player>());

        // 원화가 왼쪽을 보고 그려져 있어 FacingDirection과 부호가 반대다.
        private float RenderFacing => -PlayerRef.FacingDirection;

        private void Awake()
        {
            // 부활 팝/스케일 아웃 연출이 원래 크기로 복원할 수 있도록 프리팹 스케일을 기억한다
            if (defaultView?.skeleton != null) _defaultBaseScale = defaultView.skeleton.transform.localScale;
            if (jellyView?.skeleton != null) _jellyBaseScale = jellyView.skeleton.transform.localScale;
            if (iceView?.skeleton != null) _iceBaseScale = iceView.skeleton.transform.localScale;
        }

        // 이동 방향에 맞춰 스켈레톤을 좌우 반전한다.
        // Transform 스케일이 아니라 Spine의 ScaleX를 쓰므로 콜라이더·자식 위치 계산에는 영향이 없다.
        private void Update()
        {
            if (_active?.skeleton?.Skeleton == null) return;
            _active.skeleton.Skeleton.ScaleX = RenderFacing;
        }

        public void SetProperty(PlayerPropertyType type, Color tint)
        {
            ViewSet next;
            switch (type)
            {
                case PlayerPropertyType.Jelly: next = jellyView; break;
                case PlayerPropertyType.Ice: next = iceView; break;
                default: next = defaultView; break;
            }

            SetViewActive(defaultView, next == defaultView);
            SetViewActive(jellyView, next == jellyView);
            SetViewActive(iceView, next == iceView);
            _active = next;

            if (_active?.skeleton != null && _active.skeleton.Skeleton != null)
            {
                _active.skeleton.Skeleton.SetColor(Color.white); // 세 성질 모두 고유 아트 — 틴트 불필요
                _crawling = false;
                _sliding = false;
                PlayIdle();
            }
        }

        // 자동 바운스 순간 재생, 끝나면 Idle 복귀
        public void PlayJump()
        {
            if (!HasState || _crawling || _sliding || string.IsNullOrEmpty(_active.jump)) return;
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

        // 얼음 미끄러짐 루프
        public void SetSliding(bool sliding)
        {
            _sliding = sliding;
            if (!HasState) return;
            if (sliding && !string.IsNullOrEmpty(_active.slide))
                _active.skeleton.AnimationState.SetAnimation(0, _active.slide, true);
            else
                PlayIdle();
        }

        // 사망 연출 재생 후 소요 시간을 돌려준다. 사망 애니가 없는 스켈레톤은 스케일 아웃으로 대체.
        public float PlayDeath()
        {
            if (!HasState) return 0f;

            if (!string.IsNullOrEmpty(_active.die))
            {
                var animation = _active.skeleton.Skeleton.Data.FindAnimation(_active.die);
                if (animation != null)
                {
                    _active.skeleton.AnimationState.SetAnimation(0, _active.die, false);
                    return animation.Duration;
                }
            }

            var t = _active.skeleton.transform;
            t.DOKill();
            t.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InQuad);
            return 0.15f;
        }

        // 부활 연출: 스케일 0 → 1.3배 → 원래 크기
        public void PlayRespawnPop()
        {
            // 사망 중 스케일 아웃됐을 수 있으니 세 뷰 모두 원복해 둔다
            RestoreScale(defaultView, _defaultBaseScale);
            RestoreScale(jellyView, _jellyBaseScale);
            RestoreScale(iceView, _iceBaseScale);

            if (_active?.skeleton == null) return;
            var t = _active.skeleton.transform;
            Vector3 baseScale;
            if (_active == jellyView) baseScale = _jellyBaseScale;
            else if (_active == iceView) baseScale = _iceBaseScale;
            else baseScale = _defaultBaseScale;

            t.DOKill();
            t.localScale = Vector3.zero;
            DOTween.Sequence()
                .Append(t.DOScale(baseScale * 1.3f, 0.25f).SetEase(Ease.OutQuad))
                .Append(t.DOScale(baseScale, 0.15f).SetEase(Ease.InOutQuad))
                .SetTarget(t);

            _crawling = false;
            _sliding = false;
            PlayIdle();
        }

        private static void RestoreScale(ViewSet set, Vector3 baseScale)
        {
            if (set?.skeleton == null) return;
            set.skeleton.transform.DOKill();
            set.skeleton.transform.localScale = baseScale;
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
