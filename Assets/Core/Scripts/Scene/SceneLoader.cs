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

        public void Load(string sceneName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void Reload() => Load(SceneManager.GetActiveScene().name);

        private IEnumerator LoadRoutine(string sceneName)
        {
            _isLoading = true;
            fadeGroup.blocksRaycasts = true;
            yield return fadeGroup.DOFade(1f, fadeDuration).SetUpdate(true).WaitForCompletion();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return fadeGroup.DOFade(0f, fadeDuration).SetUpdate(true).WaitForCompletion();
            fadeGroup.blocksRaycasts = false;
            _isLoading = false;
        }
    }
}
