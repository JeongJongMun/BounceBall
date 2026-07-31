using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Tests
{
    public class SpecialTileTests
    {
        private SpecialTile CreateTile(params SpecialTile.Reaction[] reactions)
        {
            var tile = ScriptableObject.CreateInstance<SpecialTile>();
            tile.SetData("Test", new List<SpecialTile.Reaction>(reactions));
            return tile;
        }

        [Test]
        public void 성질_태그와_일치하는_반응을_반환한다()
        {
            var tile = CreateTile(
                new SpecialTile.Reaction { propertyTag = "", effectId = "Default", value = 1f },
                new SpecialTile.Reaction { propertyTag = "Rubber", effectId = "JumpMultiplier", value = 2f });

            var reaction = tile.GetReaction("Rubber");
            Assert.AreEqual("JumpMultiplier", reaction.effectId);
            Assert.AreEqual(2f, reaction.value);
        }

        [Test]
        public void 일치_태그가_없으면_기본_반응으로_폴백한다()
        {
            var tile = CreateTile(
                new SpecialTile.Reaction { propertyTag = "", effectId = "Default", value = 1.5f });

            var reaction = tile.GetReaction("Magnet");
            Assert.AreEqual("Default", reaction.effectId);
        }

        [Test]
        public void 반응이_없으면_null()
        {
            var tile = CreateTile();
            Assert.IsNull(tile.GetReaction("Rubber"));
        }

        [Test]
        public void 월드_좌표로_특수_타일을_조회한다()
        {
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();
            var mapGo = new GameObject("Tilemap");
            mapGo.transform.SetParent(gridGo.transform);
            var tilemap = mapGo.AddComponent<Tilemap>();

            var special = CreateTile(new SpecialTile.Reaction { effectId = "Default" });
            tilemap.SetTile(new Vector3Int(2, -3, 0), special);
            StageTiles.InvalidateCache();

            // 셀 (2,-3)의 윗면 중앙에 착지했다고 가정 (노멀 = 위)
            var found = StageTiles.GetSpecialTileAt(new Vector2(2.5f, -2f), Vector2.up);
            Assert.AreSame(special, found);

            // 다른 셀은 null
            Assert.IsNull(StageTiles.GetSpecialTileAt(new Vector2(5.5f, -2f), Vector2.up));

            Object.DestroyImmediate(gridGo);
            StageTiles.InvalidateCache();
        }
    }
}
