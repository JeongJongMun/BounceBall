using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // 사운드 ID와 실제 클립을 잇는 목록. 게임 코드는 클립을 직접 참조하지 않고 ID로만 부른다.
    [CreateAssetMenu(menuName = "Game/Sound Database", fileName = "SoundDatabase")]
    public class SoundDatabase : ScriptableObject
    {
        public const string ResourcePath = "SoundDatabase";

        [Serializable]
        public class Entry
        {
            public SoundId id;
            public AudioClip clip;
            [Range(0f, 1f)]
            [Tooltip("이 사운드만 따로 줄이고 싶을 때 쓰는 배율")]
            public float volume = 1f;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<SoundId, Entry> _lookup;

        public static SoundDatabase Load() => Resources.Load<SoundDatabase>(ResourcePath);

        // 등록되지 않았거나 클립이 비어 있으면 null — 호출부는 조용히 건너뛴다.
        public Entry Find(SoundId id)
        {
            if (id == SoundId.None) return null;

            if (_lookup == null || _lookup.Count != entries.Count)
            {
                _lookup = new Dictionary<SoundId, Entry>();
                foreach (var entry in entries)
                {
                    if (entry != null && entry.id != SoundId.None) _lookup[entry.id] = entry;
                }
            }

            if (!_lookup.TryGetValue(id, out var found)) return null;
            return found.clip != null ? found : null;
        }

        public void SetEntries(List<Entry> list)
        {
            entries = list;
            _lookup = null;
        }
    }
}
