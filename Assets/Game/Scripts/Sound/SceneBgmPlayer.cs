using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    // 씬에 맞는 BGM을 자동으로 튼다 (사운드 기획: 메인·스테이지 선택 = Main_BGM, 모든 스테이지 = Stage_BGM).
    // Systems 프리팹에 얹혀 씬 전환에도 살아남는다.
    public class SceneBgmPlayer : MonoBehaviour
    {
        [SerializeField] private string menuSceneName = "MainMenu";

        private void Start() => Apply(SceneManager.GetActiveScene());

        private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

        private void Apply(Scene scene)
        {
            var bgm = ResolveBgm(scene.name);
            if (bgm != SoundId.None) Sound.PlayBgm(bgm);
        }

        // 메뉴 씬이면 Main, 스테이지 목록에 있으면 Stage. 그 외(데모 등)는 건드리지 않는다.
        private SoundId ResolveBgm(string sceneName)
        {
            if (sceneName == menuSceneName) return SoundId.Main_BGM;

            var database = Resources.Load<StageDatabase>("StageDatabase");
            if (database == null) return SoundId.None;

            foreach (var stage in database.Stages)
            {
                if (stage != null && stage.sceneName == sceneName) return SoundId.Stage_BGM;
            }
            return SoundId.None;
        }
    }
}
