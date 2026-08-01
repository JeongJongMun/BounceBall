using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class SettingUI : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        
        [SerializeField] private Canvas mainCanvas;

        private void Awake()
        {
            if (backButton)
            {
                backButton.onClick.AddListener(() =>
                {
                    mainCanvas.gameObject.SetActive(true);
                    gameObject.SetActive(false);
                });
            }
        }
    }
}
