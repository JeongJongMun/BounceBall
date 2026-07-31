using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    // 에디터를 처음 켰을 때 메인 메뉴 씬을 자동으로 연다.
    // SessionState는 에디터 세션 동안 유지되므로 스크립트 리컴파일(도메인 리로드)에는 다시 실행되지 않는다.
    [InitializeOnLoad]
    public static class OpenMenuSceneOnLaunch
    {
        private const string SessionKey = "Game.OpenedMenuSceneOnLaunch";

        static OpenMenuSceneOnLaunch()
        {
            EditorApplication.delayCall += TryOpenMenuScene;
        }

        private static void TryOpenMenuScene()
        {
            if (Application.isBatchMode) return; // CLI 테스트/생성 실행에는 관여하지 않음
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!System.IO.File.Exists(MenuSceneGenerator.MenuScenePath)) return;
            if (EditorSceneManager.GetActiveScene().path == MenuSceneGenerator.MenuScenePath) return;

            EditorSceneManager.OpenScene(MenuSceneGenerator.MenuScenePath);
        }
    }
}
