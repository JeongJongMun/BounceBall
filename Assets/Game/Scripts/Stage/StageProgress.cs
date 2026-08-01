using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // 스테이지별 클리어 여부를 저장한다. 키는 StageDatabase의 sceneName을 기준으로 한다.
    public static class StageProgress
    {
        private const string KeyPrefix = "game.stage.cleared.";

        public static bool IsCleared(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return PlayerPrefs.GetInt(KeyPrefix + sceneName, 0) == 1;
        }

        public static void SetCleared(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            PlayerPrefs.SetInt(KeyPrefix + sceneName, 1);
            PlayerPrefs.Save();
        }

        public static void ResetAll(IEnumerable<string> sceneNames)
        {
            foreach (var name in sceneNames)
                PlayerPrefs.DeleteKey(KeyPrefix + name);
            PlayerPrefs.Save();
        }
    }
}
