using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class SceneLoader : Singleton<SceneLoader>
    {
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeDuration = 0.3f;

        private bool _isLoading;

        public bool IsLoading => _isLoading;
        public float FadeDuration => fadeDuration;

        public void Load(string sceneName, Action afterFadeIn = null)
        {
            if (_isLoading) return;
            StartCoroutine(LoadRoutine(sceneName, afterFadeIn));
        }

        public void Reload(Action afterFadeIn = null) => Load(SceneManager.GetActiveScene().name, afterFadeIn);

        private IEnumerator LoadRoutine(string sceneName, Action afterFadeIn)
        {
            _isLoading = true;
            fadeGroup.blocksRaycasts = true;
            fadeGroup.DOKill();
            yield return fadeGroup.DOFade(1f, fadeDuration).SetEase(Ease.InOutQuad).SetUpdate(true).WaitForCompletion();

            afterFadeIn?.Invoke();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(sceneName);

            // 로드 직후 unscaledDeltaTime 스파이크로 페이드가 한 프레임에 끝나지 않게 한 박자 둔다.
            fadeGroup.alpha = 1f;
            yield return null;

            yield return fadeGroup.DOFade(0f, fadeDuration).SetEase(Ease.InOutQuad).SetUpdate(true).WaitForCompletion();
            fadeGroup.blocksRaycasts = false;
            _isLoading = false;
        }
    }
}
