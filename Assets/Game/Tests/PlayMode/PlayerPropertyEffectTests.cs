using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    // 성질 이펙트: 얼음 미끄러짐·젤리 부착 이동에 흘리는 트레일과, 성질 아이템을 먹을 때의 변신 버스트.
    // 점프는 파티클이 아니라 JumpDustEffect의 프레임 애니메이션이라 여기서 다루지 않는다.
    public class PlayerPropertyEffectTests
    {
        private GameObject _go;
        private GameObject _gridGo;
        private Sprite[] _iceSprites;
        private Sprite[] _jellySprites;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            if (_gridGo != null) Object.Destroy(_gridGo);
            yield return null;
            StageTiles.InvalidateCache();
        }

        private static Sprite[] CreateSprites(string prefix, int count)
        {
            return CreateSprites(prefix, count, 32);
        }

        // 실제 리소스는 93px~1002px로 편차가 크다. 크기를 지정해 그 상황을 재현한다.
        private static Sprite[] CreateSprites(string prefix, int count, int pixelSize)
        {
            var texture = new Texture2D(pixelSize, pixelSize);
            var sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                sprites[i] = Sprite.Create(texture, new Rect(0, 0, pixelSize, pixelSize), Vector2.one * 0.5f, 100f);
                sprites[i].name = prefix + i;
            }
            return sprites;
        }

        // 컴포넌트가 Awake에서 풀을 잡으므로, 데이터를 넣은 뒤 활성화한다.
        private PlayerPropertyEffect CreatePlayer(out Player player, out PlayerIceSlide slide,
            PlayerPropertyEffect.PropertySprites[] sets, float interval)
        {
            _go = new GameObject("Player");
            _go.SetActive(false);
            _go.AddComponent<Rigidbody2D>().freezeRotation = true;
            _go.AddComponent<CircleCollider2D>().radius = 0.35f;
            _go.AddComponent<PlayerStats>();
            player = _go.AddComponent<Player>();
            slide = _go.AddComponent<PlayerIceSlide>();
            slide.ReadKeyboard = false;

            var effect = _go.AddComponent<PlayerPropertyEffect>();
            effect.SetData(sets, interval);
            _go.SetActive(true);
            return effect;
        }

        private PlayerPropertyEffect CreateWithIce(out Player player, out PlayerIceSlide slide, float interval = 0.05f)
        {
            _iceSprites = CreateSprites("Ice", 5);
            return CreatePlayer(out player, out slide, new[]
            {
                new PlayerPropertyEffect.PropertySprites { property = PlayerPropertyType.Ice, sprites = _iceSprites },
            }, interval);
        }

        // 미끄러짐을 실제로 켜서 트레일이 나오는 상태로 만든다.
        private static IEnumerator StartSliding(Player player, PlayerIceSlide slide)
        {
            player.PropertyType = PlayerPropertyType.Ice;
            player.Body.gravityScale = 0f;
            slide.Enter();
            Assert.IsTrue(slide.IsSliding, "미끄러짐 진입에 실패해 테스트가 성립하지 않습니다.");
            yield return new WaitForSeconds(0.2f);
        }

        private static List<SpriteRenderer> LiveParticles()
        {
            var live = new List<SpriteRenderer>();
            var container = GameObject.Find("EffectParticles");
            if (container == null) return live;
            foreach (var sr in container.GetComponentsInChildren<SpriteRenderer>(false)) live.Add(sr);
            return live;
        }

        [UnityTest]
        public IEnumerator 변신하면_설정한_개수만큼_한_번에_터진다()
        {
            Player player;
            PlayerIceSlide slide;
            var effect = CreateWithIce(out player, out slide);
            effect.SetTransformData(CreateSprites("Transform", 4), 7);
            yield return null;

            effect.PlayTransform();

            Assert.AreEqual(7, effect.ActiveParticleCount, "변신 한 번에 지정한 개수가 나와야 합니다.");
            Assert.AreEqual(7, LiveParticles().Count, "실제로 그려지는 파편 수가 다릅니다.");
        }

        [UnityTest]
        public IEnumerator 수명이_지나면_회수된다()
        {
            Player player;
            PlayerIceSlide slide;
            var effect = CreateWithIce(out player, out slide);
            effect.SetTransformData(CreateSprites("Transform", 4), 6);
            yield return null;

            effect.PlayTransform();
            Assert.Greater(effect.ActiveParticleCount, 0);

            // 기본 수명 최대치(0.6초)보다 넉넉히 기다린다.
            yield return new WaitForSeconds(1.2f);

            Assert.AreEqual(0, effect.ActiveParticleCount, "수명이 지난 파편이 회수되지 않았습니다.");
            Assert.AreEqual(0, LiveParticles().Count, "회수된 파편이 화면에 남아 있습니다.");
        }

        [UnityTest]
        public IEnumerator 미끄러질_때_현재_성질의_스프라이트만_쓴다()
        {
            Player player;
            PlayerIceSlide slide;
            var effect = CreateWithIce(out player, out slide);
            yield return null;

            yield return StartSliding(player, slide);

            var live = LiveParticles();
            Assert.IsNotEmpty(live, "미끄러지는데 파편이 나오지 않았습니다.");
            foreach (var sr in live)
                CollectionAssert.Contains(_iceSprites, sr.sprite,
                    "얼음 성질인데 다른 스프라이트가 나왔습니다: " + (sr.sprite != null ? sr.sprite.name : "null"));
        }

        // 파편은 플레이어를 따라다니면 안 된다 — 뿌려진 자리에 남아야 한다.
        [UnityTest]
        public IEnumerator 파편은_플레이어를_따라다니지_않는다()
        {
            Player player;
            PlayerIceSlide slide;
            var effect = CreateWithIce(out player, out slide);
            effect.SetTransformData(CreateSprites("Transform", 4), 5);
            yield return null;

            effect.PlayTransform();
            var particle = LiveParticles()[0].transform;
            float before = particle.position.x;

            _go.transform.position += new Vector3(10f, 0f, 0f);
            yield return null;

            Assert.Less(Mathf.Abs(particle.position.x - before), 1f,
                "플레이어가 이동하자 파편이 함께 끌려갔습니다.");
        }

        // 원본 리소스는 0.93~10.02 유닛으로 편차가 크다. 그대로 쓰면 어떤 조각이 화면을 덮어버리므로
        // 스프라이트 크기와 무관하게 목표 크기로 맞춰야 한다.
        [UnityTest]
        public IEnumerator 원본_크기와_무관하게_비슷한_크기로_나온다()
        {
            Player player;
            PlayerIceSlide slide;
            var effect = CreateWithIce(out player, out slide);
            yield return null;

            // 32px(0.32유닛)와 1000px(10유닛) — 실제 리소스의 양 극단
            effect.SetTransformData(CreateSprites("Small", 1, 32), 1);
            effect.PlayTransform();
            float smallSize = LiveParticles()[0].bounds.size.x;

            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, effect.ActiveParticleCount);

            effect.SetTransformData(CreateSprites("Huge", 1, 1000), 1);
            effect.PlayTransform();
            float hugeSize = LiveParticles()[0].bounds.size.x;

            // 원본은 31배 차이지만 결과는 크기 변동(0.7~1.3) 범위 안이어야 한다.
            Assert.Less(smallSize, 1f, "작은 스프라이트가 너무 큽니다: " + smallSize);
            Assert.Less(hugeSize, 1f, "큰 스프라이트가 목표 크기로 줄지 않았습니다: " + hugeSize);
            Assert.Less(Mathf.Max(smallSize, hugeSize) / Mathf.Max(0.001f, Mathf.Min(smallSize, hugeSize)), 3f,
                "원본 크기 차이가 결과까지 그대로 넘어왔습니다 (작은 것 " + smallSize + ", 큰 것 " + hugeSize + ").");
        }

        // 얼음은 빠를수록 많이 흘린다. 측정 창(0.3초)을 파편 최소 수명(0.35초)보다 짧게 잡아
        // 창 안에서는 아무것도 만료되지 않게 한다 — 그래야 살아 있는 수가 곧 그동안 나온 수가 된다.
        [UnityTest]
        public IEnumerator 얼음은_속도가_빠를수록_많이_나온다()
        {
            Player player;
            PlayerIceSlide slide;
            var effect = CreateWithIce(out player, out slide, 0.1f);
            effect.SetSlideIntervals(0.2f, 0.02f);
            yield return null;

            player.PropertyType = PlayerPropertyType.Ice;
            player.Body.gravityScale = 0f;
            slide.Enter();
            Assert.IsTrue(slide.IsSliding, "미끄러짐 진입에 실패해 테스트가 성립하지 않습니다.");

            int slow = 0;
            yield return Measure(effect, player, slide.MinimumSlideSpeedForTest, v => slow = v);

            // 두 번째 측정을 0에서 시작하려면 방출을 멈추고 남은 파편을 모두 흘려보내야 한다.
            slide.Exit();
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, effect.ActiveParticleCount, "다음 측정 전에 파편이 남아 있습니다.");

            slide.Enter();
            Assert.IsTrue(slide.IsSliding, "두 번째 측정을 위한 재진입에 실패했습니다.");

            int fast = 0;
            yield return Measure(effect, player, slide.MaximumSlideSpeed, v => fast = v);

            Assert.Greater(fast, slow,
                "빠르게 미끄러질 때 더 많이 나와야 합니다 (느림 " + slow + ", 빠름 " + fast + ").");
        }

        private static IEnumerator Measure(PlayerPropertyEffect effect, Player player, float speed, System.Action<int> onResult)
        {
            float deadline = Time.time + 0.3f;
            while (Time.time < deadline)
            {
                player.Body.linearVelocity = new Vector2(speed, 0f);
                yield return new WaitForFixedUpdate();
            }
            onResult(effect.ActiveParticleCount);
        }

        // 젤리의 "미끄러짐"은 얼음과 달리 젤리 타일 표면을 따라가는 부착 이동이다.
        // 붙어만 있고 멈춰 있으면 흘리지 않고, 실제로 기어갈 때만 나온다.
        [UnityTest]
        public IEnumerator 젤리는_표면을_기어갈_때_나온다()
        {
            _jellySprites = CreateSprites("Jelly", 2);

            _go = new GameObject("Player");
            _go.SetActive(false);
            _go.AddComponent<Rigidbody2D>().freezeRotation = true;
            _go.AddComponent<CircleCollider2D>().radius = 0.35f;
            _go.AddComponent<PlayerStats>();
            var player = _go.AddComponent<Player>();
            var attach = _go.AddComponent<PlayerJellyAttach>();
            attach.ReadKeyboard = false;
            var effect = _go.AddComponent<PlayerPropertyEffect>();
            effect.SetData(new[]
            {
                new PlayerPropertyEffect.PropertySprites { property = PlayerPropertyType.Jelly, sprites = _jellySprites },
            }, 0.05f);
            _go.SetActive(true);
            yield return null;

            player.PropertyType = PlayerPropertyType.Jelly;

            // 젤리 타일 한 줄을 깔고 그 위로 떨어뜨려 실제로 부착시킨다.
            BuildJellyFloor();
            _go.transform.position = new Vector3(0.5f, -0.6f, 0f);

            float deadline = Time.time + 3f;
            while (Time.time < deadline && !attach.IsAttached) yield return new WaitForFixedUpdate();
            Assert.IsTrue(attach.IsAttached, "젤리 타일에 부착하지 못해 테스트가 성립하지 않습니다.");

            // 붙어만 있고 입력이 없는 동안에는 흘리지 않아야 한다.
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, effect.ActiveParticleCount, "붙어서 멈춰 있는데 이펙트가 나왔습니다.");

            attach.SetInput(1f);
            yield return new WaitForSeconds(0.25f);

            Assert.IsTrue(attach.IsCrawling, "입력을 줬는데 기어가지 않습니다.");
            Assert.Greater(effect.ActiveParticleCount, 0, "젤리가 기어가는데 이펙트가 나오지 않았습니다.");
        }

        // 젤리 부착은 타일 성질을 조회해서 판정하므로 실제 타일맵이 필요하다.
        private void BuildJellyFloor()
        {
            _gridGo = new GameObject("Grid");
            _gridGo.AddComponent<UnityEngine.Grid>();
            var mapGo = new GameObject("Ground");
            mapGo.transform.SetParent(_gridGo.transform);
            var tilemap = mapGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            mapGo.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            mapGo.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>();

            var jelly = ScriptableObject.CreateInstance<SpecialTile>();
            jelly.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.Grid;
            jelly.SetTileProperty(TilePropertyType.Jelly);

            for (int x = -5; x <= 5; x++) tilemap.SetTile(new Vector3Int(x, -2, 0), jelly);
            StageTiles.InvalidateCache();
        }

        // 변신 이펙트는 플레이어 앞, 트레일은 뒤에 그린다.
        [UnityTest]
        public IEnumerator 변신은_플레이어_앞에_트레일은_뒤에_그린다()
        {
            Player player;
            PlayerIceSlide slide;
            var effect = CreateWithIce(out player, out slide);
            effect.SetTransformData(CreateSprites("Transform", 4), 4);
            yield return null;

            yield return StartSliding(player, slide);
            var trail = LiveParticles();
            Assert.IsNotEmpty(trail);
            int trailOrder = trail[0].sortingOrder;

            slide.Exit();
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, effect.ActiveParticleCount);

            effect.PlayTransform();
            int transformOrder = LiveParticles()[0].sortingOrder;

            Assert.Greater(transformOrder, trailOrder,
                "변신 파편이 트레일보다 앞에 그려져야 합니다 (변신 " + transformOrder + ", 트레일 " + trailOrder + ").");
        }

        // 트레일은 몸 중심이 아니라 발이 닿은 면에서 나온다.
        [UnityTest]
        public IEnumerator 트레일은_닿은_면에서_나온다()
        {
            Player player;
            PlayerIceSlide slide;
            var effect = CreateWithIce(out player, out slide);
            yield return null;

            _go.transform.position = new Vector3(0f, 3f, 0f);
            yield return StartSliding(player, slide);

            // 파편은 생성 직후부터 위로 날아오르므로, 갓 나온 것을 잡아야 생성 지점을 볼 수 있다.
            slide.Exit();
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, effect.ActiveParticleCount, "측정 전에 파편이 남아 있습니다.");

            slide.Enter();
            float deadline = Time.time + 2f;
            while (Time.time < deadline && effect.ActiveParticleCount == 0) yield return null;
            Assert.Greater(effect.ActiveParticleCount, 0, "미끄러지는데 파편이 나오지 않았습니다.");

            float radius = _go.GetComponent<CircleCollider2D>().radius;
            float below = _go.transform.position.y - LiveParticles()[0].transform.position.y;
            Assert.Greater(below, radius * 0.5f,
                "파편이 몸 중심 근처에서 나왔습니다 — 발밑 접촉면에서 나와야 합니다 (아래로 " + below + ").");
        }

        // 성질에 스프라이트가 없으면 아무것도 나오지 않아야 한다 (예외 없이).
        [UnityTest]
        public IEnumerator 스프라이트가_없는_성질에서는_나오지_않는다()
        {
            Player player;
            PlayerIceSlide slide;
            var effect = CreatePlayer(out player, out slide,
                new PlayerPropertyEffect.PropertySprites[0], 0.05f);
            yield return null;

            yield return StartSliding(player, slide);

            Assert.AreEqual(0, effect.ActiveParticleCount);
        }
    }
}
