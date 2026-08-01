using System.Linq;
using UnityEditor;

namespace Game.EditorTools
{
    // Stages 폴더의 씬이 추가·저장·삭제·이동되면 StageDatabase와 Build Settings를 자동 동기화한다.
    // 덕분에 기획자는 New Stage로 씬만 만들면 메뉴 버튼과 빌드 목록에 바로 반영된다.
    public class StageAssetPostprocessor : AssetPostprocessor
    {
        private const string StagesPrefix = "Assets/Game/Scenes/Stages/";

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
