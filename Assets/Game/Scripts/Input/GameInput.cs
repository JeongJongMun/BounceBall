using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    // 키보드와 터치를 한곳에서 합친다.
    //
    // 플랫폼이 아니라 "입력 장치가 있는가"로 갈라야 한다.
    // Application.isMobilePlatform은 네이티브 앱에서만 true이고 모바일 브라우저에서는
    // false라, 그걸로 분기하면 WebGL을 폰으로 여는 경우를 통째로 놓친다.
    // Touchscreen.current로 보면 데스크톱 웹·모바일 웹·안드로이드 앱이 빌드 하나로 덮인다.
    public static class GameInput
    {
        // 눌려 있는 터치 영역들의 방향 합. 좌우를 동시에 누르면 0이 되어
        // 키보드의 "동시 입력 = 0" 규칙(기획 §8.2)과 자연히 같아진다.
        private static float _touchAxis;

        // 터치 조작 UI를 띄울지. 데스크톱에서는 false라 화면을 가리지 않는다.
        public static bool HasTouchscreen => Touchscreen.current != null;

        public static float TouchHorizontal => Mathf.Clamp(_touchAxis, -1f, 1f);

        // 좌우 축. 키보드가 우선이고 눌린 키가 없을 때만 터치를 본다 —
        // 태블릿에 블루투스 키보드를 붙인 경우처럼 둘 다 있어도 서로 방해하지 않는다.
        public static float Horizontal()
        {
            float keyboard = KeyboardHorizontal();
            return Mathf.Approximately(keyboard, 0f) ? TouchHorizontal : keyboard;
        }

        private static float KeyboardHorizontal()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return 0f;

            bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            return left == right ? 0f : (left ? -1f : 1f);
        }

        // 터치 영역이 눌리고 떼일 때 호출한다. 손가락이 여럿이어도 합으로 관리해
        // 한 손가락을 떼도 남은 손가락의 입력이 유지된다.
        public static void AddTouchDirection(float direction) => _touchAxis += direction;

        // 씬 전환이나 UI 파괴로 뗀 신호를 놓쳤을 때 남는 입력을 끊는다.
        public static void ClearTouch() => _touchAxis = 0f;
    }
}
