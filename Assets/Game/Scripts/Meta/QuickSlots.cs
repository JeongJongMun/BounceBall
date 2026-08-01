using System;
using UnityEngine;

namespace Game
{
    // 퀵슬롯 (인벤토리 문서 §5.6). 아이템을 따로 보관하지 않고 인벤토리의 ItemID만 참조한다.
    // 수량은 항상 Inventory에서 조회하므로 인벤토리와 항상 같은 값을 보여준다 (§5.3).
    public static class QuickSlots
    {
        // 기본 3칸. 늘리면 HUD와 숫자키(1~9, 0)가 함께 따라간다. 최대 10칸.
        public const int SlotCount = 3;
        public const int MaxSlotCount = 10;

        private const string SlotsKey = "game.quickslots";
        private const char Separator = '|';

        public static event Action OnChanged;

        private static string[] _cache;

        private static string[] Data
        {
            get
            {
                if (_cache != null) return _cache;

                _cache = new string[SlotCount];
                var saved = PlayerPrefs.GetString(SlotsKey, "");
                if (!string.IsNullOrEmpty(saved))
                {
                    var parts = saved.Split(Separator);
                    for (int i = 0; i < SlotCount && i < parts.Length; i++)
                        _cache[i] = string.IsNullOrEmpty(parts[i]) ? null : parts[i];
                }
                return _cache;
            }
        }

        private static void Save()
        {
            PlayerPrefs.SetString(SlotsKey, string.Join(Separator.ToString(), Data));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        public static bool IsValidIndex(int index) => index >= 0 && index < SlotCount;

        // 등록된 ItemID. 인벤토리에서 수량이 0이 된 아이템은 자동으로 비운다 (문서 §5.3)
        public static string GetItemId(int index)
        {
            if (!IsValidIndex(index)) return null;

            var itemId = Data[index];
            if (string.IsNullOrEmpty(itemId)) return null;

            if (Inventory.GetCount(itemId) <= 0)
            {
                Data[index] = null;
                Save();
                return null;
            }
            return itemId;
        }

        public static bool IsEmpty(int index) => string.IsNullOrEmpty(GetItemId(index));

        // 같은 아이템을 여러 칸에 중복 등록하지 않는다 — 기존 칸을 비우고 옮긴다 (문서 §5.6.4)
        public static void Register(int index, string itemId)
        {
            if (!IsValidIndex(index) || string.IsNullOrEmpty(itemId)) return;

            for (int i = 0; i < SlotCount; i++)
            {
                if (Data[i] == itemId) Data[i] = null;
            }
            Data[index] = itemId;
            Save();
        }

        public static void Clear(int index)
        {
            if (!IsValidIndex(index) || string.IsNullOrEmpty(Data[index])) return;
            Data[index] = null;
            Save();
        }

        public static int IndexOf(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return -1;
            for (int i = 0; i < SlotCount; i++)
            {
                if (Data[i] == itemId) return i;
            }
            return -1;
        }

        // 수량이 0이 된 등록을 한 번에 정리한다.
        public static void PruneEmpty()
        {
            bool changed = false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (!string.IsNullOrEmpty(Data[i]) && Inventory.GetCount(Data[i]) <= 0)
                {
                    Data[i] = null;
                    changed = true;
                }
            }
            if (changed) Save();
        }

        public static void ResetAll()
        {
            _cache = new string[SlotCount];
            PlayerPrefs.DeleteKey(SlotsKey);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }
}
