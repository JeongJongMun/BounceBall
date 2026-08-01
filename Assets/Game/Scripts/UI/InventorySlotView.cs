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
        IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image iconImage;
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
                iconImage.enabled = iconImage.sprite != null;
            }
            if (countText != null) countText.text = count > 1 ? count.ToString() : "";
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectionOutline != null) selectionOutline.enabled = selected;
        }

        // 슬롯은 Selectable이 아니라 UiClickSoundSource가 붙지 않으므로 여기서 직접 낸다.
        // 누르는 순간에 내야 늦게 들리지 않는다.
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            // 두 번째 클릭은 사용(성공음·실패음)이 알려 주므로 클릭음을 겹치지 않는다
            if (eventData.clickCount < 2) Sound.Play(SoundId.UI_Click);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this);
            if (eventData.clickCount >= 2) DoubleClicked?.Invoke(this);
        }

        // 드래그 이벤트를 구현해야 퀵슬롯의 OnDrop이 호출된다.
        // 끄는 동안 원본은 흐려지고, 커서에는 고스트가 따라붙는다.
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(ItemId)) return;

            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0.5f;
            _canvasGroup.blocksRaycasts = false; // 아래의 퀵슬롯이 드롭을 받도록

            // 퀵슬롯 칸들이 강조 상태로 들어가게 알린다
            QuickSlotDragState.Begin(ItemId);

            var database = ItemDatabase.Load();
            var item = database != null ? database.Find(ItemId) : null;
            if (item != null && DragGhostView.Instance != null)
                DragGhostView.Instance.Show(item, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (DragGhostView.Instance != null) DragGhostView.Instance.Move(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (DragGhostView.Instance != null) DragGhostView.Instance.Hide();
            QuickSlotDragState.End();

            if (_canvasGroup == null) return;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(Image icon, TMP_Text count, Image outline)
        {
            iconImage = icon;
            countText = count;
            selectionOutline = outline;
        }
    }
}
