using UnityEngine;

namespace Game
{
    // 아이템 분류 (인벤토리 문서 §12.2). 배치형은 이번 범위에서 구현하지 않는다.
    // 값이 에셋에 int로 저장되므로 새 분류는 항상 뒤에 추가한다.
    public enum ItemCategory
    {
        PropertyConsumable,
        GimmickPlacement,
        SpecialTilePlacement,
        Consumable // 상점 전용 소비형 (더블 점프·대시·실드)
    }

    // 상점·인벤토리에서 다루는 아이템 정의 (인벤토리 문서 §5.5, §12.1).
    [CreateAssetMenu(menuName = "Game/Item Data", fileName = "NewItemData")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite thumbnail;
        [SerializeField] private ItemCategory category = ItemCategory.PropertyConsumable;
        [SerializeField] private int price = 10;

        [Header("사용 효과")]
        [Tooltip("사용했을 때 일어날 일. 소비형 아이템은 반드시 지정해야 한다")]
        [SerializeField] private ItemEffect effect;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Thumbnail => thumbnail;
        public ItemCategory Category => category;
        public int Price => price;
        public ItemEffect Effect => effect;

        // 인게임에서 사용 가능한가 (문서 §5.5 IsUsableInGame)
        public bool IsUsableInGame => IsConsumable;

        // 맵에 설치하는 아이템인가 (문서 §5.5 IsPlaceable). 배치 기능은 이번 범위 밖이라 데이터만 둔다.
        public bool IsPlaceable => !IsConsumable;

        // 퀵슬롯 등록 가능 여부 (문서 §5.6.2 — 소비형만 등록 가능)
        public bool CanRegisterQuickSlot => IsConsumable;

        // 성질 변화든 상점 전용이든 '쓰면 사라지는' 아이템은 동일하게 다룬다.
        private bool IsConsumable =>
            category == ItemCategory.PropertyConsumable || category == ItemCategory.Consumable;

        // 에디터 툴링에서 배선할 때 사용.
        public void SetData(string id, string displayName, string desc, ItemCategory itemCategory,
            int itemPrice, ItemEffect itemEffect, Sprite icon)
        {
            itemId = id;
            itemName = displayName;
            description = desc;
            category = itemCategory;
            price = itemPrice;
            effect = itemEffect;
            thumbnail = icon;
        }
    }
}
