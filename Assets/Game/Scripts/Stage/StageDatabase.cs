using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // 스테이지 목록의 단일 출처. 메뉴 UI의 버튼 생성, 다음 스테이지 진행, Build Settings 동기화가 이걸 참조한다.
    [CreateAssetMenu(menuName = "Game/Stage Database", fileName = "StageDatabase")]
    public class StageDatabase : ScriptableObject
    {
        [Serializable]
        public class StageEntry
        {
            public string sceneName;
            public string displayName;
        }

        [SerializeField] private List<StageEntry> stages = new();

        public IReadOnlyList<StageEntry> Stages => stages;

        public void SetStages(List<StageEntry> entries) => stages = entries;

        // 다음 스테이지 씬 이름. 마지막이거나 목록에 없으면 null.
        public string GetNextStageScene(string currentSceneName)
        {
            for (int i = 0; i < stages.Count - 1; i++)
            {
                if (stages[i].sceneName == currentSceneName)
                    return stages[i + 1].sceneName;
            }
            return null;
        }
    }
}
