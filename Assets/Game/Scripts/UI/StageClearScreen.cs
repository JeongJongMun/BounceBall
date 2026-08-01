using Core;
using Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    // 스테이지 클리어 화면 (기획 §22.2 — 다음 스테이지 또는 재시작 기능 제공).
    // 상점/인벤토리 열기는 프리팹의 ShopOpenButton·InventoryOpenButton에 맡긴다.
    // 스테이지 선택은 프리팹의 StageSelectOnClick + 아래 menuButton(BackToMenu)로 처리한다.
    public class StageClearScreen : UIScreen
    {
        [SerializeField] private Button nextButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        [Header("보상 표시 (UI 기획서 §6.2)")]
        [SerializeField] private TMP_Text stageCoinText;
        [SerializeField] private TMP_Text rewardCoinText;
        [SerializeField] private TMP_Text totalCoinText;
        [SerializeField] private TMP_Text ownedCoinText;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(LoadNextStage);
            if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance.BackToMenu());
        }

        private void OnEnable() => CurrencyWallet.OnCoinChanged += HandleCoinChanged;
        private void OnDisable() => CurrencyWallet.OnCoinChanged -= HandleCoinChanged;

        // 마지막 스테이지면 다음 스테이지가 없으므로 버튼을 숨긴다.
        protected override void OnShow()
        {
            if (nextButton != null) nextButton.gameObject.SetActive(GetNextStageScene() != null);
            UpdateRewardTexts();
        }

        // 스테이지 획득 / 클리어 보상 / 총 획득 골드 / 소지 골드 (UI 기획서 §6.3)
        private void UpdateRewardTexts()
        {
            var stage = FindAnyObjectByType<StageController>();
            int earned = stage != null ? stage.StageCoinEarned : 0;
            int reward = stage != null ? stage.ClearRewardCoin : 0;

            if (stageCoinText != null) stageCoinText.text = earned.ToString();
            if (rewardCoinText != null) rewardCoinText.text = reward.ToString();
            if (totalCoinText != null) totalCoinText.text = (earned + reward).ToString();
            RefreshOwnedCoin(CurrencyWallet.Coin);
        }

        private void HandleCoinChanged(int coin) => RefreshOwnedCoin(coin);

        private void RefreshOwnedCoin(int coin)
        {
            if (ownedCoinText != null) ownedCoinText.text = coin.ToString();
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetRewardTexts(TMP_Text stageCoin, TMP_Text rewardCoin, TMP_Text totalCoin, TMP_Text ownedCoin = null)
        {
            stageCoinText = stageCoin;
            rewardCoinText = rewardCoin;
            totalCoinText = totalCoin;
            ownedCoinText = ownedCoin;
        }

        private static string GetNextStageScene()
        {
            var database = Resources.Load<StageDatabase>("StageDatabase");
            if (database == null) return null;
            return database.GetNextStageScene(SceneManager.GetActiveScene().name);
        }

        private void LoadNextStage()
        {
            var next = GetNextStageScene();
            if (next == null || SceneLoader.Instance == null) return;
            SceneLoader.Instance.Load(next);
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetButtons(Button next, Button restart, Button menu)
        {
            nextButton = next;
            restartButton = restart;
            menuButton = menu;
        }
    }
}
