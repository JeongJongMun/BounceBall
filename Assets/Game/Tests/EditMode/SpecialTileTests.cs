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

        // 가시 타일은 위·옆에만 가시가 돋아 있다 — 밑에서 천장으로 받는 접촉으로는 죽지 않는다.
        // contactNormal은 타일에서 플레이어를 향하므로 아래를 향하면 아랫면 접촉이다.
        [Test]
        public void 아랫면_사망을_끄면_천장으로_받아도_죽지_않는다()
        {
            var tile = CreateTile(TilePropertyType.Default);
            tile.SetDeadly(true, applySurfaceEffect: true, fromBelow: false);

            Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.up), "윗면은 죽어야 한다");
            Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.right), "옆면은 죽어야 한다");
            Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.left), "옆면은 죽어야 한다");
            Assert.IsFalse(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.down), "아랫면은 죽으면 안 된다");

            // 경계: 노멀 y가 -0.5보다 위면 아직 옆면으로 본다 (PlayerBounce의 하단 접촉 기준과 동일).
            Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, new Vector2(0.9f, -0.44f).normalized),
                "비스듬한 옆면 접촉까지 살려주면 안 된다");
        }

        // 기믹 문서 §4, §6: 일반 사망 발판은 벽·천장 접촉에서도 사망한다 — 기본값은 그대로여야 한다.
        [Test]
        public void 사망_발판은_기본적으로_모든_방향에서_죽는다()
        {
            var tile = CreateDeathTile(TilePropertyType.Default);
            Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.up));
            Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.right));
            Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.down));
        }

        [Test]
        public void 생존_성질은_접촉_방향과_무관하게_안전하다()
        {
            var tile = CreateTile(TilePropertyType.Jelly);
            tile.SetDeadly(true, applySurfaceEffect: true, fromBelow: false);

            Assert.IsFalse(tile.IsLethalOnContact(PlayerPropertyType.Jelly, Vector2.up));
            Assert.IsFalse(tile.IsLethalOnContact(PlayerPropertyType.Jelly, Vector2.down));
        }

        // 실제 스테이지에 깔리는 가시 타일 3종이 아랫면 사망을 꺼둔 채로 유지되는지 확인한다.
        [TestCase("Assets/Game/Tiles/Default/Tile_Default_DS_01.asset")]
        [TestCase("Assets/Game/Tiles/Ice/Tile_Ice_DS_01.asset")]
        [TestCase("Assets/Game/Tiles/Jelly/Tile_Jelly_DS_01.asset")]
        public void 가시_타일은_아랫면_사망이_꺼져_있다(string assetPath)
        {
            var tile = UnityEditor.AssetDatabase.LoadAssetAtPath<SpecialTile>(assetPath);
            Assert.IsNotNull(tile, $"{assetPath}를 찾을 수 없습니다");
            Assert.IsTrue(tile.IsDeadly, "가시 타일은 사망 발판이어야 한다");

            Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.up), "윗면");
            Assert.IsFalse(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.down), "아랫면");
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
