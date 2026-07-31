using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Core.Tests
{
    public class GameManagerTests
    {
        private GameManager _gm;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveData.ResetAll();
            _gm = new GameObject("GM").AddComponent<GameManager>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(_gm.gameObject);
            SaveData.ResetAll();
            yield return null;
        }

        [Test]
        public void 초기_상태는_Ready()
        {
            Assert.AreEqual(GameState.Ready, _gm.State);
        }

        [Test]
        public void StartGame_하면_Playing이고_점수_0()
        {
            _gm.AddScore(10);
            _gm.StartGame();
            Assert.AreEqual(GameState.Playing, _gm.State);
            Assert.AreEqual(0, _gm.Score);
        }

        [Test]
        public void AddScore는_Playing에서만_동작()
        {
            _gm.StartGame();
            _gm.AddScore(10);
            _gm.Pause();
            _gm.AddScore(5);
            Assert.AreEqual(10, _gm.Score);
        }

        [Test]
        public void Pause_Resume이_timeScale을_토글()
        {
            _gm.StartGame();
            _gm.Pause();
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(GameState.Paused, _gm.State);
            _gm.Resume();
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(GameState.Playing, _gm.State);
        }

        [Test]
        public void GameOver_시_하이스코어_갱신()
        {
            _gm.StartGame();
            _gm.AddScore(100);
            _gm.GameOver();
            Assert.AreEqual(GameState.GameOver, _gm.State);
            Assert.AreEqual(100, SaveData.HighScore);
        }

        [Test]
        public void 낮은_점수는_하이스코어를_덮지_않음()
        {
            SaveData.HighScore = 500;
            _gm.StartGame();
            _gm.AddScore(100);
            _gm.GameOver();
            Assert.AreEqual(500, SaveData.HighScore);
        }
    }
}
