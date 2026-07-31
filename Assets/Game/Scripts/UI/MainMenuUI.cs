using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 메인 메뉴 씬 UI. StageDatabase를 읽어 스테이지 버튼을 런타임 생성한다.
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Button stageButtonTemplate;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            quitButton.onClick.AddListener(Application.Quit);
        }

        private void Start()
        {
            var database = Resources.Load<StageDatabase>("StageDatabase");
            if (database == null || database.Stages.Count == 0)
            {
                Debug.LogWarning("[Game] StageDatabase가 비어 있습니다. Game > Sync Stage Database를 실행하세요.");
                return;
            }

            stageButtonTemplate.gameObject.SetActive(false);
            foreach (var stage in database.Stages)
            {
                var button = Instantiate(stageButtonTemplate, buttonContainer);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = stage.displayName;
                string sceneName = stage.sceneName;
                button.onClick.AddListener(() => SceneLoader.Instance.Load(sceneName));
            }
        }
    }
}
