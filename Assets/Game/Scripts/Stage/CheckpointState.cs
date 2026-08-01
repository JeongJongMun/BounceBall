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

        // 코인도 같은 규칙을 따른다 (인벤토리 문서 §6.5).
        // 저장 이후 먹은 코인은 되살아나고, 보유 코인도 저장 시점 잔액으로 복구된다.
        public HashSet<CoinItem> CollectedCoins { get; }
        public int CoinBalance { get; }
        public int StageCoinEarned { get; }

        public int AcquiredGoalItemCount => CollectedGoals.Count;

        public CheckpointState(Vector3 position, PropertyData property, IEnumerable<GoalItem> collectedGoals,
            IEnumerable<CoinItem> collectedCoins, int coinBalance, int stageCoinEarned)
        {
            Position = position;
            Property = property;
            CollectedGoals = collectedGoals != null ? new HashSet<GoalItem>(collectedGoals) : new HashSet<GoalItem>();
            CollectedCoins = collectedCoins != null ? new HashSet<CoinItem>(collectedCoins) : new HashSet<CoinItem>();
            CoinBalance = coinBalance;
            StageCoinEarned = stageCoinEarned;
        }
    }
}
