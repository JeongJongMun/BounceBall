using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    // 인벤토리 슬롯 하나. 클릭하면 선택(상세 표시), 더블 클릭하면 사용을 요청한다 (인벤토리 문서 §5.4).
    public class InventorySlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image selectionOutline;

        public string ItemId { get; private set; }

        public event Action<InventorySlotView> Clicked;
        public event Action<InventorySlotView> DoubleClicked;

        public void Bind(ItemData item, int count)
        {
            ItemId = item != null ? item.ItemId : null;

            if (iconImage != null)
            {
                iconImage.sprite = item != null ? item.Thumbnail : null;
                // 썸네일이 아직 없으면 아이콘을 숨기고 이름으로 구분한다
                iconImage.enabled = iconImage.sprite != null;
            }
            if (nameText != null) nameText.text = item != null ? item.ItemName : "";
            if (countText != null) countText.text = count > 1 ? count.ToString() : "";
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectionOutline != null) selectionOutline.enabled = selected;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this);
            if (eventData.clickCount >= 2) DoubleClicked?.Invoke(this);
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(Image icon, TMP_Text itemName, TMP_Text count, Image outline)
        {
            iconImage = icon;
            nameText = itemName;
            countText = count;
            selectionOutline = outline;
        }
    }
}
