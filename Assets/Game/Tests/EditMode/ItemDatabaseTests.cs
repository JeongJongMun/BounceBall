using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class ItemDatabaseTests
    {
        private ItemDatabase _database;
        private readonly List<Object> _created = new();

        private ItemData CreateItem(string id, ItemCategory category)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.SetData(id, id, "", category, 10, null, null);
            _created.Add(item);
            return item;
        }

        [SetUp]
        public void SetUp()
        {
            _database = ScriptableObject.CreateInstance<ItemDatabase>();
            _created.Add(_database);

            var jelly = CreateItem("Property_Jelly", ItemCategory.PropertyConsumable);
            var ice = CreateItem("Property_Ice", ItemCategory.PropertyConsumable);
            _database.SetData(new List<ItemData> { jelly, ice }, new List<ItemData> { jelly, ice });
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        [Test]
        public void ID로_아이템을_찾는다()
        {
            Assert.AreEqual("Property_Jelly", _database.Find("Property_Jelly").ItemId);
        }

        [Test]
        public void 없는_ID는_null()
        {
            Assert.IsNull(_database.Find("Unknown"));
            Assert.IsNull(_database.Find(null));
        }

        [Test]
        public void 소비형은_인게임_사용과_퀵슬롯_등록이_가능하다()
        {
            var item = CreateItem("Property_Default", ItemCategory.PropertyConsumable);
            Assert.IsTrue(item.IsUsableInGame);
            Assert.IsTrue(item.CanRegisterQuickSlot);
            Assert.IsFalse(item.IsPlaceable);
        }

        [Test]
        public void 배치형은_퀵슬롯에_등록할_수_없다()
        {
            var item = CreateItem("Gimmick_Jump", ItemCategory.GimmickPlacement);
            Assert.IsTrue(item.IsPlaceable);
            Assert.IsFalse(item.CanRegisterQuickSlot);
            Assert.IsFalse(item.IsUsableInGame);
        }
    }
}
