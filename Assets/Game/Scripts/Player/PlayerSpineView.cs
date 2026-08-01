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
            [Tooltip("아이템 방향으로 뻗는 혀 스프라이트. 뿌리(입 쪽)가 pivot")]
            public Sprite tongueSprite;
            [Tooltip("혀가 뻗어나가기 시작하는 입 위치. 플레이어 루트 기준 로컬 오프셋")]
            public Vector2 tongueOrigin = new(0f, 0.3f);
        }

        [SerializeField] private ViewSet defaultView;
        [SerializeField] private ViewSet jellyView;
        [SerializeField] private ViewSet iceView;

        [Header("먹기 연출")]
        [Tooltip("입이 벌어져 혀가 나갔다가 닫히며 되감기까지의 전체 시간. 입 애니메이션도 여기에 맞춰 배속된다")]
        [SerializeField] private float eatDuration = 0.4f;

        [Tooltip("연출이 끝난 뒤 다음 아이템을 먹기까지 더 기다리는 시간. 0이면 연출이 끝나는 즉시 먹을 수 있다")]
        [SerializeField] private float eatCooldown = 0f;

        [Tooltip("전체 시간 중 입이 벌어지는 앞 구간. 이 동안은 혀가 아직 안 나온다")]
        [Range(0f, 0.8f)] [SerializeField] private float mouthOpenRatio = 0.25f;
        [Tooltip("전체 시간 중 입이 닫히는 뒤 구간. 혀는 이 전에 다 들어와 있다")]
        [Range(0f, 0.8f)] [SerializeField] private float mouthCloseRatio = 0.25f;

        [Header("혀")]
        [SerializeField] private SpriteRenderer tongue;
        [Tooltip("혀 두께(세로 스케일). 가로는 아이템까지 거리에 맞춰 자동 계산한다")]
        [SerializeField] private float tongueThickness = 0.08f;
        [Tooltip("혀 구간 중 뻗어나가는 비율")]
        [Range(0f, 1f)] [SerializeField] private float tongueExtendRatio = 0.45f;
        [Tooltip("혀 구간 중 아이템에 닿은 채 머무는 비율")]
        [Range(0f, 1f)] [SerializeField] private float tongueHoldRatio = 0.1f;

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

        // 아이템 먹기 — 입 애니메이션(이동 애니메이션 위 트랙 1) + 아이템 방향으로 혀 뻗기.
        // 혀 끝이 아이템에 닿기까지 걸리는 시간을 돌려준다 (아이템이 사라지고 효과가 발동할 시점).
        public float PlayEat(Vector3 targetWorldPosition)
        {
            float grabDelay = PlayTongueAt(targetWorldPosition);
            PlayEatAnimation();
            LockEating();
            return grabDelay;
        }

        // 인벤토리에서 소비하는 아이템처럼 스테이지에 대상이 없는 경우 — 입 애니메이션만 재생한다.
        public void PlayEat()
        {
            // 스파인 상태와 무관하게 먹는 소리는 낸다 (사운드 기획: Lick)
            Sound.Play(SoundId.Lick);


            PlayEatAnimation();
            LockEating();
        }

        // 연출이 끝나고 쿨다운이 지날 때까지 다른 아이템을 먹지 못하게 잠근다.
        private void LockEating()
        {
            if (PlayerRef != null) PlayerRef.BeginEat(eatDuration + Mathf.Max(0f, eatCooldown));
        }

        // 입 애니메이션을 eatDuration 안에 다 재생되도록 배속한다.
        // 혀는 이 시간의 가운데 구간에서만 나갔다 들어오므로
        // "입 벌어짐 → 혀 나갔다 들어옴 → 입 닫힘" 순서로 읽힌다.
        private void PlayEatAnimation()
        {
            if (!HasState || string.IsNullOrEmpty(_active.eat)) return;

            var state = _active.skeleton.AnimationState;
            var entry = state.SetAnimation(1, _active.eat, false);

            float native = entry.Animation != null ? entry.Animation.Duration : 0f;
            if (native > 0.0001f && eatDuration > 0.01f) entry.TimeScale = native / eatDuration;

            state.AddEmptyAnimation(1, 0.05f, 0f);
        }

        // 입 위치에서 대상을 향해 혀를 뻗었다가 되감는다.
        // 입이 다 벌어질 때까지 기다렸다가 시작하고, 닫히기 전에 다 들어온다.
        // 혀 끝이 대상에 닿는 시점(대기 + 뻗기)을 돌려준다. 재생할 수 없으면 0.
        private float PlayTongueAt(Vector3 targetWorldPosition)
        {
            if (tongue == null || _active == null) return 0f;
            var sprite = _active.tongueSprite;
            if (sprite == null) return 0f;

            // 캐릭터가 반전돼 있으면 입 위치도 좌우로 같이 미러링한다
            var localOrigin = new Vector2(_active.tongueOrigin.x * RenderFacing, _active.tongueOrigin.y);
            var origin = transform.TransformPoint(localOrigin);
            Vector2 toTarget = targetWorldPosition - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.01f) return 0f;

            float nativeWidth = sprite.bounds.size.x; // 스프라이트 원본 너비(유닛). 뿌리(pivot)가 왼쪽 끝.
            if (nativeWidth < 0.0001f) return 0f;

            var t = tongue.transform;
            t.DOKill();
            tongue.sprite = sprite;
            tongue.enabled = false; // 입이 벌어진 뒤에 켠다
            t.position = origin;
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(0f, tongueThickness, 1f);

            // 앞뒤로 입이 벌어지고 닫히는 시간을 빼고, 남은 가운데 구간에서만 혀가 움직인다.
            float openTime = eatDuration * mouthOpenRatio;
            float window = Mathf.Max(0.02f, eatDuration * (1f - mouthOpenRatio - mouthCloseRatio));
            float extend = window * tongueExtendRatio;
            float hold = window * tongueHoldRatio;
            float retract = Mathf.Max(0.01f, window - extend - hold);

            var renderer = tongue;
            DOTween.Sequence()
                .AppendInterval(openTime) // 입이 벌어지는 동안 대기
                .AppendCallback(() => renderer.enabled = true)
                .Append(t.DOScaleX(distance / nativeWidth, extend).SetEase(Ease.OutQuad))
                .AppendInterval(hold)
                .Append(t.DOScaleX(0f, retract).SetEase(Ease.InQuad))
                .OnComplete(() => renderer.enabled = false)
                .SetTarget(t);

            return openTime + extend; // 혀 끝이 아이템에 닿는 시점
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

#if UNITY_EDITOR
        [Header("에디터")]
        [Tooltip("Scene 뷰에 혀 시작 위치를 표시한다")]
        [SerializeField] private bool showTongueOriginGizmo = true;

        // 성질별 입 위치(tongueOrigin)를 Scene 뷰에서 눈으로 맞출 수 있도록 표시한다.
        // 재생 중에는 활성 뷰만, 편집 중에는 세 성질을 모두 보여준다.
        private void OnDrawGizmos()
        {
            if (!showTongueOriginGizmo) return;

            if (Application.isPlaying && _active != null)
            {
                DrawTongueOrigin(_active, Color.yellow, RenderFacing);
                return;
            }

            // 편집 중에는 방향 반전이 없으므로 원본 오프셋 그대로 그린다.
            DrawTongueOrigin(defaultView, Color.green, 1f);
            DrawTongueOrigin(jellyView, Color.magenta, 1f);
            DrawTongueOrigin(iceView, Color.cyan, 1f);
        }

        private void DrawTongueOrigin(ViewSet set, Color color, float facing)
        {
            if (set == null) return;

            var world = transform.TransformPoint(new Vector3(set.tongueOrigin.x * facing, set.tongueOrigin.y, 0f));
            Gizmos.color = color;
            Gizmos.DrawSphere(world, 0.04f);

            // 혀가 뻗어나갈 방향을 짧은 선으로 같이 표시
            Gizmos.DrawLine(world, world + Vector3.right * facing * 0.25f);
        }
#endif
    }
}
