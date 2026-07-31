using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class CheckpointPlayTests
    {
        private GameObject _playerGo;
        private GameObject _stageGo;
        private GameObject _startGo;
        private GameObject _checkpointGo;
        private Player _player;
        private StageController _stage;
        private Checkpoint _checkpoint;

        private static readonly Vector3 StartPos = new(0f, 0f, 0f);
        private static readonly Vector3 CheckpointPos = new(4f, 1f, 0f);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                Object.Destroy(systems);
                yield return null;
            }

            _playerGo = new GameObject("Player");
            _playerGo.transform.position = StartPos;
            var body = _playerGo.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 0f;
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _playerGo.AddComponent<PlayerStats>();
            _player = _playerGo.AddComponent<Player>();

            _startGo = new GameObject("StartPosition");
            _startGo.transform.position = StartPos;

            _stageGo = new GameObject("Stage");
            _stage = _stageGo.AddComponent<StageController>();
            _stage.SetBounds(minX: -10f, maxX: 10f, minY: -6f, maxY: 6f, fallLimitY: -8f);
            _stage.SetGoalCounts(total: 1, required: 1);
            _stage.SetStartPosition(_startGo.transform);

            _checkpointGo = new GameObject("Checkpoint");
            _checkpointGo.transform.position = CheckpointPos;
            var collider = _checkpointGo.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;
            _checkpointGo.AddComponent<SpriteRenderer>();
            _checkpoint = _checkpointGo.AddComponent<Checkpoint>();

            yield return null; // Start() — 시작 지점이 기본 체크포인트로 등록된다
            yield return new WaitForFixedUpdate();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_playerGo);
            Object.Destroy(_stageGo);
            Object.Destroy(_startGo);
            Object.Destroy(_checkpointGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 접촉하면_체크포인트가_활성화된다()
        {
            _playerGo.transform.position = CheckpointPos;

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(_checkpoint.IsActivated, "접촉했는데 체크포인트가 활성화되지 않았습니다.");
            Assert.AreSame(_checkpoint, _stage.ActiveCheckpoint);
        }

        [UnityTest]
        public IEnumerator 체크포인트_활성화_후_낙사하면_시작_지점이_아니라_체크포인트로_부활한다()
        {
            _playerGo.transform.position = CheckpointPos;
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(_checkpoint.IsActivated, "사전 조건: 체크포인트가 활성화돼야 합니다.");

            // 낙사선 아래로 떨어뜨린다
            _playerGo.transform.position = new Vector3(0f, -20f, 0f);
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(CheckpointPos.x, _playerGo.transform.position.x, 0.01f);
            Assert.AreEqual(CheckpointPos.y, _playerGo.transform.position.y, 0.01f);
            Assert.AreNotEqual(PlayerState.Disabled, _player.State);
        }

        [UnityTest]
        public IEnumerator 체크포인트를_밟지_않으면_시작_지점으로_부활한다()
        {
            _playerGo.transform.position = new Vector3(0f, -20f, 0f);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsNull(_stage.ActiveCheckpoint);
            Assert.AreEqual(StartPos.x, _playerGo.transform.position.x, 0.01f);
            Assert.AreEqual(StartPos.y, _playerGo.transform.position.y, 0.01f);
        }
    }
}
