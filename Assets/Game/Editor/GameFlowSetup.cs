using Core;
using Core.UI;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    // 씬 기반 메뉴 흐름에 맞게 Systems.prefab을 조정하고 메뉴 씬/스테이지 DB를 준비한다.
    public static class GameFlowSetup
    {
        private const string SystemsPrefabPath = "Assets/Core/Resources/Systems.prefab";

        // CLI 진입점: Unity.exe -batchmode -executeMethod Game.EditorTools.GameFlowSetup.ApplyAll
        [MenuItem("Game/Apply Game Flow Setup")]
        public static void ApplyAll()
        {
            AdjustSystemsPrefab();
            MenuSceneGenerator.CreateMenuScene();
            StageDatabaseTools.Sync();
            Debug.Log("[Game] 게임 흐름 셋업 완료");
        }

        // Systems.prefab: 오버레이 메인 메뉴 제거 + BackToMenu가 MainMenu 씬을 로드하도록 설정.
        // 재실행해도 안전하다 (이미 적용된 항목은 건너뜀).
        private static void AdjustSystemsPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefabPath);
            try
            {
                var uiManager = root.GetComponentInChildren<UIManager>(true);
                if (uiManager != null)
                {
                    var so = new SerializedObject(uiManager);
                    var mainMenuProp = so.FindProperty("mainMenu");
                    var menuScreen = mainMenuProp.objectReferenceValue as MainMenuScreen;
                    mainMenuProp.objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    if (menuScreen != null) Object.DestroyImmediate(menuScreen.gameObject);
                }

                var gameManager = root.GetComponentInChildren<GameManager>(true);
                if (gameManager != null)
                {
                    var so = new SerializedObject(gameManager);
                    so.FindProperty("menuSceneName").stringValue = "MainMenu";
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[Game] Systems.prefab 조정 완료 (오버레이 메뉴 제거, menuSceneName=MainMenu)");
        }
    }
}
