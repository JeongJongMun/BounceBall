using Core;
using UnityEngine;

namespace Game
{
    // 사운드 재생의 단일 진입점. 어디서든 Sound.Play(SoundId.UI_Click) 한 줄로 부른다.
    // 클립이 아직 없거나 AudioManager가 없으면 조용히 무시하므로, 에셋을 나중에 채워도 된다.
    public static class Sound
    {
        private static SoundDatabase _database;

        private static SoundDatabase Database
        {
            get
            {
                if (_database == null) _database = SoundDatabase.Load();
                return _database;
            }
        }

        public static void Play(SoundId id)
        {
            var entry = Database != null ? Database.Find(id) : null;
            if (entry == null) return;

            var audio = AudioManager.Instance;
            if (audio == null) return;

            audio.PlaySFX(entry.clip, entry.volume);
        }

        public static void PlayBgm(SoundId id)
        {
            var entry = Database != null ? Database.Find(id) : null;
            if (entry == null) return;

            var audio = AudioManager.Instance;
            if (audio == null) return;

            // 같은 곡이면 AudioManager가 다시 시작하지 않는다 — 씬을 옮겨도 BGM이 이어진다.
            audio.PlayBGM(entry.clip);
        }

        public static void StopBgm()
        {
            var audio = AudioManager.Instance;
            if (audio != null) audio.StopBGM();
        }
    }
}
