using System.Collections.Generic;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game
{
    // 상점 창 (인벤토리 문서 §7, UI 기획서 §4). 스테이지 선택 화면에서만 열 수 있다 (§7.1).
    public class ShopWindow : Singleton<ShopWindow>
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform productContainer;
        [SerializeField] private ShopProductView productTemplate;
        [SerializeField] private Button closeButton;

        [Header("상세 · 구매")]
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailDescriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text ownedCoinText;
        [SerializeField] private Button buyButton;

        [Header("금액 색상 (문서 §7.5)")]
        [SerializeField] private Color affordableColor = new(0.1f, 0.1f, 0.1f);
        [SerializeField] private Color insufficientColor = new(0.85f, 0.2f, 0.2f);

        private readonly List<ShopProductView> _products = new();
        private ItemDatabase _database;
        private ItemData _selected;
        private UiClickSoundSource _buySound;

        public bool IsOpen => root != null && root.activeSelf;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            _database = ItemDatabase.Load();
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (buyButton != null) buyButton.onClick.AddListener(Purchase);
            if (root != null) root.SetActive(false);
        }

        private void OnEnable() => CurrencyWallet.OnCoinChanged += HandleCoinChanged;
        private void OnDisable() => CurrencyWallet.OnCoinChanged -= HandleCoinChanged;

        protected override void OnDestroy()
        {
            UIPopupState.SetOpen(this, false);
            base.OnDestroy();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.pKey.wasPressedThisFrame) Toggle();
            else if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (root == null || !CanOpenHere()) return;

            root.SetActive(true);
            UIPopupState.SetOpen(this, true);
            BuildProductList();
        }

        public void Close()
        {
            if (root == null) return;
            root.SetActive(false);
            UIPopupState.SetOpen(this, false);
        }

        // 인게임 플레이 중에는 상점을 열 수 없다 (문서 §7.1).
        // 스테이지 선택 화면(Ready)과 클리어 화면(Cleared)에서만 연다.
        private static bool CanOpenHere()
        {
            var manager = GameManager.Instance;
            return manager == null || manager.State == GameState.Ready || manager.State == GameState.Cleared;
        }

        private void BuildProductList()
        {
            foreach (var view in _products) Destroy(view.gameObject);
            _products.Clear();

            _database ??= ItemDatabase.Load();
            if (_database == null || productTemplate == null) return;

            productTemplate.gameObject.SetActive(false);

            foreach (var item in _database.ShopProducts)
            {
                if (item == null) continue;

                var view = Instantiate(productTemplate, productContainer);
                view.gameObject.SetActive(true);
                view.Bind(item);
                view.Clicked += HandleProductClicked;
                _products.Add(view);
            }

            Select(_products.Count > 0 ? _products[0].Item : null);
        }

        private void HandleProductClicked(ShopProductView view) => Select(view.Item);

        private void Select(ItemData item)
        {
            _selected = item;

            foreach (var view in _products) view.SetSelected(view.Item == item);
            RefreshDetail();
        }

        private void HandleCoinChanged(int coin) => RefreshDetail();

        private void RefreshDetail()
        {
            if (ownedCoinText != null) ownedCoinText.text = CurrencyWallet.Coin.ToString();

            if (_selected == null)
            {
                if (detailNameText != null) detailNameText.text = "";
                if (detailDescriptionText != null) detailDescriptionText.text = "";
                if (priceText != null) priceText.text = "";
                return;
            }

            bool affordable = Shop.CanAfford(_selected, 1);

            // 살 수 없는 상태면 구매 버튼이 클릭음 대신 UI_Error를 낸다
            SetBuySound(affordable ? SoundId.UI_Click : SoundId.UI_Error);

            if (detailNameText != null) detailNameText.text = _selected.ItemName;
            if (detailDescriptionText != null) detailDescriptionText.text = _selected.Description;

            // 보유 금액보다 비싸면 가격을 빨간색으로 표시한다 (문서 §7.5)
            if (priceText != null)
            {
                priceText.text = _selected.Price.ToString();
                priceText.color = affordable ? affordableColor : insufficientColor;
            }
        }

        private void SetBuySound(SoundId sound)
        {
            if (buyButton == null) return;

            if (_buySound == null) _buySound = UiClickSound.Ensure(buyButton.gameObject);
            if (_buySound != null) _buySound.sound = sound;
        }

        // 버튼을 누르면 바로 1개 구매한다. 코인이 모자라면 토스트로 알린다.
        private void Purchase()
        {
            if (_selected == null) return;

            if (!Shop.TryPurchase(_selected, 1))
            {
                // UI_Error는 버튼을 누르는 순간 이미 났다 (SetBuySound)
                ToastManager.Show("돈이 부족합니다");
                return;
            }

            ToastManager.Show($"{KoreanParticle.WithObject(_selected.ItemName)} 구매하였습니다.");
            RefreshDetail(); // 보유 금액·색상 즉시 갱신 (문서 §7.5)
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(GameObject windowRoot, Transform container, ShopProductView template, Button close)
        {
            root = windowRoot;
            productContainer = container;
            productTemplate = template;
            closeButton = close;
        }

        public void SetDetailReferences(TMP_Text detailName, TMP_Text detailDescription,
            TMP_Text price, TMP_Text ownedCoin, Button buy)
        {
            detailNameText = detailName;
            detailDescriptionText = detailDescription;
            priceText = price;
            ownedCoinText = ownedCoin;
            buyButton = buy;
        }
    }
}
