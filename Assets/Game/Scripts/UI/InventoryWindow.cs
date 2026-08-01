using System.Collections.Generic;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game
{
    // 인벤토리 창 (인벤토리 문서 §5, UI 기획서 §5).
    // Systems 프리팹에 얹혀 씬 전환에도 유지되므로 스테이지 선택·인게임 양쪽에서 같은 창을 쓴다.
    public class InventoryWindow : Singleton<InventoryWindow>
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private InventorySlotView slotTemplate;
        [SerializeField] private Button closeButton;

        [Header("상세")]
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailDescriptionText;
        [SerializeField] private TMP_Text emptyText;

        private readonly List<InventorySlotView> _slots = new();
        private ItemDatabase _database;
        private string _selectedItemId;

        public bool IsOpen => root != null && root.activeSelf;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            _database = ItemDatabase.Load();
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (root != null) root.SetActive(false);
        }

        private void OnEnable() => Inventory.OnChanged += Refresh;
        private void OnDisable() => Inventory.OnChanged -= Refresh;

        protected override void OnDestroy()
        {
            UIPopupState.SetOpen(this, false);
            base.OnDestroy();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // I 로 열고 닫는다 (문서 §5.1)
            if (Keyboard.current.iKey.wasPressedThisFrame) Toggle();
            else if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (root == null) return;
            root.SetActive(true);
            UIPopupState.SetOpen(this, true);
            Refresh();
        }

        public void Close()
        {
            if (root == null) return;
            root.SetActive(false);
            UIPopupState.SetOpen(this, false);
        }

        private void Refresh()
        {
            if (!IsOpen) return;

            foreach (var slot in _slots) Destroy(slot.gameObject);
            _slots.Clear();

            _database ??= ItemDatabase.Load();
            if (_database == null || slotTemplate == null) return;

            slotTemplate.gameObject.SetActive(false);

            foreach (var entry in Inventory.Entries)
            {
                var item = _database.Find(entry.Key);
                if (item == null) continue;

                var slot = Instantiate(slotTemplate, slotContainer);
                slot.gameObject.SetActive(true);
                slot.Bind(item, entry.Value);
                slot.Clicked += HandleSlotClicked;
                slot.DoubleClicked += HandleSlotDoubleClicked;
                _slots.Add(slot);
            }

            if (emptyText != null) emptyText.gameObject.SetActive(_slots.Count == 0);

            // 선택이 사라졌으면(수량 0으로 제거) 상세를 비운다
            if (!string.IsNullOrEmpty(_selectedItemId) && Inventory.GetCount(_selectedItemId) <= 0)
                _selectedItemId = null;

            ShowDetail(_selectedItemId);
            foreach (var slot in _slots) slot.SetSelected(slot.ItemId == _selectedItemId);
        }

        private void HandleSlotClicked(InventorySlotView slot)
        {
            _selectedItemId = slot.ItemId;
            foreach (var other in _slots) other.SetSelected(other == slot);
            ShowDetail(_selectedItemId);
        }

        // 더블 클릭으로 사용 (문서 §5.4). 인게임이 아니면 ItemUseService가 막는다.
        private void HandleSlotDoubleClicked(InventorySlotView slot)
        {
            ItemUseService.TryUse(slot.ItemId);
        }

        private void ShowDetail(string itemId)
        {
            var item = string.IsNullOrEmpty(itemId) ? null : _database?.Find(itemId);
            if (detailNameText != null) detailNameText.text = item != null ? item.ItemName : "";
            if (detailDescriptionText != null) detailDescriptionText.text = item != null ? item.Description : "";
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(GameObject windowRoot, Transform container, InventorySlotView template,
            Button close, TMP_Text detailName, TMP_Text detailDescription, TMP_Text empty)
        {
            root = windowRoot;
            slotContainer = container;
            slotTemplate = template;
            closeButton = close;
            detailNameText = detailName;
            detailDescriptionText = detailDescription;
            emptyText = empty;
        }
    }
}
