using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Tests
{
    public class SpecialTileTests
    {
        private static SpecialTile CreateTile(TilePropertyType property)
        {
            var tile = ScriptableObject.CreateInstance<SpecialTile>();
            tile.SetTileProperty(property);
            return tile;
        }

        [Test]
        public void 타일_성질을_보관한다()
        {
            Assert.AreEqual(TilePropertyType.Jelly, CreateTile(TilePropertyType.Jelly).TileProperty);
            Assert.AreEqual(TilePropertyType.Ice, CreateTile(TilePropertyType.Ice).TileProperty);
        }

        [Test]
        public void 기본값은_Default다()
        {
            var tile = ScriptableObject.CreateInstance<SpecialTile>();
            Assert.AreEqual(TilePropertyType.Default, tile.TileProperty);
        }

        private static SpecialTile CreateDeathTile(TilePropertyType property, bool applySurfaceEffect = true)
        {
            var tile = CreateTile(property);
            tile.SetDeadly(true, applySurfaceEffect);
            return tile;
        }

        // 기믹 문서 §5 복합형 사망 발판 상호작용표
        [Test]
        public void 기본_사망_발판은_모든_성질에서_사망한다()
        {
            var tile = CreateDeathTile(TilePropertyType.Default);
            Assert.IsTrue(tile.IsLethalTo(PlayerPropertyType.Default));
            Assert.IsTrue(tile.IsLethalTo(PlayerPropertyType.Jelly));
            Assert.IsTrue(tile.IsLethalTo(PlayerPropertyType.Ice));
        }

        [Test]
        public void 젤리_사망_발판은_젤리_성질만_생존한다()
        {
            var tile = CreateDeathTile(TilePropertyType.Jelly);
            Assert.IsTrue(tile.IsLethalTo(PlayerPropertyType.Default));
            Assert.IsFalse(tile.IsLethalTo(PlayerPropertyType.Jelly));
            Assert.IsTrue(tile.IsLethalTo(PlayerPropertyType.Ice));
        }

        [Test]
        public void 얼음_사망_발판은_얼음_성질만_생존한다()
        {
            var tile = CreateDeathTile(TilePropertyType.Ice);
            Assert.IsTrue(tile.IsLethalTo(PlayerPropertyType.Default));
            Assert.IsTrue(tile.IsLethalTo(PlayerPropertyType.Jelly));
            Assert.IsFalse(tile.IsLethalTo(PlayerPropertyType.Ice));
        }

        [Test]
        public void 사망_발판이_아니면_아무도_죽지_않는다()
        {
            var tile = CreateTile(TilePropertyType.Jelly);
            Assert.IsFalse(tile.IsLethalTo(PlayerPropertyType.Default));
            Assert.IsFalse(tile.IsLethalTo(PlayerPropertyType.Ice));
        }

        // 기믹 문서 §4.3, §4.4: 생존 성질에서 부착·미끄러짐을 적용할지는 발판 데이터로 정한다
        [Test]
        public void 생존_성질은_표면_효과를_적용받는다()
        {
            var jelly = CreateDeathTile(TilePropertyType.Jelly);
            Assert.IsTrue(jelly.AppliesSurfaceEffectFor(PlayerPropertyType.Jelly));

            var ice = CreateDeathTile(TilePropertyType.Ice);
            Assert.IsTrue(ice.AppliesSurfaceEffectFor(PlayerPropertyType.Ice));
        }

        [Test]
        public void 표면_효과를_끄면_생존_성질에도_적용하지_않는다()
        {
            var tile = CreateDeathTile(TilePropertyType.Jelly, applySurfaceEffect: false);
            Assert.IsFalse(tile.IsLethalTo(PlayerPropertyType.Jelly), "생존 판정까지 막으면 안 된다");
            Assert.IsFalse(tile.AppliesSurfaceEffectFor(PlayerPropertyType.Jelly));
        }

        [Test]
        public void 일반_타일은_항상_표면_효과를_적용한다()
        {
            var tile = CreateTile(TilePropertyType.Jelly);
            Assert.IsTrue(tile.AppliesSurfaceEffectFor(PlayerPropertyType.Jelly));
            Assert.IsTrue(tile.AppliesSurfaceEffectFor(PlayerPropertyType.Default));
        }

        [Test]
        public void 월드_좌표로_특수_타일을_조회한다()
        {
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();
            var mapGo = new GameObject("Tilemap");
            mapGo.transform.SetParent(gridGo.transform);
            var tilemap = mapGo.AddComponent<Tilemap>();

            var special = CreateTile(TilePropertyType.Jelly);
            tilemap.SetTile(new Vector3Int(2, -3, 0), special);
            StageTiles.InvalidateCache();

            // 셀 (2,-3)의 윗면 중앙에 착지했다고 가정 (노멀 = 위)
            var found = StageTiles.GetSpecialTileAt(new Vector2(2.5f, -2f), Vector2.up);
            Assert.AreSame(special, found);

            // 다른 셀은 null → 호출부는 TilePropertyType.Default로 취급한다
            Assert.IsNull(StageTiles.GetSpecialTileAt(new Vector2(5.5f, -2f), Vector2.up));

            Object.DestroyImmediate(gridGo);
            StageTiles.InvalidateCache();
        }
    }
}
