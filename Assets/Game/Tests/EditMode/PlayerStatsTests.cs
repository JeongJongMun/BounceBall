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
        public void 공용_점프값은_감소_일반_증가_순으로_커진다()
        {
            // 기획 §4: 점프 감소 < 일반 점프 < 점프 증가
            Assert.Less(_stats.LowJumpForce, _stats.NormalJumpForce);
            Assert.Less(_stats.NormalJumpForce, _stats.HighJumpForce);
        }

        [Test]
        public void 상호작용_결과에_맞는_공용_점프값을_돌려준다()
        {
            Assert.AreEqual(_stats.NormalJumpForce, _stats.GetJumpForce(PropertyInteractionType.NormalJump));
            Assert.AreEqual(_stats.LowJumpForce, _stats.GetJumpForce(PropertyInteractionType.LowJump));
            Assert.AreEqual(_stats.HighJumpForce, _stats.GetJumpForce(PropertyInteractionType.HighJump));
        }

        [Test]
        public void 미끄러짐은_일반_점프값을_쓴다()
        {
            // 기획 §6.1: 얼음 타일 착지 시에는 공용 일반 점프를 적용한다
            Assert.AreEqual(_stats.NormalJumpForce, _stats.GetJumpForce(PropertyInteractionType.Slide));
        }
    }
}
