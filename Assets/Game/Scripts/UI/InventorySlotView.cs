using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    // 인벤토리 슬롯 하나. 클릭하면 선택(상세 표시), 더블 클릭하면 사용을 요청한다 (인벤토리 문서 §5.4).
    // 퀵슬롯으로 끌어다 놓아 등록할 수도 있다 (§5.6.3).
    public class InventorySlotView : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image selectionOutline;

        private CanvasGroup _canvasGroup;

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

        // 드래그 이벤트를 구현해야 퀵슬롯의 OnDrop이 호출된다.
        // 끄는 동안에는 반투명 아이콘 대신 슬롯 자체를 살짝 흐리게 해 잡고 있음을 알린다.
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(ItemId)) return;
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0.5f;
            _canvasGroup.blocksRaycasts = false; // 아래의 퀵슬롯이 드롭을 받도록
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
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
