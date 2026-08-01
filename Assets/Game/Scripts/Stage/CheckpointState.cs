using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // 체크포인트 저장 데이터 (기획 §25.3). 부활 시 여기 담긴 항목만 되돌린다 (§24.2).
    // 지금은 스테이지 시작 지점이 유일한 기본 체크포인트지만, Checkpoint 컴포넌트가 그대로 재사용한다.
    public class CheckpointState
    {
        public Vector3 Position { get; }
        public PropertyData Property { get; }

        // 저장 시점에 이미 획득돼 있던 목표 아이템. 여기 없는 것은 부활 시 되살아난다.
        public HashSet<GoalItem> CollectedGoals { get; }

        // 저장 시점에 이미 획득돼 있던 성질 아이템. 목표 아이템과 동일한 규칙으로 복구한다 (기획 §11.5).
        public HashSet<PropertyItem> AcquiredPropertyItems { get; }

        public int AcquiredGoalItemCount => CollectedGoals.Count;

        public CheckpointState(
            Vector3 position,
            PropertyData property,
            IEnumerable<GoalItem> collectedGoals,
            IEnumerable<PropertyItem> acquiredPropertyItems = null)
        {
            Position = position;
            Property = property;
            CollectedGoals = collectedGoals != null ? new HashSet<GoalItem>(collectedGoals) : new HashSet<GoalItem>();
            AcquiredPropertyItems = acquiredPropertyItems != null
                ? new HashSet<PropertyItem>(acquiredPropertyItems)
                : new HashSet<PropertyItem>();
        }
    }
}
