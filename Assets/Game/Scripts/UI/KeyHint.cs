using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 버튼에 붙는 키보드 단축키 배지(ESC·I·P·숫자)에 부착한다.
    // 터치로 조작 중이면 누를 키가 없어 안내가 거짓말이 되므로 감춘다.
    //
    // 조건은 InputMode가 판단한다 — 장치 유무로 가리면 데스크톱 브라우저에서도
    // Touchscreen이 잡혀 배지가 통째로 사라진다.
    //
    // GameObject를 끄지 않고 그래픽만 끈다. 꺼 버리면 OnDisable로 구독이 끊겨
    // 키보드로 되돌아왔을 때 다시 켤 방법이 없다.
    public class KeyHint : MonoBehaviour
    {
        private Graphic[] _graphics;

        private void Awake() => _graphics = GetComponentsInChildren<Graphic>(true);

        private void OnEnable()
        {
            InputMode.Changed += Apply;
            Apply();
        }

        private void OnDisable() => InputMode.Changed -= Apply;

        private void Apply()
        {
            if (_graphics == null) return;

            bool show = !InputMode.IsTouch;
            for (int i = 0; i < _graphics.Length; i++)
                if (_graphics[i] != null) _graphics[i].enabled = show;
        }
    }
}
