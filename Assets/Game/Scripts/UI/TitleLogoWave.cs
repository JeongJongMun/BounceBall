using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    // 타이틀 로고 클릭 시 효과음과 스쿼시·스트레치. 물결 디스토션은 UI/WaveDistort 머티리얼이 담당한다.
    public class TitleLogoWave : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private SoundId sound = SoundId.Lick;
        // Lick SFX(~0.44s)에 맞춰 전체 연출이 끝나도록 잡는다.
        [SerializeField] private float wideX = 1.07f;
        [SerializeField] private float wideY = 0.95f;
        [SerializeField] private float tallX = 0.95f;
        [SerializeField] private float tallY = 1.07f;
        [SerializeField] private float wideDuration = 0.1f;
        [SerializeField] private float tallDuration = 0.14f;
        [SerializeField] private float settleDuration = 0.2f;

        private Vector3 _baseScale = Vector3.one;
        private bool _playing;

        private void Awake() => _baseScale = transform.localScale;

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = _baseScale;
            _playing = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_playing) return;

            Sound.Play(sound);
            PlayClickScale();
        }

        private void PlayClickScale()
        {
            _playing = true;
            transform.DOKill();
            transform.localScale = _baseScale;

            var wide = new Vector3(_baseScale.x * wideX, _baseScale.y * wideY, _baseScale.z);
            var tall = new Vector3(_baseScale.x * tallX, _baseScale.y * tallY, _baseScale.z);

            DOTween.Sequence()
                .SetTarget(transform)
                .SetUpdate(true)
                .SetLink(gameObject)
                .Append(transform.DOScale(wide, wideDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(tall, tallDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(_baseScale, settleDuration).SetEase(Ease.OutBack))
                .OnComplete(() => _playing = false)
                .OnKill(() =>
                {
                    _playing = false;
                    transform.localScale = _baseScale;
                });
        }
    }
}
