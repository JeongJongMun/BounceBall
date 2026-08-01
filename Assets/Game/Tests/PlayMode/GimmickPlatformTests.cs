using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    // 일회성 발판 (기믹 문서 §3.1) · 슈퍼 점프 발판 (§3.2)
    public class GimmickPlatformTests
    {
        private GameObject _platformGo;
        private GameObject _playerGo;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                Object.Destroy(systems);
                yield return null;
            }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_platformGo != null) Object.Destroy(_platformGo);
            if (_playerGo != null) Object.Destroy(_playerGo);
            yield return null;
        }

        private DisposablePlatform CreateDisposable(float delay, float respawn)
        {
            _platformGo = new GameObject("DisposablePlatform");
            _platformGo.AddComponent<SpriteRenderer>();
            _platformGo.AddComponent<BoxCollider2D>().size = Vector2.one;
            var platform = _platformGo.AddComponent<DisposablePlatform>();
            platform.SetData(delay, respawn);
            return platform;
        }

        // 플레이어를 발판 위에 떨어뜨려 실제 착지로 작동시킨다.
        private Player CreatePlayerAbove(Vector3 platformPosition)
        {
            _playerGo = new GameObject("Player");
            _playerGo.transform.position = platformPosition + new Vector3(0f, 1.2f, 0f);
            var body = _playerGo.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _playerGo.AddComponent<PlayerStats>();
            return _playerGo.AddComponent<Player>();
        }

        [UnityTest]
        public IEnumerator 상단에_착지하면_지연_후_사라진다()
        {
            var platform = CreateDisposable(delay: 0.1f, respawn: 0.3f);
            CreatePlayerAbove(_platformGo.transform.position);

            // 낙하 → 착지
            yield return new WaitForSeconds(0.5f);

            Assert.AreNotEqual(DisposablePlatform.PlatformState.Active, platform.State,
                "상단에 착지했는데 발판이 그대로 활성 상태입니다 (기믹 문서 §3.1).");
            Assert.IsFalse(_platformGo.GetComponent<BoxCollider2D>().enabled,
                "사라진 발판의 충돌 판정이 남아 있습니다.");
        }

        [UnityTest]
        public IEnumerator 재생성_시간이_지나면_돌아온다()
        {
            var platform = CreateDisposable(delay: 0f, respawn: 0.3f);
            platform.Trigger();

            yield return null;
            Assert.AreEqual(DisposablePlatform.PlatformState.RespawnWaiting, platform.State);

            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(DisposablePlatform.PlatformState.Active, platform.State,
                "재생성 시간이 지났는데 발판이 돌아오지 않았습니다 (기믹 문서 §3.1).");
            Assert.IsTrue(_platformGo.GetComponent<BoxCollider2D>().enabled);
        }

        [UnityTest]
        public IEnumerator 재생성_시간이_음수면_돌아오지_않는다()
        {
            var platform = CreateDisposable(delay: 0f, respawn: -1f);
            platform.Trigger();

            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(DisposablePlatform.PlatformState.Disabled, platform.State,
                "RespawnTime이 -1인데 발판이 재생성됐습니다 (기믹 문서 §3.1).");
            Assert.IsFalse(_platformGo.GetComponent<BoxCollider2D>().enabled);
        }

        [UnityTest]
        public IEnumerator 스테이지_재시작으로_초기_상태로_복구한다()
        {
            var platform = CreateDisposable(delay: 0f, respawn: -1f);
            platform.Trigger();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(DisposablePlatform.PlatformState.Disabled, platform.State);

            platform.ResetPlatform();

            Assert.AreEqual(DisposablePlatform.PlatformState.Active, platform.State);
            Assert.IsTrue(_platformGo.GetComponent<BoxCollider2D>().enabled);
        }

        [UnityTest]
        public IEnumerator 옆면에_닿는_것만으로는_작동하지_않는다()
        {
            var platform = CreateDisposable(delay: 0f, respawn: 3f);

            // 발판 옆에서 밀어붙인다 — 상단 접촉이 아니므로 작동하면 안 된다
            _playerGo = new GameObject("Player");
            _playerGo.transform.position = new Vector3(1.5f, 0f, 0f);
            var body = _playerGo.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 0f;
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _playerGo.AddComponent<PlayerStats>();
            _playerGo.AddComponent<Player>();
            body.linearVelocity = new Vector2(-3f, 0f);

            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(DisposablePlatform.PlatformState.Active, platform.State,
                "옆면 접촉으로 발판이 사라졌습니다 (기믹 문서 §3.1).");
        }

        [UnityTest]
        public IEnumerator 슈퍼_점프_발판은_일반_점프보다_높게_튕긴다()
        {
            _platformGo = new GameObject("SuperJumpPlatform");
            _platformGo.AddComponent<SpriteRenderer>();
            _platformGo.AddComponent<BoxCollider2D>().size = Vector2.one;
            var superJump = _platformGo.AddComponent<SuperJumpPlatform>();
            superJump.SetData(2f);

            var player = CreatePlayerAbove(_platformGo.transform.position);
            var bounce = _playerGo.AddComponent<PlayerBounce>();
            Assert.IsNotNull(bounce);

            float normalJump = player.Stats.NormalJumpForce;

            // 낙하 → 착지 → 자동 점프. 상승 최고 속도를 잡기 위해 착지 직후를 관찰한다.
            float peak = 0f;
            for (int i = 0; i < 120; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, player.Body.linearVelocity.y);
            }

            Assert.Greater(peak, normalJump * 1.5f,
                $"슈퍼 점프 배율이 적용되지 않았습니다 (일반 {normalJump}, 관측 최고 {peak}) (기믹 문서 §3.2).");
        }

        // 기믹 문서 §3.2: 성질과 무관하게 모든 플레이어에게 동일한 결과를 준다.
        // 성질별 점프력(감소/일반/증가)에 배율을 곱하면 얼음·젤리가 서로 다른 높이로 튄다.
        [UnityTest]
        public IEnumerator 슈퍼_점프_발판은_성질과_무관하게_같은_높이로_튕긴다(
            [Values(PlayerPropertyType.Default, PlayerPropertyType.Jelly, PlayerPropertyType.Ice)]
            PlayerPropertyType property)
        {
            _platformGo = new GameObject("SuperJumpPlatform");
            _platformGo.AddComponent<BoxCollider2D>().size = Vector2.one;
            var superJump = _platformGo.AddComponent<SuperJumpPlatform>();
            superJump.SetData(2f);

            var player = CreatePlayerAbove(_platformGo.transform.position);
            player.PropertyType = property;
            _playerGo.AddComponent<PlayerBounce>();

            float expected = player.Stats.NormalJumpForce * 2f;

            float peak = 0f;
            for (int i = 0; i < 120; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, player.Body.linearVelocity.y);
            }

            Assert.AreEqual(expected, peak, 0.01f,
                $"{property} 성질의 슈퍼 점프 높이가 기본 성질과 다릅니다 (기대 {expected}, 관측 {peak}) (기믹 문서 §3.2).");
        }
    }
}
