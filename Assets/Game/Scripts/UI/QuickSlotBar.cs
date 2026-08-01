using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Game
{
    // 인게임 퀵슬롯 HUD (인벤토리 문서 §5.6, §11). 화면 하단에 가로로 배치된다.
    // 숫자키 입력은 팝업이 열려 있으면 처리하지 않는다 (§11).
    public class QuickSlotBar : MonoBehaviour
    {
        [SerializeField] private Transform slotContainer;
        [SerializeField] private QuickSlotView slotTemplate;

        private readonly List<QuickSlotView> _slots = new();

        private void Start() => Build();

        private void OnEnable()
        {
            Inventory.OnChanged += RefreshAll;
            QuickSlots.OnChanged += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            Inventory.OnChanged -= RefreshAll;
            QuickSlots.OnChanged -= RefreshAll;
        }

        private void Build()
        {
            if (slotTemplate == null || slotContainer == null) return;

            slotTemplate.gameObject.SetActive(false);
            foreach (var slot in _slots) Destroy(slot.gameObject);
            _slots.Clear();

            for (int i = 0; i < QuickSlots.SlotCount; i++)
            {
                var slot = Instantiate(slotTemplate, slotContainer);
                slot.gameObject.SetActive(true);
                // HUD는 표시·사용 전용이다. 등록은 인벤토리 창의 퀵슬롯에서 한다 (문서 §11)
                slot.Setup(i, acceptsDrop: false);
                _slots.Add(slot);
            }
        }

        private void RefreshAll()
        {
            foreach (var slot in _slots) slot.Refresh();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // 인벤토리 등 팝업이 열려 있으면 숫자키를 아이템 사용으로 처리하지 않는다 (문서 §11)
            if (UIPopupState.IsAnyOpen) return;

            for (int i = 0; i < QuickSlots.SlotCount; i++)
            {
                if (KeyForSlot(i).wasPressedThisFrame) UseSlot(i);
            }
        }

        private static KeyControl KeyForSlot(int index)
        {
            var keyboard = Keyboard.current;
            switch (index)
            {
                case 0: return keyboard.digit1Key;
                case 1: return keyboard.digit2Key;
                case 2: return keyboard.digit3Key;
                case 3: return keyboard.digit4Key;
                case 4: return keyboard.digit5Key;
                case 5: return keyboard.digit6Key;
                case 6: return keyboard.digit7Key;
                case 7: return keyboard.digit8Key;
                case 8: return keyboard.digit9Key;
                default: return keyboard.digit0Key;
            }
        }

        private void UseSlot(int index)
        {
            var itemId = QuickSlots.GetItemId(index);
            if (string.IsNullOrEmpty(itemId)) return; // 빈 슬롯이면 조용히 무시 (문서 §5.4)

            // 실패 시 알림음·안내 토스트는 ItemUseService가 사유에 맞게 처리한다
            ItemUseService.TryUse(itemId);
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(Transform container, QuickSlotView template)
        {
            slotContainer = container;
            slotTemplate = template;
        }
    }
}
