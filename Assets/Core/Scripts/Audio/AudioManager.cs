using DG.Tweening;
using UnityEngine;

namespace Core
{
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSourceA;
        [SerializeField] private AudioSource bgmSourceB;
        [SerializeField] private float bgmCrossfadeDuration = 1f;
        [SerializeField] private Vector2 sfxPitchRange = new(0.95f, 1.05f);

        private AudioSource _activeBgm;

        public float MasterVolume
        {
            get => SaveData.MasterVolume;
            set { SaveData.MasterVolume = Mathf.Clamp01(value); AudioListener.volume = SaveData.MasterVolume; }
        }

        public float BgmVolume
        {
            get => SaveData.BgmVolume;
            set
            {
                SaveData.BgmVolume = Mathf.Clamp01(value);
                if (_activeBgm != null) _activeBgm.volume = SaveData.BgmVolume;
            }
        }

        public float SfxVolume
        {
            get => SaveData.SfxVolume;
            set => SaveData.SfxVolume = Mathf.Clamp01(value);
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            AudioListener.volume = SaveData.MasterVolume;
            _activeBgm = bgmSourceA;
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            sfxSource.pitch = Random.Range(sfxPitchRange.x, sfxPitchRange.y);
            sfxSource.PlayOneShot(clip, volumeScale * SfxVolume);
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || (_activeBgm.clip == clip && _activeBgm.isPlaying)) return;

            var next = _activeBgm == bgmSourceA ? bgmSourceB : bgmSourceA;
            next.clip = clip;
            next.volume = 0f;
            next.loop = true;
            next.Play();
            next.DOFade(BgmVolume, bgmCrossfadeDuration).SetUpdate(true);

            var previous = _activeBgm;
            previous.DOFade(0f, bgmCrossfadeDuration).SetUpdate(true)
                .OnComplete(previous.Stop);

            _activeBgm = next;
        }

        public void StopBGM()
        {
            var bgm = _activeBgm;
            bgm.DOFade(0f, bgmCrossfadeDuration).SetUpdate(true).OnComplete(bgm.Stop);
        }
    }
}
