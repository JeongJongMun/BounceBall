using UnityEngine;

namespace Game
{
    // 안드로이드 상태 표시줄·내비게이션 바를 숨긴다.
    //
    // 유니티의 "Start In Fullscreen" 체크박스는 예전 방식(setSystemUiVisibility)을 쓰는데,
    // 안드로이드 15(API 35)부터 edge-to-edge가 강제되면서 그 플래그가 폐기되어 무시된다.
    // 설정은 켜져 있는데 바가 그대로 남는 이유가 이것이다.
    //
    // 현행 API인 WindowInsetsController(API 30+)로 직접 숨긴다.
    // 동작은 "쓸어내리면 잠깐 나왔다가 다시 숨는" 방식으로 지정한다 —
    // 완전히 못 꺼내게 막으면 사용자가 알림이나 뒤로 가기에 접근할 수 없다.
    public static class AndroidImmersiveMode
    {
        // 화면을 덮는 시스템 바 전체를 숨긴다. 실패해도 게임은 그대로 진행한다 —
        // 바가 보이는 건 불편할 뿐이지만, 여기서 예외가 올라가면 시작이 막힌다.
        public static void Apply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null) return;

                    // 창 조작은 반드시 UI 스레드에서 해야 한다.
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() => HideBars(activity)));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Game] 몰입 모드 적용 실패: " + e.Message);
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void HideBars(AndroidJavaObject activity)
        {
            try
            {
                using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                {
                    if (window == null) return;

                    // 콘텐츠를 시스템 바 영역까지 그린다. 이걸 켜지 않으면 바를 숨겨도
                    // 그 자리가 빈 채로 남는다. UI는 SafeAreaFitter가 안전 영역 안에 유지한다.
                    window.Call("setDecorFitsSystemWindows", false);

                    using (var controller = window.Call<AndroidJavaObject>("getInsetsController"))
                    {
                        if (controller == null) return;

                        using (var insetTypes = new AndroidJavaClass("android.view.WindowInsets$Type"))
                        {
                            controller.Call("hide", insetTypes.CallStatic<int>("systemBars"));
                        }

                        // 상수를 직접 박지 않고 클래스에서 읽는다 — OS 버전에 따라 값이 바뀌어도 안전하다.
                        using (var controllerClass = new AndroidJavaClass("android.view.WindowInsetsController"))
                        {
                            int transientBySwipe = controllerClass.GetStatic<int>("BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE");
                            controller.Call("setSystemBarsBehavior", transientBySwipe);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Game] 시스템 바 숨김 실패: " + e.Message);
            }
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Apply();

            // 앱을 나갔다 돌아오면 바가 다시 나타나므로 그때마다 다시 숨긴다.
            var go = new GameObject("AndroidImmersiveMode");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<AndroidImmersiveModeWatcher>();
#endif
        }
    }

    // 포커스를 되찾을 때 몰입 모드를 다시 건다. Bootstrap이 만들어 두므로 씬 배선이 없다.
    internal class AndroidImmersiveModeWatcher : MonoBehaviour
    {
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) AndroidImmersiveMode.Apply();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) AndroidImmersiveMode.Apply();
        }
    }
}
