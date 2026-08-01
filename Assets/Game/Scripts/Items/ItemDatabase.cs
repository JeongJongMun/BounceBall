using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // 아이템 정의의 단일 출처. 인벤토리·상점·퀵슬롯이 ItemID로 여기를 조회한다.
    [CreateAssetMenu(menuName = "Game/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public const string ResourcePath = "ItemDatabase";

        [SerializeField] private List<ItemData> items = new();
        [Tooltip("상점에 노출할 아이템 (순서대로 진열)")]
        [SerializeField] private List<ItemData> shopProducts = new();

        private Dictionary<string, ItemData> _lookup;

        public IReadOnlyList<ItemData> Items => items;
        public IReadOnlyList<ItemData> ShopProducts => shopProducts;

        public static ItemDatabase Load() => Resources.Load<ItemDatabase>(ResourcePath);

        public ItemData Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            if (_lookup == null || _lookup.Count != items.Count)
            {
                _lookup = new Dictionary<string, ItemData>();
                foreach (var item in items)
                {
                    if (item != null && !string.IsNullOrEmpty(item.ItemId)) _lookup[item.ItemId] = item;
                }
            }
            return _lookup.TryGetValue(itemId, out var found) ? found : null;
        }

        public void SetData(List<ItemData> allItems, List<ItemData> products)
        {
            items = allItems;
            shopProducts = products;
            _lookup = null;
        }
    }
}
