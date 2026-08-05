using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // HUD의 일시정지 아이콘에 붙인다.
    //
    // 키보드 없이도 일시정지에 들어갈 수 있어야 한다. 특히 브라우저 전체화면에서는
    // Escape를 브라우저가 먼저 가로채 한 번에 열리지 않고, 폰에는 아예 키보드가 없다.
    // GameManager는 Systems 프리팹(런타임 생성)에 있어 인스펙터로 연결할 수 없으므로 런타임에 찾는다.
    [RequireComponent(typeof(Button))]
    public class PauseOpenButton : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.TogglePause();
            });
        }
    }
}
