using System.Collections;
using UnityEngine;

namespace Game
{
    // 안드로이드 시스템 바(상태 표시줄·내비게이션 바)를 숨긴 상태로 유지한다.
    //
    // 엔진 소스를 확인한 근거:
    //
    // 1) 유니티는 이미 WindowInsetsController로 바를 숨긴다
    //    (com.unity3d.player.j1 이 hide/show/setSystemBarsBehavior 를 호출한다).
    //    따라서 JNI로 Window를 직접 건드리면 유니티 내부 상태와 어긋나 되돌려진다.
    //    유니티 테마 리소스 주석도 "밖에서 건드리면 유니티의 인셋 초기화를 방해한다"고 못박는다.
    //    그 경로를 움직이는 유일한 C# 스위치가 Screen.fullScreen 이다.
    //
    // 2) UnityPlayer.restoreInsetsIfNeeded() 는 안드로이드 11(API 30) 이상에서
    //    즉시 return 한다 (PlatformSupport.RED_VELVET_CAKE_SUPPORT 분기).
    //    이 메서드는 포커스 변경 때 불리므로, 최신 안드로이드에서는
    //    한 번 바가 나타나면 엔진이 다시 숨기지 않는다. 그 복원을 여기서 대신한다.
    public static class AndroidFullscreen
    {
        // 시작 시점용. 아직 전체화면이 아니므로 값 설정만으로 네이티브가 반응한다.
        public static void Apply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.fullScreen = true;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Apply();

            var go = new GameObject("AndroidFullscreen");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<AndroidFullscreenWatcher>();
#endif
        }
    }

    // 앱으로 돌아오거나 사용자가 바를 쓸어내린 뒤 다시 숨긴다.
    // Bootstrap이 만들어 두므로 씬 배선이 필요 없다.
    internal class AndroidFullscreenWatcher : MonoBehaviour
    {
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) Reapply();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) Reapply();
        }

        private void Reapply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            StopAllCoroutines();
            StartCoroutine(ForceFullscreen());
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Screen.fullScreen 이 이미 true면 값이 바뀌지 않아 네이티브 호출이 일어나지 않는다.
        // 한 프레임을 두고 false → true 로 되돌려야 유니티가 hideInsets 를 다시 부른다.
        // 돌아온 직후 한 순간 바가 스칠 수 있지만, 계속 남아 있는 것보다 낫다.
        private IEnumerator ForceFullscreen()
        {
            if (Screen.fullScreen)
            {
                Screen.fullScreen = false;
                yield return null;
            }

            Screen.fullScreen = true;
        }
#endif
    }
}
