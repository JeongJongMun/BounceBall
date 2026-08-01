using UnityEngine;

namespace Game
{
    // 아이템 분류 (인벤토리 문서 §12.2). 배치형은 이번 범위에서 구현하지 않는다.
    public enum ItemCategory { PropertyConsumable, GimmickPlacement, SpecialTilePlacement }

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

        [Header("성질 변화 아이템 (PropertyConsumable)")]
        [Tooltip("사용 시 플레이어에게 적용할 성질")]
        [SerializeField] private PropertyData grantedProperty;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Thumbnail => thumbnail;
        public ItemCategory Category => category;
        public int Price => price;
        public PropertyData GrantedProperty => grantedProperty;

        // 인게임에서 사용 가능한가 (문서 §5.5 IsUsableInGame)
        public bool IsUsableInGame => category == ItemCategory.PropertyConsumable;

        // 맵에 설치하는 아이템인가 (문서 §5.5 IsPlaceable). 배치 기능은 이번 범위 밖이라 데이터만 둔다.
        public bool IsPlaceable => category != ItemCategory.PropertyConsumable;

        // 퀵슬롯 등록 가능 여부 (문서 §5.6.2 — 소비형만 등록 가능)
        public bool CanRegisterQuickSlot => category == ItemCategory.PropertyConsumable;

        public void SetData(string id, string displayName, string desc, ItemCategory itemCategory,
            int itemPrice, PropertyData property, Sprite icon)
        {
            itemId = id;
            itemName = displayName;
            description = desc;
            category = itemCategory;
            price = itemPrice;
            grantedProperty = property;
            thumbnail = icon;
        }
    }
}
