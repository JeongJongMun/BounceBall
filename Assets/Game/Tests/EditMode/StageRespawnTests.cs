using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class StageRespawnTests
    {
        private const float FallLimitY = -8f;

        // 사망은 낙사뿐이다 (기획 §23.1). 좌우는 투명 벽이 막으므로 판정 대상이 아니다.
        [Test]
        public void 낙사선_위는_낙사가_아니다()
        {
            Assert.IsFalse(StageController.IsFallen(0f, FallLimitY));
            Assert.IsFalse(StageController.IsFallen(FallLimitY, FallLimitY));
        }

        [Test]
        public void 낙사선_아래는_낙사다()
        {
            Assert.IsTrue(StageController.IsFallen(FallLimitY - 0.1f, FallLimitY));
        }

        [Test]
        public void 위쪽으로는_아무리_올라가도_낙사가_아니다()
        {
            // 세로로 긴 스테이지를 위해 상단은 판정에서 제외한다 (기획 §23.1)
            Assert.IsFalse(StageController.IsFallen(9999f, FallLimitY));
        }

        [Test]
        public void 부활하면_체크포인트_이후_획득한_목표_아이템이_되살아난다()
        {
            var stageGo = new GameObject("Stage");
            var stage = stageGo.AddComponent<StageController>();
            stage.SetGoalCounts(total: 2, required: 2);

            var playerGo = new GameObject("Player");
            playerGo.AddComponent<Rigidbody2D>();
            playerGo.AddComponent<PlayerStats>();
            playerGo.AddComponent<Player>();

            var itemGo = new GameObject("GoalItem");
            itemGo.AddComponent<CircleCollider2D>().isTrigger = true;
            var item = itemGo.AddComponent<GoalItem>();

            // 아무것도 획득하지 않은 상태를 체크포인트로 저장한다
            stage.SaveCheckpoint(Vector3.zero);

            item.Collect();
            Assert.AreEqual(1, stage.AcquiredGoalItemCount);
            Assert.IsTrue(item.IsCollected);

            stage.RespawnPlayer();

            Assert.IsFalse(item.IsCollected, "체크포인트 이후 획득한 아이템이 되살아나지 않았습니다.");
            Assert.AreEqual(0, stage.AcquiredGoalItemCount);
            Assert.AreEqual("0 / 2", stage.GoalProgressText);

            Object.DestroyImmediate(itemGo);
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(stageGo);
        }

        [Test]
        public void 체크포인트_이전에_획득한_아이템은_유지된다()
        {
            var stageGo = new GameObject("Stage");
            var stage = stageGo.AddComponent<StageController>();
            stage.SetGoalCounts(total: 2, required: 2);

            var playerGo = new GameObject("Player");
            playerGo.AddComponent<Rigidbody2D>();
            playerGo.AddComponent<PlayerStats>();
            playerGo.AddComponent<Player>();

            var itemGo = new GameObject("GoalItem");
            itemGo.AddComponent<CircleCollider2D>().isTrigger = true;
            var item = itemGo.AddComponent<GoalItem>();

            item.Collect();
            stage.SaveCheckpoint(Vector3.zero); // 획득 이후에 저장

            stage.RespawnPlayer();

            Assert.IsTrue(item.IsCollected, "체크포인트 이전에 획득한 아이템이 초기화됐습니다.");
            Assert.AreEqual(1, stage.AcquiredGoalItemCount);

            Object.DestroyImmediate(itemGo);
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(stageGo);
        }
    }
}
