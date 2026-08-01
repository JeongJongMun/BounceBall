using System.Collections.Generic;

namespace Game
{
    // 열려 있는 팝업을 추적한다.
    // 인벤토리 등 팝업이 열린 동안에는 퀵슬롯 숫자키 입력을 아이템 사용으로 처리하지 않는다 (인벤토리 문서 §11).
    public static class UIPopupState
    {
        private static readonly HashSet<object> Open = new();

        public static bool IsAnyOpen => Open.Count > 0;

        public static void SetOpen(object owner, bool open)
        {
            if (owner == null) return;
            if (open) Open.Add(owner);
            else Open.Remove(owner);
        }

        public static void Clear() => Open.Clear();
    }
}
