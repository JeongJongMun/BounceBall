using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    // Stages 폴더의 씬이 추가·저장·삭제·이동되면 StageDatabase와 Build Settings를 자동 동기화한다.
    // 덕분에 기획자는 New Stage로 씬만 만들면 메뉴 버튼과 빌드 목록에 바로 반영된다.
    [InitializeOnLoad]
    public class StageAssetPostprocessor : AssetPostprocessor
    {
        private const string StagesPrefix = "Assets/Game/Scenes/Stages/";
        private const string SessionKey = "Game.StageDbSyncedOnLaunch";

        // 에디터 시작 시 1회 동기화 — Unity 밖(git pull, 탐색기)에서 씬이 사라지거나 생겨
        // postprocessor가 변경을 못 본 경우에도 목록이 자가 치유된다.
        static StageAssetPostprocessor()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionKey, false)) return;
                SessionState.SetBool(SessionKey, true);
                StageDatabaseTools.Sync();
            };
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool stageChanged = importedAssets
                .Concat(deletedAssets)
                .Concat(movedAssets)
                .Concat(movedFromAssetPaths)
                .Any(path => path.StartsWith(StagesPrefix) && path.EndsWith(".unity"));
            if (!stageChanged) return;

            // 임포트 파이프라인 안에서 빌드 세팅을 건드리지 않도록 지연 실행
            EditorApplication.delayCall += StageDatabaseTools.Sync;
        }
    }
}
