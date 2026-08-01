using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    // 기획 §11.1: 접촉 즉시 획득, 별도 입력 없음. §11.5: 시간 재생성 없음.
    public class PropertyItemAcquireTests
    {
        private GameObject _playerGo;
        private GameObject _itemGo;
        private Player _player;
        private PlayerProperty _property;
        private PropertyItem _item;
        private PropertyData _jelly;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                Object.Destroy(systems);
                yield return null;
            }

            _jelly = ScriptableObject.CreateInstance<PropertyData>();
            _jelly.SetData(PlayerPropertyType.Jelly, "젤리", Color.blue);

            _playerGo = new GameObject("Player");
            _playerGo.transform.position = new Vector3(5f, 0f, 0f); // 아이템과 떨어진 곳에서 시작
            var body = _playerGo.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 0f; // 접촉 판정만 검증하므로 낙하는 배제
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _playerGo.AddComponent<PlayerStats>();
            _player = _playerGo.AddComponent<Player>();
            _playerGo.AddComponent<SpriteRenderer>();
            _property = _playerGo.AddComponent<PlayerProperty>();

            _itemGo = new GameObject("PropertyItem");
            _itemGo.transform.position = Vector3.zero;
            var collider = _itemGo.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;
            _itemGo.AddComponent<SpriteRenderer>();
            _item = _itemGo.AddComponent<PropertyItem>();
            _item.SetData(_jelly);

            yield return null; // Awake/Start
            yield return new WaitForFixedUpdate();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_playerGo);
            Object.Destroy(_itemGo);
            yield return null;
        }

        // 플레이어를 아이템 위로 옮겨 트리거 진입을 발생시킨다.
        private IEnumerator TouchItem()
        {
            _playerGo.transform.position = Vector3.zero;
            _playerGo.GetComponent<Rigidbody2D>().position = Vector2.zero;
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        [UnityTest]
        public IEnumerator 접촉하면_입력_없이_성질을_획득한다()
        {
            Assert.IsFalse(_item.IsAcquired, "접촉 전에 이미 획득 상태입니다.");
            Assert.AreNotEqual(PlayerPropertyType.Jelly, _player.PropertyType);

            yield return TouchItem();

            Assert.AreSame(_jelly, _property.Current);
            Assert.AreEqual(PlayerPropertyType.Jelly, _player.PropertyType);
            Assert.IsTrue(_item.IsAcquired, "접촉 후에도 아이템이 획득 상태가 아닙니다.");
            Assert.IsFalse(_item.IsActive);
        }

        [UnityTest]
        public IEnumerator 획득한_아이템은_시간이_지나도_재생성되지_않는다()
        {
            yield return TouchItem();
            Assert.IsTrue(_item.IsAcquired);

            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(_item.IsAcquired, "시간 경과만으로 아이템이 되살아났습니다 (기획 §11.5).");
        }

        [UnityTest]
        public IEnumerator 조작_제한_중에는_획득하지_않는다()
        {
            _player.SetDisabled(true);

            yield return TouchItem();

            Assert.IsFalse(_item.IsAcquired, "Disabled 상태에서 아이템을 획득했습니다 (기획 §11.3).");
            Assert.IsNull(_property.Current);
        }

        [UnityTest]
        public IEnumerator 부활_복구로_아이템이_되살아난다()
        {
            yield return TouchItem();
            Assert.IsTrue(_item.IsAcquired);

            _item.Restore();

            Assert.IsFalse(_item.IsAcquired);
            Assert.IsTrue(_item.IsActive, "복구 후에도 획득 가능한 상태가 아닙니다 (기획 §11.5).");
        }
    }
}
