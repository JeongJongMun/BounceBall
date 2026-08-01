using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    // 퀵슬롯 한 칸. 인벤토리 슬롯을 끌어다 놓으면 등록되고, 우클릭하면 해제된다 (인벤토리 문서 §5.6.3).
    public class QuickSlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image iconImage;

        public int SlotIndex { get; private set; }

        public void Setup(int index)
        {
            SlotIndex = index;
            if (keyText != null) keyText.text = KeyLabel(index);
            Refresh();
        }

        // 0~8번 칸은 1~9, 9번 칸은 0 키를 쓴다.
        public static string KeyLabel(int index) => index == 9 ? "0" : (index + 1).ToString();

        public void Refresh()
        {
            var itemId = QuickSlots.GetItemId(SlotIndex);
            var database = ItemDatabase.Load();
            var item = database != null ? database.Find(itemId) : null;

            if (iconImage != null)
            {
                iconImage.sprite = item != null ? item.Thumbnail : null;
                iconImage.enabled = iconImage.sprite != null;
            }
            if (nameText != null) nameText.text = item != null ? item.ItemName : "";

            int count = item != null ? Inventory.GetCount(item.ItemId) : 0;
            if (countText != null) countText.text = count > 0 ? count.ToString() : "";
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            var dragged = eventData.pointerDrag.GetComponent<InventorySlotView>();
            if (dragged == null || string.IsNullOrEmpty(dragged.ItemId)) return;

            // 소비형만 등록할 수 있다 (문서 §5.6.2)
            var database = ItemDatabase.Load();
            var item = database != null ? database.Find(dragged.ItemId) : null;
            if (item == null || !item.CanRegisterQuickSlot) return;

            QuickSlots.Register(SlotIndex, item.ItemId);
        }

        // 우클릭으로 해제한다 (문서 §5.6.3의 "별도의 해제 동작")
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right) QuickSlots.Clear(SlotIndex);
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(TMP_Text key, TMP_Text itemName, TMP_Text count, Image icon)
        {
            keyText = key;
            nameText = itemName;
            countText = count;
            iconImage = icon;
        }
    }
}
