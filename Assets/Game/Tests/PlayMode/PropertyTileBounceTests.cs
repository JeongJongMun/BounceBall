using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Game.Tests
{
    // 성질 × 타일 조합이 실제 바운스에 반영되는지 검증 (기획 §3.1, §9)
    public class PropertyTileBounceTests
    {
        private GameObject _gridGo;
        private GameObject _playerGo;
        private Player _player;
        private Tilemap _tilemap;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                Object.Destroy(systems);
                yield return null;
            }

            _gridGo = new GameObject("Grid");
            _gridGo.AddComponent<Grid>();
            var mapGo = new GameObject("Ground");
            mapGo.transform.SetParent(_gridGo.transform);
            _tilemap = mapGo.AddComponent<Tilemap>();
            mapGo.AddComponent<TilemapRenderer>();
            mapGo.AddComponent<TilemapCollider2D>();

            _playerGo = new GameObject("Player");
            _playerGo.AddComponent<Rigidbody2D>().freezeRotation = true;
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _playerGo.AddComponent<PlayerStats>();
            _player = _playerGo.AddComponent<Player>();
            _playerGo.AddComponent<PlayerBounce>();

            StageTiles.InvalidateCache();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_playerGo);
            Object.Destroy(_gridGo);
            yield return null;
            StageTiles.InvalidateCache();
        }

        // 바닥 한 줄을 깔고 그 위로 플레이어를 떨어뜨려 첫 바운스 속도를 잰다.
        private IEnumerator MeasureBounce(TileBase tile, PlayerPropertyType property, System.Action<float> onResult)
        {
            for (int x = -3; x <= 3; x++) _tilemap.SetTile(new Vector3Int(x, -2, 0), tile);
            StageTiles.InvalidateCache();
            yield return new WaitForFixedUpdate();

            _player.PropertyType = property;
            _playerGo.transform.position = new Vector3(0.5f, 0.5f, 0f);
            _player.Body.linearVelocity = Vector2.zero;

            float peak = 0f;
            float deadline = Time.time + 3f;
            while (Time.time < deadline)
            {
                peak = Mathf.Max(peak, _player.Body.linearVelocity.y);
                if (peak > 0.1f) break;
                yield return new WaitForFixedUpdate();
            }
            onResult(peak);
        }

        [UnityTest]
        public IEnumerator 기본_성질은_기본_타일에서_일반_점프한다()
        {
            var ground = ScriptableObject.CreateInstance<Tile>();
            ground.colliderType = Tile.ColliderType.Grid;

            float bounce = 0f;
            yield return MeasureBounce(ground, PlayerPropertyType.Default, v => bounce = v);

            Assert.AreEqual(_player.Stats.NormalJumpForce, bounce, 0.5f);
        }

        [UnityTest]
        public IEnumerator 젤리_성질은_기본_타일에서_더_높이_점프한다()
        {
            var ground = ScriptableObject.CreateInstance<Tile>();
            ground.colliderType = Tile.ColliderType.Grid;

            float bounce = 0f;
            yield return MeasureBounce(ground, PlayerPropertyType.Jelly, v => bounce = v);

            Assert.AreEqual(_player.Stats.HighJumpForce, bounce, 0.5f);
        }

        [UnityTest]
        public IEnumerator 기본_성질은_젤리_타일에서_낮게_점프한다()
        {
            var jelly = ScriptableObject.CreateInstance<SpecialTile>();
            jelly.colliderType = Tile.ColliderType.Grid;
            jelly.SetTileProperty(TilePropertyType.Jelly);

            float bounce = 0f;
            yield return MeasureBounce(jelly, PlayerPropertyType.Default, v => bounce = v);

            Assert.AreEqual(_player.Stats.LowJumpForce, bounce, 0.5f);
        }

        [UnityTest]
        public IEnumerator 젤리_성질은_젤리_타일에서_튀지_않는다()
        {
            var jelly = ScriptableObject.CreateInstance<SpecialTile>();
            jelly.colliderType = Tile.ColliderType.Grid;
            jelly.SetTileProperty(TilePropertyType.Jelly);

            float bounce = 0f;
            yield return MeasureBounce(jelly, PlayerPropertyType.Jelly, v => bounce = v);

            // Attach — 자동 점프가 실행되지 않아야 한다 (기획 §8)
            Assert.Less(bounce, 0.1f, "젤리 타일에 부착해야 하는데 자동 점프가 발생했습니다.");
        }

        [UnityTest]
        public IEnumerator 얼음_성질은_얼음_타일에서_튀지_않는다()
        {
            var ice = ScriptableObject.CreateInstance<SpecialTile>();
            ice.colliderType = Tile.ColliderType.Grid;
            ice.SetTileProperty(TilePropertyType.Ice);

            float bounce = 0f;
            yield return MeasureBounce(ice, PlayerPropertyType.Ice, v => bounce = v);

            // Slide — 바닥에 붙어 미끄러지므로 자동 점프가 없어야 한다 (기획 §2.3)
            Assert.Less(bounce, 0.1f, "얼음 타일에서 미끄러져야 하는데 자동 점프가 발생했습니다.");
        }

        [UnityTest]
        public IEnumerator 얼음_성질은_기본_타일에서는_낮게_점프한다()
        {
            var ground = ScriptableObject.CreateInstance<Tile>();
            ground.colliderType = Tile.ColliderType.Grid;

            float bounce = 0f;
            yield return MeasureBounce(ground, PlayerPropertyType.Ice, v => bounce = v);

            Assert.AreEqual(_player.Stats.LowJumpForce, bounce, 0.5f);
        }
    }
}
