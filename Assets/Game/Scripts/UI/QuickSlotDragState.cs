using System;

namespace Game
{
    // 인벤토리에서 아이템을 끌고 있는 중인지 알린다.
    // 퀵슬롯 칸들이 이 신호를 받아 "여기에 놓으세요" 강조 상태로 들어간다.
    public static class QuickSlotDragState
    {
        public static string DraggingItemId { get; private set; }
        public static bool IsDragging => !string.IsNullOrEmpty(DraggingItemId);

        // true = 드래그 시작, false = 종료
        public static event Action<bool> OnDragChanged;

        public static void Begin(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            DraggingItemId = itemId;
            OnDragChanged?.Invoke(true);
        }

        public static void End()
        {
            if (!IsDragging) return;
            DraggingItemId = null;
            OnDragChanged?.Invoke(false);
        }
    }
}
