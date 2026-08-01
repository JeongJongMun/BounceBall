using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    // Stages 폴더의 씬을 StageDatabase에 등록하고 Build Settings를 재구성한다.
    public static class StageDatabaseTools
    {
        private const string DatabasePath = "Assets/Game/Resources/StageDatabase.asset";
        private const string StagesDir = "Assets/Game/Scenes/Stages";
        private const string MenuScenePath = "Assets/Game/Scenes/MainMenu.unity";

        // StageAssetPostprocessor가 씬 변경 시 자동 호출한다. 수동 실행: CLI -executeMethod
        public static void Sync()
        {
            var database = GetOrCreateDatabase();

            // 씬 스캔 (기존 displayName 보존)
            var previous = database.Stages.ToDictionary(s => s.sceneName, s => s.displayName);
            var entries = new List<StageDatabase.StageEntry>();
            if (AssetDatabase.IsValidFolder(StagesDir))
            {
                var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { StagesDir })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(p => p)
                    .ToList();
                foreach (var path in scenePaths)
                {
                    var sceneName = Path.GetFileNameWithoutExtension(path);
                    entries.Add(new StageDatabase.StageEntry
                    {
                        sceneName = sceneName,
                        displayName = previous.TryGetValue(sceneName, out var kept) && !string.IsNullOrEmpty(kept)
                            ? kept
                            : sceneName
                    });
                }
            }
            database.SetStages(entries);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            // Build Settings 재구성: [MainMenu, ...스테이지] — 잔재 씬은 자동 제거된다
            var buildScenes = new List<EditorBuildSettingsScene>();
            if (File.Exists(MenuScenePath))
                buildScenes.Add(new EditorBuildSettingsScene(MenuScenePath, true));
            foreach (var entry in entries)
                buildScenes.Add(new EditorBuildSettingsScene($"{StagesDir}/{entry.sceneName}.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            Debug.Log($"[Game] StageDatabase 동기화 완료 — 스테이지 {entries.Count}개, Build Settings {buildScenes.Count}개 씬");
        }

        private static StageDatabase GetOrCreateDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<StageDatabase>(DatabasePath);
            if (database != null) return database;

            database = ScriptableObject.CreateInstance<StageDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            return database;
        }
    }
}
