using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class PropertyItemAcquireTests
    {
        private GameObject _playerGo;
        private GameObject _itemGo;
        private Player _player;
        private PlayerProperty _property;
        private PlayerInteraction _interaction;
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
            _playerGo.transform.position = Vector3.zero;
            var body = _playerGo.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 0f; // 상호작용 판정만 검증하므로 낙하는 배제
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _playerGo.AddComponent<PlayerStats>();
            _player = _playerGo.AddComponent<Player>();
            _playerGo.AddComponent<SpriteRenderer>();
            _property = _playerGo.AddComponent<PlayerProperty>();
            _interaction = _playerGo.AddComponent<PlayerInteraction>();
            _interaction.ReadKeyboard = false;

            _itemGo = new GameObject("PropertyItem");
            _itemGo.transform.position = Vector3.zero; // 플레이어와 겹치게 배치해 감지 범위 안으로 만든다
            var collider = _itemGo.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1f;
            _itemGo.AddComponent<SpriteRenderer>();
            _item = _itemGo.AddComponent<PropertyItem>();
            _item.SetData(_jelly, null, respawn: 0.2f);

            yield return null; // Awake/Start + 첫 물리 스텝으로 트리거 감지 발생
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_playerGo);
            Object.Destroy(_itemGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 범위_안에서_E_입력시_성질을_획득한다()
        {
            Assert.AreSame(_item, _interaction.CurrentInteractable, "감지 범위 안의 아이템이 상호작용 대상으로 선택되지 않았습니다.");

            _interaction.TryAcquire();

            Assert.AreSame(_jelly, _property.Current);
            Assert.AreEqual(PlayerPropertyType.Jelly, _player.PropertyType);
            Assert.IsFalse(_item.IsActive, "획득 직후 아이템이 비활성화되지 않았습니다.");
            yield break;
        }

        [UnityTest]
        public IEnumerator 획득한_아이템은_지연시간후_재생성된다()
        {
            _interaction.TryAcquire();
            Assert.IsFalse(_item.IsActive);

            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(_item.IsActive, "0.2초 재생성 지연 후에도 아이템이 비활성 상태입니다.");
        }
    }
}
