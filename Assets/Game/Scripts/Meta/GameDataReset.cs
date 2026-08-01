using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Game
{
    // 저장된 플레이어 데이터를 모두 지운다 (디버그용).
    // 스테이지 진행도, 코인, 인벤토리, 퀵슬롯, 볼륨 설정이 대상이다.
    public static class GameDataReset
    {
        public static void ResetAll()
        {
            ResetStageProgress();

            CurrencyWallet.ResetAll();
            Inventory.ResetAll();
            QuickSlots.ResetAll();

            // 하이스코어·볼륨 등 Core 저장값
            SaveData.ResetAll();
        }

        private static void ResetStageProgress()
        {
            var database = Resources.Load<StageDatabase>("StageDatabase");
            if (database == null) return;

            var sceneNames = new List<string>();
            foreach (var stage in database.Stages)
            {
                if (stage != null) sceneNames.Add(stage.sceneName);
            }
            StageProgress.ResetAll(sceneNames);
        }
    }
}
