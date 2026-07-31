using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class CheckpointTests
    {
        private GameObject _stageGo;
        private GameObject _playerGo;
        private StageController _stage;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("Player");
            _playerGo.AddComponent<Rigidbody2D>();
            _playerGo.AddComponent<PlayerStats>();
            _playerGo.AddComponent<Player>();

            _stageGo = new GameObject("Stage");
            _stage = _stageGo.AddComponent<StageController>();
            _stage.SetGoalCounts(total: 2, required: 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_playerGo);
            Object.DestroyImmediate(_stageGo);
        }

        private static Checkpoint CreateCheckpoint(Vector3 position)
        {
            var go = new GameObject("Checkpoint");
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            go.AddComponent<SpriteRenderer>();
            return go.AddComponent<Checkpoint>();
        }

        [Test]
        public void 활성화하면_해당_체크포인트가_부활_지점이_된다()
        {
            var checkpoint = CreateCheckpoint(new Vector3(5f, 2f, 0f));

            _stage.ActivateCheckpoint(checkpoint);
            _stage.RespawnPlayer();

            Assert.AreSame(checkpoint, _stage.ActiveCheckpoint);
            Assert.IsTrue(checkpoint.IsActivated);
            Assert.AreEqual(5f, _playerGo.transform.position.x, 0.001f);
            Assert.AreEqual(2f, _playerGo.transform.position.y, 0.001f);

            Object.DestroyImmediate(checkpoint.gameObject);
        }

        [Test]
        public void 새_체크포인트를_활성화하면_이전_것은_해제된다()
        {
            var first = CreateCheckpoint(new Vector3(3f, 0f, 0f));
            var second = CreateCheckpoint(new Vector3(8f, 0f, 0f));

            _stage.ActivateCheckpoint(first);
            _stage.ActivateCheckpoint(second);

            Assert.IsFalse(first.IsActivated, "이전 체크포인트가 해제되지 않았습니다.");
            Assert.IsTrue(second.IsActivated);
            Assert.AreSame(second, _stage.ActiveCheckpoint);

            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(second.gameObject);
        }

        [Test]
        public void 이미_활성화된_체크포인트는_다시_저장하지_않는다()
        {
            var checkpoint = CreateCheckpoint(Vector3.zero);
            var itemGo = new GameObject("GoalItem");
            itemGo.AddComponent<CircleCollider2D>().isTrigger = true;
            var item = itemGo.AddComponent<GoalItem>();

            _stage.ActivateCheckpoint(checkpoint);
            item.Collect(); // 체크포인트 이후 획득

            // 같은 체크포인트를 다시 밟아도 획득 상태가 저장돼선 안 된다 (기획 §25.2)
            _stage.ActivateCheckpoint(checkpoint);
            _stage.RespawnPlayer();

            Assert.IsFalse(item.IsCollected, "재접촉으로 획득 상태가 체크포인트에 덮어써졌습니다.");
            Assert.AreEqual(0, _stage.AcquiredGoalItemCount);

            Object.DestroyImmediate(itemGo);
            Object.DestroyImmediate(checkpoint.gameObject);
        }

        [Test]
        public void 체크포인트_활성화_당시의_목표_아이템_상태가_저장된다()
        {
            var firstItemGo = new GameObject("GoalItem1");
            firstItemGo.AddComponent<CircleCollider2D>().isTrigger = true;
            var firstItem = firstItemGo.AddComponent<GoalItem>();

            var secondItemGo = new GameObject("GoalItem2");
            secondItemGo.AddComponent<CircleCollider2D>().isTrigger = true;
            var secondItem = secondItemGo.AddComponent<GoalItem>();

            firstItem.Collect(); // 체크포인트 이전 획득 → 유지돼야 한다
            var checkpoint = CreateCheckpoint(new Vector3(4f, 1f, 0f));
            _stage.ActivateCheckpoint(checkpoint);
            secondItem.Collect(); // 체크포인트 이후 획득 → 되살아나야 한다

            _stage.RespawnPlayer();

            Assert.IsTrue(firstItem.IsCollected, "체크포인트 이전 획득분이 초기화됐습니다.");
            Assert.IsFalse(secondItem.IsCollected, "체크포인트 이후 획득분이 되살아나지 않았습니다.");
            Assert.AreEqual(1, _stage.AcquiredGoalItemCount);

            Object.DestroyImmediate(firstItemGo);
            Object.DestroyImmediate(secondItemGo);
            Object.DestroyImmediate(checkpoint.gameObject);
        }
    }
}
