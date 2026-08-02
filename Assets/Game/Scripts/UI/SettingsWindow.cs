using Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game
{
    // 설정 창 (UI 기획서 §8). 메인 화면과 일시정지에서 공용으로 쓴다.
    // 컴포넌트가 창 본체에 붙어 있어, 다른 곳에서 SetActive(true)로 열어도 값이 채워진다.
    public class SettingsWindow : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [SerializeField] private TMP_Text masterValueText;
        [SerializeField] private TMP_Text bgmValueText;
        [SerializeField] private TMP_Text sfxValueText;

        [SerializeField] private Button closeButton;

        [Header("데이터 초기화 (디버그)")]
        [Tooltip("끄면 버튼을 숨긴다")]
        [SerializeField] private bool showResetButton = true;
        [SerializeField] private Button resetButton;

        private bool _applying;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (resetButton != null) resetButton.onClick.AddListener(ResetData);

            if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        // 열릴 때마다 현재 저장값을 읽어 슬라이더에 반영한다.
        private void OnEnable()
        {
            UIPopupState.SetOpen(this, true);
            ApplyResetButtonVisibility();

            _applying = true;
            if (masterSlider != null) masterSlider.value = GetVolume(VolumeKind.Master);
            if (bgmSlider != null) bgmSlider.value = GetVolume(VolumeKind.Bgm);
            if (sfxSlider != null) sfxSlider.value = GetVolume(VolumeKind.Sfx);
            _applying = false;

            RefreshLabels();
        }

        private void OnDisable() => UIPopupState.SetOpen(this, false);

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Open() => gameObject.SetActive(true);
        public void Close() => gameObject.SetActive(false);

        private void ApplyResetButtonVisibility()
        {
            if (resetButton != null) resetButton.gameObject.SetActive(showResetButton);
        }

        // 저장된 데이터를 모두 지우고 메인 메뉴로 돌아간다 (디버그용).
        private void ResetData()
        {
            GameDataReset.ResetAll();
            ToastManager.Show("데이터를 초기화했습니다.");

            Close();
            if (GameManager.Instance != null) GameManager.Instance.BackToMenu();
        }

        private enum VolumeKind { Master, Bgm, Sfx }

        // AudioManager가 있으면 그쪽을 통해(즉시 반영), 없으면 저장값을 직접 읽고 쓴다.
        private static float GetVolume(VolumeKind kind)
        {
            var audio = AudioManager.Instance;
            switch (kind)
            {
                case VolumeKind.Master: return audio != null ? audio.MasterVolume : SaveData.MasterVolume;
                case VolumeKind.Bgm: return audio != null ? audio.BgmVolume : SaveData.BgmVolume;
                default: return audio != null ? audio.SfxVolume : SaveData.SfxVolume;
            }
        }

        private static void SetVolume(VolumeKind kind, float value)
        {
            var audio = AudioManager.Instance;
            switch (kind)
            {
                case VolumeKind.Master:
                    if (audio != null) audio.MasterVolume = value; else SaveData.MasterVolume = value;
                    break;
                case VolumeKind.Bgm:
                    if (audio != null) audio.BgmVolume = value; else SaveData.BgmVolume = value;
                    break;
                default:
                    if (audio != null) audio.SfxVolume = value; else SaveData.SfxVolume = value;
                    break;
            }
        }

        private void OnMasterChanged(float value) => Apply(VolumeKind.Master, value);
        private void OnBgmChanged(float value) => Apply(VolumeKind.Bgm, value);
        private void OnSfxChanged(float value) => Apply(VolumeKind.Sfx, value);

        private void Apply(VolumeKind kind, float value)
        {
            if (_applying) return; // 창을 열며 값을 채우는 중에는 저장하지 않는다
            SetVolume(kind, value);
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            SetPercent(masterValueText, masterSlider);
            SetPercent(bgmValueText, bgmSlider);
            SetPercent(sfxValueText, sfxSlider);
        }

        private static void SetPercent(TMP_Text text, Slider slider)
        {
            if (text == null || slider == null) return;
            text.text = Mathf.RoundToInt(slider.value * 100f).ToString();
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetResetReferences(Button reset)
        {
            resetButton = reset;
        }

        public void SetReferences(Slider master, Slider bgm, Slider sfx,
            TMP_Text masterValue, TMP_Text bgmValue, TMP_Text sfxValue, Button close)
        {
            masterSlider = master;
            bgmSlider = bgm;
            sfxSlider = sfx;
            masterValueText = masterValue;
            bgmValueText = bgmValue;
            sfxValueText = sfxValue;
            closeButton = close;
        }
    }
}
