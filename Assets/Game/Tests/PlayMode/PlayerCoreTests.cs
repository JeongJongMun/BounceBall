using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class PlayerCoreTests
    {
        private GameObject _ground;
        private Player _player;
        private PlayerMovement _movement;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // 자동 부트스트랩된 Systems 제거 (Core 테스트와 동일 패턴)
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                Object.Destroy(systems);
                yield return null;
            }

            _ground = new GameObject("Ground");
            _ground.transform.position = new Vector3(0f, -2f, 0f);
            var groundCollider = _ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(50f, 1f);

            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, 1f, 0f);
            playerGo.AddComponent<Rigidbody2D>().freezeRotation = true;
            playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            playerGo.AddComponent<PlayerStats>();
            _player = playerGo.AddComponent<Player>();
            _movement = playerGo.AddComponent<PlayerMovement>();
            playerGo.AddComponent<PlayerBounce>();

            // 테스트에서는 키보드 입력을 읽지 않도록
            _movement.ReadKeyboard = false;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_player.gameObject);
            Object.Destroy(_ground);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 착지하면_자동으로_바운스한다()
        {
            // 낙하 → 착지 → 자동 점프로 상승 속도가 생길 때까지 대기
            float deadline = Time.time + 3f;
            bool bounced = false;
            while (Time.time < deadline)
            {
                if (_player.Body.linearVelocity.y > 1f) { bounced = true; break; }
                yield return new WaitForFixedUpdate();
            }
            Assert.IsTrue(bounced, "3초 안에 자동 바운스로 상승하지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 입력하면_수평으로_가속한다()
        {
            _movement.SetInput(1f);
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(_player.Body.linearVelocity.x, 1f);

            _movement.SetInput(0f);
            for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
            Assert.Less(Mathf.Abs(_player.Body.linearVelocity.x), 1f);
        }

        [UnityTest]
        public IEnumerator Disabled_상태에서는_바운스와_이동이_멈춘다()
        {
            _player.SetDisabled(true);
            _movement.SetInput(1f);

            // 바닥에 떨어질 시간을 충분히 준다
            for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();

            Assert.AreEqual(PlayerState.Disabled, _player.State);
            Assert.Less(Mathf.Abs(_player.Body.linearVelocity.x), 0.01f, "Disabled 상태에서 수평 이동이 발생했습니다.");
            Assert.LessOrEqual(_player.Body.linearVelocity.y, 0.01f, "Disabled 상태에서 바운스가 발생했습니다.");
        }
    }
}
