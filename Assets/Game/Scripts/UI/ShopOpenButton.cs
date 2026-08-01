using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 씬에 있는 상점 아이콘에 붙인다. 상점 창은 Systems 프리팹(런타임 생성)에 있어 런타임에 찾는다.
    [RequireComponent(typeof(Button))]
    public class ShopOpenButton : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (ShopWindow.Instance != null) ShopWindow.Instance.Toggle();
            });
        }
    }
}
