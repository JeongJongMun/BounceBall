using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    // 지금 플레이어가 무엇으로 조작하고 있는가.
    //
    // "장치가 있는가"로는 판별할 수 없다. 데스크톱 브라우저에서도 Unity가 Touchscreen
    // 장치를 만들기 때문에, Touchscreen.current != null 로 모바일을 가리면 데스크톱 웹이
    // 통째로 모바일로 오판된다. 반대로 모바일 브라우저는 Keyboard 장치를 보고할 수 있어
    // 키보드 유무로 갈라도 틀린다.
    //
    // 그래서 "마지막으로 실제로 쓴 입력"을 본다. 장치 목록이 아니라 사용자의 행동이
    // 기준이라 데스크톱 웹·모바일 웹·앱·터치 노트북이 모두 알아서 맞아떨어진다.
    public static class InputMode
    {
        public static event System.Action Changed;

        private static bool _isTouch;
        private static bool _initialized;

        public static bool IsTouch
        {
            get { EnsureInitialized(); return _isTouch; }
        }

        // 첫 입력이 오기 전 기본값. 키보드가 아예 없으면 터치 기기로 본다.
        // 어긋나더라도 첫 조작에서 바로 교정된다.
        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            _isTouch = ComputeDefaultIsTouch(Keyboard.current != null);
        }

        // 기본값 규칙만 떼어낸 것. 테스트 어셈블리는 Input System을 참조하지 않으므로
        // 장치를 직접 만들 수 없어, 규칙을 순수 함수로 검증한다.
        public static bool ComputeDefaultIsTouch(bool hasKeyboard) => !hasKeyboard;

        public static void ReportTouch() => Set(true);
        public static void ReportKeyboard() => Set(false);

        private static void Set(bool touch)
        {
            EnsureInitialized();
            if (_isTouch == touch) return;

            _isTouch = touch;
            var handler = Changed;
            if (handler != null) handler();
        }

        // 테스트용. 다음 조회에서 기본값을 다시 계산하게 만든다.
        public static void ResetForTest()
        {
            _initialized = false;
            Changed = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("InputModeWatcher");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<InputModeWatcher>();
        }
    }

    // 실제 입력을 감시해 InputMode를 갱신한다. Bootstrap이 만들어 두므로 씬 배선이 필요 없다.
    internal class InputModeWatcher : MonoBehaviour
    {
        private void Update()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                InputMode.ReportTouch();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) InputMode.ReportKeyboard();
        }
    }
}
