using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class HazardSurfaceTests
    {
        private GameObject _go;
        private HazardSurface _hazard;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Spikes");
            _go.AddComponent<PolygonCollider2D>();
            _hazard = _go.AddComponent<HazardSurface>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void 가시면_접촉은_사망_등면은_생존이다()
        {
            // 기본 배치: 가시 위 (transform.up = 위)
            Assert.IsTrue(_hazard.IsLethalOnContact(PlayerPropertyType.Default, Vector2.up));
            Assert.IsFalse(_hazard.IsLethalOnContact(PlayerPropertyType.Default, Vector2.down));
        }

        [Test]
        public void 프리팹을_회전하면_가시_방향이_따라간다()
        {
            // 180도 회전 = 천장 가시: 아래서 받으면 사망, 위(등면)를 밟으면 생존
            _go.transform.rotation = Quaternion.Euler(0f, 0f, 180f);

            Assert.IsTrue(_hazard.IsLethalOnContact(PlayerPropertyType.Default, Vector2.down));
            Assert.IsFalse(_hazard.IsLethalOnContact(PlayerPropertyType.Default, Vector2.up));
        }

        [Test]
        public void 타일_사망_발판과_같은_면제_표를_쓴다()
        {
            var so = new UnityEditor.SerializedObject(_hazard);
            so.FindProperty("tileProperty").enumValueIndex = (int)TilePropertyType.Jelly;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsFalse(_hazard.IsLethalOnContact(PlayerPropertyType.Jelly, Vector2.up));  // 면제
            Assert.IsTrue(_hazard.IsLethalOnContact(PlayerPropertyType.Default, Vector2.up)); // 사망
        }

        [Test]
        public void 전방향_사망이면_등면에서도_죽는다()
        {
            var so = new UnityEditor.SerializedObject(_hazard);
            so.FindProperty("lethalAllDirections").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(_hazard.IsLethalOnContact(PlayerPropertyType.Default, Vector2.down));
        }
    }
}
