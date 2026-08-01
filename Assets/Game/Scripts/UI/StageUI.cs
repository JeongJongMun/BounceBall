using System.Collections.Generic;
using Core;
using DG.Tweening;
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
        [Tooltip("잠긴 스테이지를 눌렀을 때 표시할 안내 팝업")]
        [SerializeField] private LockedStagePopup lockedStagePopup;

        [SerializeField] Canvas mainCanvas;

        [SerializeField] private float introDuration = 0.65f;
        [SerializeField] private float introDropMultiplier = 1.15f;
        [SerializeField] private float introTiltAngle = 8f;

        private readonly List<GameObject> _spawnedButtons = new();
        private RectTransform _content;

        private void Awake()
        {
            _content = EnsureDropContent();

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
        private void OnEnable()
        {
            Refresh();
            PlayDropIntro();
        }

        private void OnDisable()
        {
            _content.DOKill();
            _content.anchoredPosition = Vector2.zero;
            _content.localRotation = Quaternion.identity;
        }

        // Overlay 루트 Canvas는 위치가 매 프레임 덮어써지므로, 자식을 감싼 콘텐츠를 떨어뜨린다.
        private RectTransform EnsureDropContent()
        {
            var root = (RectTransform)transform;
            var existing = root.Find("DropContent") as RectTransform;
            if (existing != null) return existing;

            var go = new GameObject("DropContent", typeof(RectTransform));
            var content = (RectTransform)go.transform;
            content.SetParent(root, false);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.localScale = Vector3.one;

            var children = new List<Transform>(root.childCount);
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child != content) children.Add(child);
            }

            for (int i = 0; i < children.Count; i++)
                children[i].SetParent(content, false);

            return content;
        }

        // 살짝 기울어진 채로 수직 낙하한 뒤, 튕기며 바로 선다.
        private void PlayDropIntro()
        {
            _content.DOKill();
            Canvas.ForceUpdateCanvases();

            float height = ((RectTransform)transform).rect.height;
            if (height < 1f) height = 1080f;

            float tilt = introTiltAngle * (Random.value < 0.5f ? -1f : 1f);
            _content.anchoredPosition = new Vector2(0f, height * introDropMultiplier);
            _content.localRotation = Quaternion.Euler(0f, 0f, tilt);

            DOTween.Sequence()
                .SetTarget(_content)
                .SetUpdate(true)
                .Append(_content.DOAnchorPosY(0f, introDuration).SetEase(Ease.OutBounce))
                .Join(_content.DOLocalRotate(Vector3.zero, introDuration).SetEase(Ease.OutBounce));
        }

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
                var state = cleared[i] ? StageButtonState.Cleared
                    : unlocked ? StageButtonState.Playable
                    : StageButtonState.Locked;

                var button = Instantiate(stageButtonTemplate, stageButtonContainer);
                button.gameObject.SetActive(true);

                var view = button.GetComponent<StageButtonView>();
                if (view != null) view.SetDisplay(i + 1, state);

                if (unlocked)
                {
                    var sceneName = database.Stages[i].sceneName;
                    button.onClick.AddListener(() => SceneLoader.Instance.Load(sceneName));
                }
                else
                {
                    // 잠긴 스테이지는 시작하지 않고 안내 팝업만 띄운다 (UI 기획서 §2.6).
                    // 실패 알림이므로 클릭음 대신 UI_Error를 낸다 (누르는 순간).
                    var soundSource = UiClickSound.Ensure(button.gameObject);
                    if (soundSource != null) soundSource.sound = SoundId.UI_Error;

                    button.onClick.AddListener(() =>
                    {
                        if (lockedStagePopup != null) lockedStagePopup.Show();
                    });
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
