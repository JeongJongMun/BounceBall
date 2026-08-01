using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class StageRespawnPlayTests
    {
        private GameObject _playerGo;
        private GameObject _stageGo;
        private GameObject _startGo;
        private Player _player;
        private StageController _stage;

        private static readonly Vector3 StartPos = new(2f, 3f, 0f);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                Object.Destroy(systems);
                yield return null;
            }

            // StageController.Start()의 스폰을 건너뛰도록 플레이어를 먼저 만든다.
            _playerGo = new GameObject("Player");
            _playerGo.transform.position = StartPos;
            var body = _playerGo.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 0f; // 낙하는 테스트에서 직접 위치를 옮겨 재현한다
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

            yield return null; // Start() — 시작 지점이 기본 체크포인트로 등록된다
            yield return new WaitForFixedUpdate();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_playerGo);
            Object.Destroy(_stageGo);
            Object.Destroy(_startGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 낙사선_아래로_떨어지면_시작_지점으로_부활한다()
        {
            _playerGo.transform.position = new Vector3(0f, -20f, 0f);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(StartPos.x, _playerGo.transform.position.x, 0.01f);
            Assert.AreEqual(StartPos.y, _playerGo.transform.position.y, 0.01f);
            Assert.AreEqual(Vector2.zero, _player.Body.linearVelocity);
            Assert.AreNotEqual(PlayerState.Disabled, _player.State, "부활 후 조작이 재개되지 않았습니다.");
        }

        // 좌우는 사망이 아니다 — 투명 벽이 막을 뿐이다 (기획 §23.1: 사망은 낙사만)
        [UnityTest]
        public IEnumerator 좌우_경계를_넘어도_부활하지_않는다()
        {
            var left = new Vector3(-50f, 0f, 0f);
            _playerGo.transform.position = left;

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(left.x, _playerGo.transform.position.x, 0.01f, "좌측 이탈이 사망 처리됐습니다.");

            var right = new Vector3(50f, 0f, 0f);
            _playerGo.transform.position = right;

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(right.x, _playerGo.transform.position.x, 0.01f, "우측 이탈이 사망 처리됐습니다.");
        }

        // 스테이지 시작 시 경계 좌우에 투명 벽이 생성된다
        [UnityTest]
        public IEnumerator 경계에_투명_벽이_생성된다()
        {
            yield return null; // StageController.Start 대기

            var walls = new System.Collections.Generic.List<string>();
            foreach (var collider in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            {
                if (collider.gameObject.name.Contains("BoundaryWall")) walls.Add(collider.gameObject.name);
            }
            CollectionAssert.Contains(walls, "LeftBoundaryWall");
            CollectionAssert.Contains(walls, "RightBoundaryWall");
        }

        // 밟아 없앤 발판이 남아 있으면 부활 지점에서 진행이 막힐 수 있다
        [UnityTest]
        public IEnumerator 부활하면_일회성_발판이_되살아난다()
        {
            var platformGo = new GameObject("DisposablePlatform");
            platformGo.AddComponent<SpriteRenderer>();
            platformGo.AddComponent<BoxCollider2D>().size = Vector2.one;
            var platform = platformGo.AddComponent<DisposablePlatform>();
            platform.SetData(delay: 0f, respawn: -1f); // 스스로는 절대 돌아오지 않는 발판

            platform.Trigger();
            yield return null;
            Assert.AreEqual(DisposablePlatform.PlatformState.Disabled, platform.State);

            _playerGo.transform.position = new Vector3(0f, -20f, 0f);
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(DisposablePlatform.PlatformState.Active, platform.State,
                "부활했는데 일회성 발판이 사라진 채로 남아 있습니다.");
            Assert.IsTrue(platformGo.GetComponent<BoxCollider2D>().enabled);

            Object.Destroy(platformGo);
        }

        [UnityTest]
        public IEnumerator 위로_높이_올라가도_부활하지_않는다()
        {
            var high = new Vector3(0f, 100f, 0f);
            _playerGo.transform.position = high;

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(high.y, _playerGo.transform.position.y, 0.01f, "상단 이탈이 사망 처리됐습니다.");
        }
    }
}
