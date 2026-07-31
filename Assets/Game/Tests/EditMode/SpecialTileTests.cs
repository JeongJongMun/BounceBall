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
