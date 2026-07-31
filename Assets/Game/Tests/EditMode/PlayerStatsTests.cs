using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class PlayerStatsTests
    {
        private PlayerStats _stats;

        [SetUp]
        public void SetUp()
        {
            _stats = new GameObject("Stats").AddComponent<PlayerStats>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stats.gameObject);
        }

        [Test]
        public void 배율_기본값은_1이라_기본값이_그대로_나온다()
        {
            Assert.AreEqual(12f, _stats.JumpForce);
            Assert.AreEqual(7f, _stats.MoveSpeed);
            Assert.AreEqual(3f, _stats.GravityScale);
        }

        [Test]
        public void 배율_적용_시_곱셈_결과가_나온다()
        {
            _stats.SetMultipliers(jump: 1.5f, move: 0.5f, gravity: 2f, airControlMult: 0.25f);
            Assert.AreEqual(12f * 1.5f, _stats.JumpForce);
            Assert.AreEqual(7f * 0.5f, _stats.MoveSpeed);
            Assert.AreEqual(3f * 2f, _stats.GravityScale);
            Assert.AreEqual(0.8f * 0.25f, _stats.AirControl, 0.0001f);
        }

        [Test]
        public void ResetMultipliers_후_기본값으로_복귀한다()
        {
            _stats.SetMultipliers(2f, 2f, 2f, 2f);
            _stats.ResetMultipliers();
            Assert.AreEqual(12f, _stats.JumpForce);
            Assert.AreEqual(7f, _stats.MoveSpeed);
        }
    }
}
