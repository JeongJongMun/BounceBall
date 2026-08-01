using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    // 성질 이펙트. 스프라이트를 무작위로 골라 파편처럼 튀겼다가 떨어뜨린다.
    //
    // 두 갈래로 쓴다:
    //   트레일 — 얼음 미끄러짐, 젤리 부착 이동처럼 표면을 훑는 동안 계속 흘린다. 성질별 세트를 쓴다.
    //   변신   — 성질 아이템을 먹는 순간 한 번에 터뜨린다 (Effect_Transform).
    [RequireComponent(typeof(Player))]
    public class PlayerPropertyEffect : MonoBehaviour
    {
        // 성질 하나가 쓰는 스프라이트 묶음. 인스펙터에서 폴더별로 넣는다.
        [Serializable]
        public class PropertySprites
        {
            public PlayerPropertyType property;
            public Sprite[] sprites;
        }

        [Header("성질별 트레일")]
        [Tooltip("미끄러짐·부착 이동에 쓰는 성질별 스프라이트. 점프는 JumpDustEffect가 따로 담당한다")]
        [SerializeField] private PropertySprites[] trailSets;

        [Header("성질 변경 (아이템 획득)")]
        [Tooltip("성질이 바뀌는 순간 터지는 스프라이트 (Effect_Transform)")]
        [SerializeField] private Sprite[] transformSprites;
        [SerializeField] private int transformBurstCount = 10;

        [Header("얼음 미끄러짐")]
        [Tooltip("느리게 갈 때의 방출 간격(초). 클수록 드문드문 나온다")]
        [SerializeField] private float slideIntervalSlow = 0.18f;
        [Tooltip("최고 속도일 때의 방출 간격(초)")]
        [SerializeField] private float slideIntervalFast = 0.03f;

        [Header("젤리 부착 이동")]
        [Tooltip("젤리는 기본적으로 적게 흘린다")]
        [SerializeField] private float crawlInterval = 0.22f;

        [Header("파편 거동")]
        [SerializeField] private Vector2 speedRange = new Vector2(2f, 5f);
        [Tooltip("위쪽을 기준으로 좌우로 퍼지는 전체 각도")]
        [SerializeField] private float spreadAngle = 80f;
        [SerializeField] private float gravity = 14f;
        [SerializeField] private Vector2 lifetimeRange = new Vector2(0.35f, 0.6f);
        [Tooltip("파편 하나의 목표 크기(월드 단위). 원본 스프라이트가 0.9~10 유닛으로 제각각이라 이 크기에 맞춰 줄인다")]
        [SerializeField] private float particleSize = 0.35f;
        [Tooltip("목표 크기 대비 무작위 배율")]
        [SerializeField] private Vector2 sizeVariation = new Vector2(0.7f, 1.3f);
        [SerializeField] private float maxSpin = 180f;
        [Tooltip("수명의 뒤쪽 몇 할을 페이드에 쓸지")]
        [SerializeField] private float fadeRatio = 0.6f;

        [Header("배치")]
        [Tooltip("접촉면에서 얼마나 띄워서 낼지. 트레일·점프 파편이 면에 파묻히지 않게 한다")]
        [SerializeField] private float surfaceOffset = 0.05f;
        [Tooltip("변신 파편이 나오는 플레이어 중심 기준 위치")]
        [SerializeField] private Vector2 transformOffset = Vector2.zero;
        [Tooltip("트레일·점프 파편의 렌더 순서 오프셋. 음수면 플레이어 뒤에 그려진다")]
        [SerializeField] private int trailSortingOffset = -1;
        [Tooltip("변신 파편의 렌더 순서 오프셋. 양수면 플레이어 앞에 그려진다")]
        [SerializeField] private int transformSortingOffset = 1;

        private Player _player;
        private PlayerIceSlide _slide;
        private PlayerJellyAttach _attach;
        private CircleCollider2D _body;
        private Transform _container;
        private ObjectPool<EffectParticle> _pool;
        private float _trailTimer;
        private int _sortingLayerId;
        private int _playerSortingOrder;

        // 테스트에서 확인할 수 있게 살아 있는 파편 수를 드러낸다.
        public int ActiveParticleCount { get; private set; }

        // 에디터 툴링/테스트에서 데이터를 지정할 때 사용.
        public void SetData(PropertySprites[] sets, float interval)
        {
            trailSets = sets;
            slideIntervalSlow = interval;
            slideIntervalFast = interval;
            crawlInterval = interval;
        }

        public void SetTransformData(Sprite[] sprites, int burstCount)
        {
            transformSprites = sprites;
            transformBurstCount = burstCount;
        }

        public void SetSlideIntervals(float slow, float fast)
        {
            slideIntervalSlow = slow;
            slideIntervalFast = fast;
        }

        private void Awake()
        {
            _player = GetComponent<Player>();
            _slide = GetComponent<PlayerIceSlide>();
            _attach = GetComponent<PlayerJellyAttach>();
            _body = GetComponent<CircleCollider2D>();

            // 플레이어와 같은 정렬 레이어에 그린다. 앞뒤는 종류별 오프셋으로 정한다.
            var playerRenderer = GetComponentInChildren<Renderer>();
            if (playerRenderer != null)
            {
                _sortingLayerId = playerRenderer.sortingLayerID;
                _playerSortingOrder = playerRenderer.sortingOrder;
            }
        }

        // 풀과 컨테이너는 필요할 때 만든다. Awake에서만 만들면 에디터에서 플레이 중 스크립트를 고칠 때
        // 도메인 리로드로 풀(직렬화 안 되는 관리 객체)만 날아가 NullReference가 난다.
        private ObjectPool<EffectParticle> PoolRef
        {
            get
            {
                if (_pool == null)
                    _pool = new ObjectPool<EffectParticle>(
                        CreateParticle,
                        particle => particle.gameObject.SetActive(true),
                        particle => particle.gameObject.SetActive(false),
                        particle => { if (particle != null) Destroy(particle.gameObject); },
                        false, 16, 128);
                return _pool;
            }
        }

        // 파편은 플레이어를 따라다니면 안 되므로 씬 루트에 따로 담는다.
        private Transform ContainerRef
            => _container != null ? _container : (_container = new GameObject("EffectParticles").transform);

        // 컨테이너는 이 컴포넌트가 만든 것이므로 같이 정리한다.
        private void OnDestroy()
        {
            if (_container != null) Destroy(_container.gameObject);
        }

        private void Update()
        {
            float interval = CurrentTrailInterval();
            if (interval <= 0f)
            {
                _trailTimer = 0f;
                return;
            }

            _trailTimer -= Time.deltaTime;
            if (_trailTimer > 0f) return;

            _trailTimer = interval;
            var normal = TrailNormal();
            Emit(TrailSpritesFor(CurrentProperty), 1, ContactPoint(normal), normal, TrailSortingOrder);
        }

        // 파편이 튀어나갈 기준 방향 = 플레이어가 닿아 있는 면의 법선.
        // 젤리는 벽·천장에도 붙으므로 항상 위쪽으로 뿌리면 면 안으로 파고든다.
        private Vector2 TrailNormal()
        {
            if (_attach != null && _attach.IsCrawling)
                return JellySurface.NormalOf(_attach.AttachDirection);
            return Vector2.up;
        }

        // 플레이어가 닿아 있는 면 위의 지점. 몸 중심이 아니라 여기서 파편이 나온다.
        private Vector3 ContactPoint(Vector2 normal)
        {
            float radius = _body != null ? _body.radius : 0.35f;
            return transform.position - (Vector3)(normal * Mathf.Max(0f, radius - surfaceOffset));
        }

        private int TrailSortingOrder => _playerSortingOrder + trailSortingOffset;

        // 흘릴 상태가 아니면 0을 돌려준다.
        // 성질마다 "표면을 훑는" 상태가 다르다 — 얼음은 미끄러짐, 젤리는 젤리 타일을 따라가는 부착 이동이다.
        private float CurrentTrailInterval()
        {
            if (_slide != null && _slide.IsSliding)
            {
                // 빠를수록 촘촘하게. 느릴 때는 드문드문 흘린다.
                float max = _slide.MaximumSlideSpeed;
                float t = max > 0f ? Mathf.Clamp01(Mathf.Abs(_slide.CurrentSlideSpeed) / max) : 0f;
                return Mathf.Lerp(slideIntervalSlow, slideIntervalFast, t);
            }

            // 젤리는 붙어만 있고 멈춰 있으면 흘리지 않는다.
            if (_attach != null && _attach.IsCrawling) return crawlInterval;

            return 0f;
        }

        // 성질 아이템을 먹어 성질이 바뀌는 순간 PropertyItem이 호출한다.
        // 접촉면이 아니라 플레이어 몸에서, 그리고 플레이어 앞쪽에 그린다.
        public void PlayTransform()
            => Emit(transformSprites, transformBurstCount,
                    transform.position + (Vector3)transformOffset, Vector2.up,
                    _playerSortingOrder + transformSortingOffset);

        private PlayerPropertyType CurrentProperty
            => _player != null ? _player.PropertyType : PlayerPropertyType.Default;

        private void Emit(Sprite[] sprites, int count, Vector3 origin, Vector2 normal, int sortingOrder)
        {
            if (sprites == null || sprites.Length == 0 || count <= 0) return;

            // 면에서 튀어나가는 방향을 기준으로 삼는다.
            float baseAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;

            for (int i = 0; i < count; i++)
            {
                var sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
                if (sprite == null) continue;

                float angle = baseAngle + UnityEngine.Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
                float radians = angle * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(speedRange.x, speedRange.y);

                var motion = new EffectParticle.Motion
                {
                    Velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * speed,
                    Gravity = gravity,
                    Lifetime = UnityEngine.Random.Range(lifetimeRange.x, lifetimeRange.y),
                    AngularSpeed = UnityEngine.Random.Range(-maxSpin, maxSpin),
                    FadeRatio = fadeRatio,
                };

                var particle = PoolRef.Get();
                ActiveParticleCount++;
                particle.Launch(sprite, origin, ScaleFor(sprite), sortingOrder, motion, Release);
            }
        }

        private void Release(EffectParticle particle)
        {
            ActiveParticleCount--;
            PoolRef.Release(particle);
        }

        // 원본 스프라이트의 긴 변을 particleSize에 맞춘다. 크기가 제각각인 리소스를 그대로 쓰면
        // 어떤 조각은 타일 열 칸을 덮어버린다.
        private float ScaleFor(Sprite sprite)
        {
            float variation = UnityEngine.Random.Range(sizeVariation.x, sizeVariation.y);
            float ppu = sprite.pixelsPerUnit;
            if (ppu <= 0f) return variation;

            float longestSide = Mathf.Max(sprite.rect.width, sprite.rect.height) / ppu;
            if (longestSide <= 0f) return variation;

            return particleSize / longestSide * variation;
        }

        private Sprite[] TrailSpritesFor(PlayerPropertyType property)
        {
            if (trailSets == null) return null;
            foreach (var set in trailSets)
                if (set != null && set.property == property) return set.sprites;
            return null;
        }

        private EffectParticle CreateParticle()
        {
            var go = new GameObject("EffectParticle");
            go.transform.SetParent(ContainerRef, false);

            // sortingOrder는 종류마다 다르므로 Launch에서 매번 지정한다.
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = _sortingLayerId;

            return go.AddComponent<EffectParticle>();
        }
    }
}
