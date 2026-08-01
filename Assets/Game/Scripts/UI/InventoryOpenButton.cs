using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 씬에 있는 인벤토리 아이콘에 붙인다.
    // 인벤토리 창은 Systems 프리팹(런타임 생성)에 있어 인스펙터로 연결할 수 없으므로 런타임에 찾는다.
    [RequireComponent(typeof(Button))]
    public class InventoryOpenButton : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (InventoryWindow.Instance != null) InventoryWindow.Instance.Toggle();
            });
        }
    }
}
