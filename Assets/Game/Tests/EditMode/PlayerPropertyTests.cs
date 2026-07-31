using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class PlayerPropertyTests
    {
        private GameObject _go;
        private Player _player;
        private SpriteRenderer _renderer;
        private PlayerProperty _property;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Player");
            _go.AddComponent<Rigidbody2D>();
            _go.AddComponent<PlayerStats>();
            _player = _go.AddComponent<Player>();
            _renderer = _go.AddComponent<SpriteRenderer>();
            _property = _go.AddComponent<PlayerProperty>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private static PropertyData CreateProperty(PlayerPropertyType type, Color color)
        {
            var data = ScriptableObject.CreateInstance<PropertyData>();
            data.SetData(type, type.ToString(), color);
            return data;
        }

        [Test]
        public void 성질_적용시_성질과_색상이_반영된다()
        {
            var jelly = CreateProperty(PlayerPropertyType.Jelly, Color.blue);

            _property.Apply(jelly);

            Assert.AreEqual(PlayerPropertyType.Jelly, _player.PropertyType);
            Assert.AreEqual(Color.blue, _renderer.color);
        }

        [Test]
        public void 동일_성질_재적용시_변화가_없다()
        {
            var first = CreateProperty(PlayerPropertyType.Jelly, Color.blue);
            var sameTypeDifferentColor = CreateProperty(PlayerPropertyType.Jelly, Color.red);

            _property.Apply(first);
            _property.Apply(sameTypeDifferentColor);

            Assert.AreSame(first, _property.Current);
            Assert.AreEqual(Color.blue, _renderer.color);
        }

        [Test]
        public void 다른_성질_적용시_교체된다()
        {
            var basic = CreateProperty(PlayerPropertyType.Default, Color.green);
            var ice = CreateProperty(PlayerPropertyType.Ice, Color.cyan);

            _property.Apply(basic);
            _property.Apply(ice);

            Assert.AreSame(ice, _property.Current);
            Assert.AreEqual(PlayerPropertyType.Ice, _player.PropertyType);
            Assert.AreEqual(Color.cyan, _renderer.color);
        }

        [Test]
        public void Restore는_같은_성질이어도_강제로_다시_적용한다()
        {
            var jelly = CreateProperty(PlayerPropertyType.Jelly, Color.blue);
            _property.Apply(jelly);
            _renderer.color = Color.black; // 외부에서 어긋난 상태를 흉내낸다

            _property.Restore(jelly);

            Assert.AreEqual(Color.blue, _renderer.color, "Restore가 강제 재적용하지 않았습니다.");
        }
    }
}
