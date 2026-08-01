using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // 보유 아이템 (인벤토리 문서 §5, §12.3). 코인과 마찬가지로 플레이어 공용 데이터라 영구 저장한다.
    public static class Inventory
    {
        private const string EntriesKey = "game.inventory.entries";

        [Serializable]
        private class Entry
        {
            public string itemId;
            public int count;
        }

        [Serializable]
        private class SaveModel
        {
            public List<Entry> entries = new();
        }

        public static event Action OnChanged;

        private static SaveModel _cache;

        private static SaveModel Data
        {
            get
            {
                if (_cache != null) return _cache;

                var json = PlayerPrefs.GetString(EntriesKey, "");
                _cache = string.IsNullOrEmpty(json) ? new SaveModel() : JsonUtility.FromJson<SaveModel>(json);
                _cache ??= new SaveModel();
                return _cache;
            }
        }

        private static void Save()
        {
            PlayerPrefs.SetString(EntriesKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        // 보유 중인 아이템 ID와 수량. 인벤토리 UI가 이 순서대로 슬롯을 만든다.
        public static IEnumerable<KeyValuePair<string, int>> Entries
        {
            get
            {
                foreach (var entry in Data.entries)
                    yield return new KeyValuePair<string, int>(entry.itemId, entry.count);
            }
        }

        public static int EntryCount => Data.entries.Count;

        public static int GetCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            var entry = Find(itemId);
            return entry != null ? entry.count : 0;
        }

        public static bool Has(string itemId, int count = 1) => GetCount(itemId) >= count;

        public static void Add(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return;

            var entry = Find(itemId);
            if (entry == null) Data.entries.Add(new Entry { itemId = itemId, count = count });
            else entry.count += count;

            Save();
        }

        // 수량이 부족하면 차감하지 않고 false (문서 §5.4 — 사용 실패 시 수량을 차감하지 않는다)
        public static bool TryConsume(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return false;

            var entry = Find(itemId);
            if (entry == null || entry.count < count) return false;

            entry.count -= count;
            // 수량이 0이면 슬롯에서 제거한다 (문서 §5.3)
            if (entry.count <= 0) Data.entries.Remove(entry);

            Save();
            return true;
        }

        public static void ResetAll()
        {
            _cache = new SaveModel();
            PlayerPrefs.DeleteKey(EntriesKey);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        private static Entry Find(string itemId)
        {
            foreach (var entry in Data.entries)
            {
                if (entry.itemId == itemId) return entry;
            }
            return null;
        }
    }
}
