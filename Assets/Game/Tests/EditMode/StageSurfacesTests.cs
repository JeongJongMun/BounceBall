using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class StageSurfacesTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp() => _go = new GameObject("Surface");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private Collider2D AddCollider() => _go.AddComponent<BoxCollider2D>();

        [Test]
        public void 표면_성질_콜라이더는_해당_성질로_판정된다()
        {
            var collider = AddCollider();
            _go.AddComponent<SurfaceProperty>().SetTileProperty(TilePropertyType.Jelly);

            var surface = StageSurfaces.Resolve(collider, Vector2.zero, Vector2.up);

            Assert.AreEqual(TilePropertyType.Jelly, surface.Property);
            // 사망 발판이 아니므로 어떤 성질에든 표면 효과 적용 — 젤리는 부착한다
            Assert.IsTrue(surface.AppliesSurfaceEffectFor(PlayerPropertyType.Jelly));
            Assert.IsTrue(surface.AppliesSurfaceEffectFor(PlayerPropertyType.Default));
        }

        [Test]
        public void 사망_콜라이더는_면제_성질에만_표면_효과를_적용한다()
        {
            var collider = AddCollider();
            var hazard = _go.AddComponent<HazardSurface>();
            var so = new UnityEditor.SerializedObject(hazard);
            so.FindProperty("tileProperty").enumValueIndex = (int)TilePropertyType.Jelly;
            so.ApplyModifiedPropertiesWithoutUndo();

            var surface = StageSurfaces.Resolve(collider, Vector2.zero, Vector2.up);

            Assert.AreEqual(TilePropertyType.Jelly, surface.Property);
            Assert.IsTrue(surface.AppliesSurfaceEffectFor(PlayerPropertyType.Jelly));   // 면제 → 부착 가능
            Assert.IsFalse(surface.AppliesSurfaceEffectFor(PlayerPropertyType.Default)); // 비면제 → 효과 없음 (사망은 별도)
        }

        [Test]
        public void 표식이_없는_콜라이더는_기본_표면이다()
        {
            var collider = AddCollider();

            var surface = StageSurfaces.Resolve(collider, new Vector2(9999f, 9999f), Vector2.up);

            Assert.AreEqual(TilePropertyType.Default, surface.Property);
            Assert.IsTrue(surface.AppliesSurfaceEffectFor(PlayerPropertyType.Default));
        }

        [Test]
        public void 콜라이더가_없어도_기본_표면으로_동작한다()
        {
            var surface = StageSurfaces.Resolve(null, new Vector2(9999f, 9999f), Vector2.up);
            Assert.AreEqual(TilePropertyType.Default, surface.Property);
        }
    }
}
