using Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    // 부활 암전 중 Esc로 일시정지를 열고 씬을 옮기면, 화면을 다시 밝힐 주체가 사라져
    // 검은 화면에 갇혔다. 그 입구를 막는 차단 플래그의 규칙을 고정한다.
    public class PauseBlockTests
    {
        private GameObject _go;
        private GameManager _game;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("GameManager");
            _game = _go.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Time.timeScale = 1f;
        }

        [Test]
        public void 차단_중에는_일시정지가_열리지_않는다()
        {
            _game.StartGame();
            _game.SetPauseBlocked(true);

            _game.Pause();

            Assert.AreEqual(GameState.Playing, _game.State);
            Assert.AreEqual(1f, Time.timeScale, "차단됐는데 시간이 멈추면 안 된다");
        }

        [Test]
        public void 차단_중에는_토글도_무시된다()
        {
            _game.StartGame();
            _game.SetPauseBlocked(true);

            _game.TogglePause();

            Assert.AreEqual(GameState.Playing, _game.State);
        }

        [Test]
        public void 차단이_풀리면_다시_일시정지된다()
        {
            _game.StartGame();
            _game.SetPauseBlocked(true);
            _game.Pause();

            _game.SetPauseBlocked(false);
            _game.Pause();

            Assert.AreEqual(GameState.Paused, _game.State);
        }

        // 해제까지 막으면 어떤 경로로든 멈춘 상태에서 빠져나오지 못한다.
        [Test]
        public void 차단_중에도_해제는_동작한다()
        {
            _game.StartGame();
            _game.Pause();
            Assert.AreEqual(GameState.Paused, _game.State, "사전 조건: 일시정지 상태");

            _game.SetPauseBlocked(true);
            _game.Resume();

            Assert.AreEqual(GameState.Playing, _game.State);
            Assert.AreEqual(1f, Time.timeScale);
        }

        // 연출 도중 스테이지가 사라지면 차단이 켜진 채 남는다 — 진입에서 항상 푼다.
        [Test]
        public void 스테이지_진입은_차단을_해제한다()
        {
            _game.SetPauseBlocked(true);

            _game.EnterStage();

            Assert.IsFalse(_game.IsPauseBlocked);
        }
    }
}
