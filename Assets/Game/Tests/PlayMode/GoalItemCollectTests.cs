using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class GoalItemCollectTests
    {
        private GameObject _playerGo;
        private GameObject _stageGo;
        private GameObject _itemGo;
        private Player _player;
        private StageController _stage;
        private GoalItem _item;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                Object.Destroy(systems);
                yield return null;
            }

            // StageController.Start()의 플레이어 스폰은 씬에 Player가 이미 있으면 건너뛴다.
            // 중복 스폰을 막기 위해 플레이어를 먼저 만든다.
            _playerGo = new GameObject("Player");
            _playerGo.transform.position = Vector3.zero;
            var body = _playerGo.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 0f; // 획득 판정만 검증하므로 낙하는 배제
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _playerGo.AddComponent<PlayerStats>();
            _player = _playerGo.AddComponent<Player>();

            _stageGo = new GameObject("Stage");
            _stage = _stageGo.AddComponent<StageController>();
            _stage.SetGoalCounts(total: 2, required: 2);

            _itemGo = new GameObject("GoalItem");
            _itemGo.transform.position = Vector3.zero; // 플레이어와 겹치게 배치해 접촉시킨다
            var collider = _itemGo.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;
            _itemGo.AddComponent<SpriteRenderer>();
            _item = _itemGo.AddComponent<GoalItem>();

            yield return null; // Awake/Start
            yield return new WaitForFixedUpdate(); // 트리거 감지
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_playerGo);
            Object.Destroy(_stageGo);
            Object.Destroy(_itemGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 접촉하면_자동으로_획득된다()
        {
            Assert.IsTrue(_item.IsCollected, "접촉했는데 획득되지 않았습니다.");
            Assert.AreEqual(1, _stage.AcquiredGoalItemCount);
            Assert.IsFalse(_itemGo.GetComponent<CircleCollider2D>().enabled, "획득 후 콜라이더가 꺼지지 않았습니다.");
            yield break;
        }

        [UnityTest]
        public IEnumerator 같은_아이템을_중복_획득하지_않는다()
        {
            _item.Collect();
            _item.Collect();

            Assert.AreEqual(1, _stage.AcquiredGoalItemCount, "같은 아이템이 중복 집계됐습니다.");
            yield break;
        }

        [UnityTest]
        public IEnumerator 요구_수량을_채우면_플레이어가_정지한다()
        {
            // SetUp에서 1개 획득됨 — 요구 수량 2를 채우기 위해 하나 더 배치한다.
            var second = new GameObject("GoalItem2");
            second.transform.position = new Vector3(5f, 0f, 0f);
            var collider = second.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;
            var secondItem = second.AddComponent<GoalItem>();
            yield return null;

            secondItem.Collect();

            Assert.IsTrue(_stage.IsStageCleared, "요구 수량을 채웠는데 클리어되지 않았습니다.");
            Assert.AreEqual(PlayerState.Disabled, _player.State, "클리어 후 플레이어가 정지하지 않았습니다.");

            Object.Destroy(second);
        }
    }
}
