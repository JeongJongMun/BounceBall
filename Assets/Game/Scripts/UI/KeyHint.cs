using UnityEngine;

namespace Game
{
    // 버튼에 붙는 키보드 단축키 배지(ESC·I·P·숫자)에 부착한다.
    // 터치 기기에는 누를 키가 없어 안내가 거짓말이 되므로 숨긴다.
    //
    // TouchMoveZone과 같은 신호(Touchscreen.current)를 쓴다 —
    // 터치 조작이 켜지면 단축키 안내는 꺼지도록 항상 짝이 맞는다.
    //
    // 터치가 되는 노트북에서는 키보드가 있어도 배지가 숨는다. 단축키 자체는 그대로
    // 동작하고 버튼도 누를 수 있어 기능 손실은 없다.
    public class KeyHint : MonoBehaviour
    {
        private void Awake()
        {
            if (GameInput.HasTouchscreen) gameObject.SetActive(false);
        }
    }
}
