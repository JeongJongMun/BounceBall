using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 스테이지 버튼의 상태 (UI 기획서 §2.4)
    public enum StageButtonState { Cleared, Playable, Locked }

    // 스테이지 선택 버튼 하나의 표시를 담당한다.
    // 번호는 자리수 이미지로 넣고, Clear/Lock 아이콘과 루트 색으로 상태를 표현한다.
    public class StageButtonView : MonoBehaviour
    {
        [SerializeField] private Image rootImage;
        [SerializeField] private Image digitImage1;
        [SerializeField] private Image digitImage2;
        [SerializeField] private GameObject stageNumberRoot;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject clearIcon;

        [Tooltip("0~9 순서의 숫자 스프라이트 (Img_Stage_Slot_No_0 ~ No_9)")]
        [SerializeField] private List<Sprite> digitSprites = new();

        [SerializeField] private Color lockedRootColor = new Color32(0x9a, 0x9a, 0x9a, 0xff);
        [SerializeField] private float lockedShakeDuration = 0.55f;
        [SerializeField] private float lockedShakeStrength = 42f;

        private Color _normalRootColor;
        private bool _hasNormalRootColor;
        private Vector2 _restPos;
        private bool _hasRestPos;

        private void OnDisable()
        {
            var rt = (RectTransform)transform;
            rt.DOKill();
            if (_hasRestPos) rt.anchoredPosition = _restPos;
        }

        public void SetDisplay(int stageNumber, StageButtonState state)
        {
            ApplyStageNumber(stageNumber);

            bool locked = state == StageButtonState.Locked;
            stageNumberRoot.SetActive(!locked);
            lockIcon.SetActive(locked);
            clearIcon.SetActive(state == StageButtonState.Cleared);

            if (!_hasNormalRootColor)
            {
                _normalRootColor = rootImage.color;
                _hasNormalRootColor = true;
            }
            rootImage.color = locked ? lockedRootColor : _normalRootColor;

            // 잠긴 버튼도 클릭해 흔들림 피드백을 줘야 하므로 interactable을 끄지 않는다.
            // 대신 Hover 색 변화를 없애 "선택 불가"임을 알린다 (UI 기획서 §2.4).
            if (TryGetComponent<Button>(out var button))
            {
                var colors = button.colors;
                colors.highlightedColor = locked
                    ? colors.normalColor
                    : Color.white;
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
            }
        }

        // 잠긴 스테이지를 눌렀을 때 좌우로 짧게 흔든다.
        public void PlayLockedShake()
        {
            var rt = (RectTransform)transform;
            if (!_hasRestPos)
            {
                _restPos = rt.anchoredPosition;
                _hasRestPos = true;
            }

            float s = lockedShakeStrength;
            float step = lockedShakeDuration / 5.5f;

            rt.DOKill();
            rt.anchoredPosition = _restPos;
            DOTween.Sequence()
                .SetTarget(rt)
                .SetUpdate(true)
                .SetLink(gameObject)
                .Append(rt.DOAnchorPosX(_restPos.x + s, step).SetEase(Ease.OutBack))
                .Append(rt.DOAnchorPosX(_restPos.x - s, step).SetEase(Ease.OutBack))
                .Append(rt.DOAnchorPosX(_restPos.x + s * 0.6f, step).SetEase(Ease.OutBack))
                .Append(rt.DOAnchorPosX(_restPos.x - s * 0.35f, step).SetEase(Ease.OutBack))
                .Append(rt.DOAnchorPosX(_restPos.x, step * 1.5f).SetEase(Ease.OutBack))
                .OnKill(() => rt.anchoredPosition = _restPos);
        }

        private void ApplyStageNumber(int stageNumber)
        {
            if (stageNumber < 10)
            {
                digitImage1.sprite = digitSprites[stageNumber];
                digitImage1.gameObject.SetActive(true);
                digitImage2.gameObject.SetActive(false);
                return;
            }

            digitImage1.sprite = digitSprites[stageNumber / 10];
            digitImage2.sprite = digitSprites[stageNumber % 10];
            digitImage1.gameObject.SetActive(true);
            digitImage2.gameObject.SetActive(true);
        }
    }
}
