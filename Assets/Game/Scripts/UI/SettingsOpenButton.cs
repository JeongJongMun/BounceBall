using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 씬에 있는 설정 버튼에 붙인다.
    // 설정 창은 Systems 프리팹(런타임 생성)에 있고 평소 꺼져 있으므로 비활성 오브젝트까지 찾는다.
    [RequireComponent(typeof(Button))]
    public class SettingsOpenButton : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                var window = FindWindow();
                if (window != null) window.Open();
            });
        }

        private static SettingsWindow FindWindow()
        {
            var windows = Object.FindObjectsByType<SettingsWindow>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            return windows.Length > 0 ? windows[0] : null;
        }
    }
}
