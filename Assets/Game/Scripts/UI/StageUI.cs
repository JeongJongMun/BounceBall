using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // StageDatabase를 읽어 스테이지 버튼을 런타임 생성한다.
    public class StageUI : MonoBehaviour
    {
        [SerializeField] private Transform stageButtonContainer;
        [SerializeField] private Button stageButtonTemplate;
        [SerializeField] private Button backButton;
        
        [SerializeField] Canvas mainCanvas;
        

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

        void Start()
        {
            var database = Resources.Load<StageDatabase>("StageDatabase");
            if (database == null || database.Stages.Count == 0)
            {
                Debug.LogWarning("[Game] StageDatabase가 비어 있습니다. 스테이지 씬을 만들어 저장하면 자동 등록됩니다.");
                return;
            }

            stageButtonTemplate.gameObject.SetActive(false);
            foreach (var stage in database.Stages)
            {
                var button = Instantiate(stageButtonTemplate, stageButtonContainer);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = stage.displayName;
                var sceneName = stage.sceneName;
                button.onClick.AddListener(() => SceneLoader.Instance.Load(sceneName));
            }
        }
    }
}
