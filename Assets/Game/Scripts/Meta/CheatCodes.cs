using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    // 키보드 시퀀스 치트. Y → E → A → H 순서로 누르면 전 스테이지 클리어 + 골드 9999.
    public class CheatCodes : MonoBehaviour
    {
        private static readonly Key[] YeahSequence = { Key.Y, Key.E, Key.A, Key.H };

        private int _yeahIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<CheatCodes>() != null) return;

            var systems = GameObject.Find("Systems");
            var go = new GameObject("CheatCodes");
            if (systems != null)
                go.transform.SetParent(systems.transform, false);
            else
                DontDestroyOnLoad(go);

            go.AddComponent<CheatCodes>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.anyKey.wasPressedThisFrame) return;

            var expected = YeahSequence[_yeahIndex];
            if (kb[expected].wasPressedThisFrame)
            {
                _yeahIndex++;
                if (_yeahIndex < YeahSequence.Length) return;

                _yeahIndex = 0;
                ApplyYeahCheat();
                return;
            }

            // 틀린 키라도 시퀀스 시작 키면 처음부터 다시 맞춘다.
            _yeahIndex = kb[YeahSequence[0]].wasPressedThisFrame ? 1 : 0;
        }

        private static void ApplyYeahCheat()
        {
            var database = Resources.Load<StageDatabase>("StageDatabase");
            if (database != null)
            {
                foreach (var stage in database.Stages)
                {
                    if (stage != null) StageProgress.SetCleared(stage.sceneName);
                }
            }

            CurrencyWallet.RestoreTo(9999);
            ToastManager.Show("예영이와-아이들");

            var stageUIs = FindObjectsByType<StageUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var ui in stageUIs)
                ui.RefreshProgress();

            Debug.Log("[Game] YEAH 치트 적용: 전 스테이지 클리어, 골드 9999");
        }
    }
}
