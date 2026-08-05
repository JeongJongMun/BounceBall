using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    // 퀵슬롯 한 칸 (인벤토리 문서 §5.6).
    // 인벤토리 창의 칸은 드롭을 받아 등록하고, 인게임 HUD의 칸은 표시 전용이다.
    public class QuickSlotView : MonoBehaviour,
        IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image iconImage;

        [Header("드래그 지시 효과")]
        [Tooltip("드래그가 시작되면 이 배율로 커진다")]
        [SerializeField] private float highlightScale = 1.12f;
        [Tooltip("이 칸 위에 올렸을 때의 배율")]
        [SerializeField] private float hoverScale = 1.2f;
        [Tooltip("등록이 확정될 때 튀는 배율")]
        [SerializeField] private float dropPopScale = 1.3f;
        [SerializeField] private float tweenDuration = 0.15f;

        private bool _acceptsDrop = true;
        private bool _hovered;

        public int SlotIndex { get; private set; }

        public void Setup(int index, bool acceptsDrop)
        {
            SlotIndex = index;
            _acceptsDrop = acceptsDrop;
            if (keyText != null) keyText.text = KeyLabel(index);
            Refresh();
        }

        // 0~8번 칸은 1~9, 9번 칸은 0 키를 쓴다.
        public static string KeyLabel(int index) => index == 9 ? "0" : (index + 1).ToString();

        private void OnEnable()
        {
            QuickSlotDragState.OnDragChanged += HandleDragChanged;
            ApplyScale(TargetScale(), 0f);
        }

        private void OnDisable()
        {
            QuickSlotDragState.OnDragChanged -= HandleDragChanged;
            transform.DOKill();
            transform.localScale = Vector3.one;
            _hovered = false;
        }

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

            int count = item != null ? Inventory.GetCount(item.ItemId) : 0;
            if (countText != null) countText.text = count > 0 ? count.ToString() : "";
        }

        // ── 드래그 지시 효과 ──

        private void HandleDragChanged(bool dragging)
        {
            if (!dragging) _hovered = false;
            ApplyScale(TargetScale(), tweenDuration);
        }

        // 드롭을 받지 않는 HUD 칸은 강조하지 않는다.
        private float TargetScale()
        {
            if (!_acceptsDrop || !QuickSlotDragState.IsDragging) return 1f;
            return _hovered ? hoverScale : highlightScale;
        }

        private void ApplyScale(float scale, float duration)
        {
            transform.DOKill();
            if (duration <= 0f)
            {
                transform.localScale = Vector3.one * scale;
                return;
            }
            // 일시정지(timeScale 0) 중에도 동작하도록 unscaled
            transform.DOScale(scale, duration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            if (QuickSlotDragState.IsDragging) ApplyScale(TargetScale(), tweenDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            if (QuickSlotDragState.IsDragging) ApplyScale(TargetScale(), tweenDuration);
        }

        // ── 등록 / 해제 ──

        public void OnDrop(PointerEventData eventData)
        {
            if (!_acceptsDrop || eventData.pointerDrag == null) return;

            string itemId = null;
            var inventorySlot = eventData.pointerDrag.GetComponent<InventorySlotView>();
            if (inventorySlot != null) itemId = inventorySlot.ItemId;
            else
            {
                // 퀵슬롯끼리 끌어 옮기는 경우
                var otherSlot = eventData.pointerDrag.GetComponent<QuickSlotView>();
                if (otherSlot != null) itemId = QuickSlots.GetItemId(otherSlot.SlotIndex);
            }
            if (string.IsNullOrEmpty(itemId)) return;

            // 소비형만 등록할 수 있다 (문서 §5.6.2)
            var database = ItemDatabase.Load();
            var item = database != null ? database.Find(itemId) : null;
            if (item == null || !item.CanRegisterQuickSlot) return;

            QuickSlots.Register(SlotIndex, item.ItemId);

            // 등록 확정 피드백: 톡 튀었다가 원래대로
            transform.DOKill();
            transform.localScale = Vector3.one * dropPopScale;
            transform.DOScale(TargetScale(), tweenDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // HUD 칸은 좌클릭·탭으로 사용한다. 숫자키가 없는 터치 기기에서는
            // 이 경로가 아이템을 쓸 수 있는 유일한 방법이다.
            // 인벤토리 창의 칸(등록용)은 드래그로 등록·해제하므로 제외한다.
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (_acceptsDrop) return;
                UseSlot();
                return;
            }

            // 우클릭으로 해제한다 (문서 §5.6.3의 "별도의 해제 동작")
            if (eventData.button != PointerEventData.InputButton.Right) return;
            if (QuickSlots.IsEmpty(SlotIndex)) return;

            QuickSlots.Clear(SlotIndex);
            Sound.Play(SoundId.UI_Click); // 우클릭은 전역 클릭음(좌클릭) 대상이 아니다
        }

        // 숫자키 경로(QuickSlotBar)와 같은 규칙을 따른다.
        // 빈 칸이면 조용히 무시하고(문서 §5.4), 실패 사유별 알림은 ItemUseService가 처리한다.
        private void UseSlot()
        {
            if (UIPopupState.IsAnyOpen) return; // 팝업이 열려 있으면 사용하지 않는다 (문서 §11)

            var itemId = QuickSlots.GetItemId(SlotIndex);
            if (string.IsNullOrEmpty(itemId)) return;

            ItemUseService.TryUse(itemId);
        }

        // 등록된 아이템을 칸 밖으로 끌어내면 해제한다 (문서 §5.6.3)
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_acceptsDrop || QuickSlots.IsEmpty(SlotIndex)) return;

            var itemId = QuickSlots.GetItemId(SlotIndex);
            QuickSlotDragState.Begin(itemId);

            var database = ItemDatabase.Load();
            var item = database != null ? database.Find(itemId) : null;
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

            // 다른 퀵슬롯 위에서 놓았으면 그쪽 OnDrop이 이동을 처리한다.
            // 칸 밖에서 놓았을 때만 해제한다.
            bool droppedOnQuickSlot = eventData.pointerCurrentRaycast.gameObject != null &&
                eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<QuickSlotView>() != null;

            if (!droppedOnQuickSlot) QuickSlots.Clear(SlotIndex);

            QuickSlotDragState.End();
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(TMP_Text key, TMP_Text count, Image icon)
        {
            keyText = key;
            countText = count;
            iconImage = icon;
        }
    }
}
