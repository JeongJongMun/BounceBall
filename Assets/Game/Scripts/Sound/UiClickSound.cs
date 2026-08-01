using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    // 씬 안의 모든 버튼에 UiClickSoundSource를 한 번씩 붙여 준다.
    // 클릭할 때마다 화면을 레이캐스트해 대상을 찾던 방식을 대체한다 — 클릭 시점의 비용이 0이 된다.
    // 비활성 팝업과 버튼 템플릿까지 훑으므로, 템플릿을 복제해 만드는 런타임 버튼도 자동으로 소리가 난다.
    public class UiClickSound : MonoBehaviour
    {
        private static readonly List<Selectable> Buffer = new();

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            InstallAll();
        }

        private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => InstallAll();

        public static void InstallAll()
        {
            var selectables = FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var selectable in selectables) Ensure(selectable.gameObject);
        }

        // 런타임에 만든 UI에 쓴다. 자식 버튼까지 모두 부착한다.
        public static void Install(GameObject root)
        {
            if (root == null) return;

            root.GetComponentsInChildren(true, Buffer);
            foreach (var selectable in Buffer) Ensure(selectable.gameObject);
        }

        // 버튼 하나에 부착하고 그 컴포넌트를 돌려준다. 소리 종류를 바꿀 때 쓴다.
        public static UiClickSoundSource Ensure(GameObject buttonObject)
        {
            if (buttonObject == null) return null;

            return buttonObject.TryGetComponent<UiClickSoundSource>(out var source)
                ? source
                : buttonObject.AddComponent<UiClickSoundSource>();
        }
    }
}
