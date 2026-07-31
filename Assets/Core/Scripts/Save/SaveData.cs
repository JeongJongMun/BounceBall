using UnityEngine;

namespace Core
{
    public static class SaveData
    {
        private const string HighScoreKey = "core.highscore";
        private const string MasterVolumeKey = "core.volume.master";
        private const string BgmVolumeKey = "core.volume.bgm";
        private const string SfxVolumeKey = "core.volume.sfx";

        public static int HighScore
        {
            get => PlayerPrefs.GetInt(HighScoreKey, 0);
            set { PlayerPrefs.SetInt(HighScoreKey, value); PlayerPrefs.Save(); }
        }

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            set { PlayerPrefs.SetFloat(MasterVolumeKey, value); PlayerPrefs.Save(); }
        }

        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            set { PlayerPrefs.SetFloat(BgmVolumeKey, value); PlayerPrefs.Save(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            set { PlayerPrefs.SetFloat(SfxVolumeKey, value); PlayerPrefs.Save(); }
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(HighScoreKey);
            PlayerPrefs.DeleteKey(MasterVolumeKey);
            PlayerPrefs.DeleteKey(BgmVolumeKey);
            PlayerPrefs.DeleteKey(SfxVolumeKey);
            PlayerPrefs.Save();
        }
    }
}
