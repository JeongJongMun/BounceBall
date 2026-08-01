using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // StageDatabase를 읽어 스테이지 버튼을 런타임 생성한다.
    // 버튼에는 번호와 클리어 여부를 표시하고, 미클리어 스테이지는 번호가 가장 낮은 것만 선택할 수 있다.
    public class StageUI : MonoBehaviour
    {
        [SerializeField] private Transform stageButtonContainer;
        [SerializeField] private Button stageButtonTemplate;
        [SerializeField] private Button backButton;

        [SerializeField] Canvas mainCanvas;

        private readonly List<GameObject> _spawnedButtons = new();

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

        // 스테이지를 클리어하고 돌아왔을 때도 최신 상태가 보이도록 화면이 열릴 때마다 다시 그린다.
        private void OnEnable() => Refresh();

        private void Refresh()
        {
            foreach (var go in _spawnedButtons) Destroy(go);
            _spawnedButtons.Clear();

            var database = Resources.Load<StageDatabase>("StageDatabase");
            if (database == null || database.Stages.Count == 0)
            {
                Debug.LogWarning("[Game] StageDatabase가 비어 있습니다. 스테이지 씬을 만들어 저장하면 자동 등록됩니다.");
                return;
            }

            stageButtonTemplate.gameObject.SetActive(false);

            if (stageButtonTemplate.GetComponent<StageButtonView>() == null)
                Debug.LogWarning("[Game] StageButtonTemplate에 StageButtonView가 없습니다. 번호와 클리어 상태가 표시되지 않습니다.");

            var cleared = new List<bool>(database.Stages.Count);
            foreach (var stage in database.Stages)
                cleared.Add(StageProgress.IsCleared(stage.sceneName));

            int firstUncleared = FindFirstUnclearedIndex(cleared);

            for (int i = 0; i < database.Stages.Count; i++)
            {
                // 미클리어는 가장 앞선 하나만 열어 준다 (그 앞은 모두 클리어된 상태다).
                bool unlocked = cleared[i] || i == firstUncleared;

                var button = Instantiate(stageButtonTemplate, stageButtonContainer);
                button.gameObject.SetActive(true);
                button.interactable = unlocked;

                var view = button.GetComponent<StageButtonView>();
                if (view != null) view.SetDisplay(i + 1, cleared[i]);

                if (unlocked)
                {
                    var sceneName = database.Stages[i].sceneName;
                    button.onClick.AddListener(() => SceneLoader.Instance.Load(sceneName));
                }

                _spawnedButtons.Add(button.gameObject);
            }
        }

        // 순서상 처음으로 미클리어인 스테이지의 인덱스. 전부 클리어했으면 -1.
        public static int FindFirstUnclearedIndex(IReadOnlyList<bool> clearedFlags)
        {
            for (int i = 0; i < clearedFlags.Count; i++)
            {
                if (!clearedFlags[i]) return i;
            }
            return -1;
        }
    }
}
