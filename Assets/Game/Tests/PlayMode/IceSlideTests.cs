using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    // 얼음 미끄러짐 (기획 §6). 물리 스텝이 필요하므로 PlayMode에서 검증한다.
    public class IceSlideTests
    {
        private GameObject _playerGo;
        private Player _player;
        private PlayerIceSlide _slide;
        private PlayerMovement _movement;
        private Rigidbody2D _body;

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
            _body = _playerGo.AddComponent<Rigidbody2D>();
            _body.freezeRotation = true;
            _body.gravityScale = 0f; // 수평 거동만 검증한다
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _playerGo.AddComponent<PlayerStats>();
            _player = _playerGo.AddComponent<Player>();
            // PlayerMovement가 Awake에서 미끄러짐 컴포넌트를 잡으므로 먼저 붙인다.
            _slide = _playerGo.AddComponent<PlayerIceSlide>();
            _slide.ReadKeyboard = false;
            _movement = _playerGo.AddComponent<PlayerMovement>();
            _movement.ReadKeyboard = false;

            _player.PropertyType = PlayerPropertyType.Ice;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_playerGo);
            yield return null;
        }

        private IEnumerator Steps(int count)
        {
            for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator 입력이_없으면_바라보는_방향으로_최소_속도를_유지한다()
        {
            _player.FacingDirection = 1f;
            _body.linearVelocity = Vector2.zero;
            _slide.Enter();

            yield return Steps(3);

            Assert.Greater(_body.linearVelocity.x, 0.1f,
                "입력이 없는데도 정지했습니다 (기획 §6.2).");
        }

        [UnityTest]
        public IEnumerator 진입_시_기존_속도가_최소보다_빠르면_그대로_유지한다()
        {
            _player.FacingDirection = 1f;
            _body.linearVelocity = new Vector2(-10f, 0f); // 왼쪽으로 빠르게 이동 중
            _slide.Enter();

            Assert.Less(_body.linearVelocity.x, -5f,
                "기존 이동 방향과 속도가 최소 속도로 덮어써졌습니다 (기획 §6.2).");
            Assert.AreEqual(-1f, _player.FacingDirection,
                "기존 이동 방향이 바라보는 방향에 반영되지 않았습니다 (기획 §6.5).");
            yield break;
        }

        [UnityTest]
        public IEnumerator 입력을_유지하면_가속하고_최대_속도에서_멈춘다()
        {
            _slide.Enter();
            _slide.SetInput(1f);

            yield return Steps(10);
            float mid = _body.linearVelocity.x;

            yield return Steps(200);
            float top = _body.linearVelocity.x;

            Assert.Greater(mid, 0f);
            Assert.Greater(top, mid, "입력을 유지했는데 가속하지 않았습니다 (기획 §6.3).");
            Assert.LessOrEqual(top, 14.01f, "최대 미끄러짐 속도를 넘었습니다 (기획 §6.3).");
        }

        [UnityTest]
        public IEnumerator 반대_방향_입력은_즉시_방향을_바꾸지_않는다()
        {
            _slide.Enter();
            _slide.SetInput(1f);
            yield return Steps(200); // 오른쪽 최대 속도까지

            float before = _body.linearVelocity.x;
            Assert.Greater(before, 5f);

            _slide.SetInput(-1f);
            yield return new WaitForFixedUpdate();

            float after = _body.linearVelocity.x;
            Assert.Greater(after, 0f, "반대 입력 한 스텝 만에 방향이 뒤집혔습니다 (기획 §6.4).");
            Assert.Less(after, before, "반대 입력인데 감속하지 않았습니다 (기획 §6.4).");
            Assert.AreEqual(1f, _player.FacingDirection,
                "실제 이동 방향이 바뀌기 전에 바라보는 방향이 먼저 바뀌었습니다 (기획 §6.5).");
        }

        [UnityTest]
        public IEnumerator 반대_입력을_계속하면_결국_방향이_바뀐다()
        {
            _slide.Enter();
            _slide.SetInput(1f);
            yield return Steps(200);

            _slide.SetInput(-1f);
            yield return Steps(400);

            Assert.Less(_body.linearVelocity.x, 0f, "반대 입력을 유지했는데 방향이 바뀌지 않았습니다 (기획 §6.4).");
            Assert.AreEqual(-1f, _player.FacingDirection,
                "이동 방향이 바뀐 뒤에도 바라보는 방향이 갱신되지 않았습니다 (기획 §6.5).");
        }

        [UnityTest]
        public IEnumerator 성질이_바뀌면_미끄러짐이_해제된다()
        {
            _slide.Enter();
            Assert.IsTrue(_slide.IsSliding);

            _player.PropertyType = PlayerPropertyType.Default;
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(_slide.IsSliding, "성질이 바뀌었는데 미끄러짐이 유지됩니다 (기획 §6.7).");
        }

        [UnityTest]
        public IEnumerator 해제해도_수평_속도는_즉시_0이_되지_않는다()
        {
            _slide.Enter();
            _slide.SetInput(1f);
            yield return Steps(100);

            float before = _body.linearVelocity.x;
            _slide.Exit();

            Assert.IsFalse(_slide.IsSliding);
            Assert.AreEqual(before, _body.linearVelocity.x, 0.001f,
                "해제하면서 수평 속도를 0으로 만들었습니다 (기획 §6.7).");
            yield break;
        }

        [UnityTest]
        public IEnumerator 얼음_성질이_아니면_진입하지_않는다()
        {
            _player.PropertyType = PlayerPropertyType.Jelly;
            _slide.Enter();

            Assert.IsFalse(_slide.IsSliding, "얼음 성질이 아닌데 미끄러짐에 진입했습니다 (기획 §6.1).");
            yield break;
        }

        [UnityTest]
        public IEnumerator 미끄러짐_중에는_일반_이동이_속도를_덮어쓰지_않는다()
        {
            _slide.Enter();
            _slide.SetInput(1f);
            yield return Steps(200);

            float slideSpeed = _body.linearVelocity.x;

            // 일반 이동의 최대 속도(MoveSpeed 7)보다 빨라야 미끄러짐이 살아 있는 것이다
            Assert.Greater(slideSpeed, _player.Stats.MoveSpeed + 1f,
                "일반 이동이 미끄러짐 속도를 덮어썼습니다 (기획 §6.1).");
        }
    }
}
