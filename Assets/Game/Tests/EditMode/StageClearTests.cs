using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class StageClearTests
    {
        private GameObject _go;
        private StageController _stage;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Stage");
            _stage = _go.AddComponent<StageController>();
            _stage.SetGoalCounts(total: 3, required: 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void 획득할수록_수량이_증가한다()
        {
            Assert.AreEqual(0, _stage.AcquiredGoalItemCount);

            _stage.NotifyGoalCollected();

            Assert.AreEqual(1, _stage.AcquiredGoalItemCount);
            Assert.IsFalse(_stage.IsStageCleared, "요구 수량에 못 미쳤는데 클리어됐습니다.");
        }

        [Test]
        public void 요구_수량을_채우면_클리어된다()
        {
            _stage.NotifyGoalCollected();
            _stage.NotifyGoalCollected();

            Assert.IsTrue(_stage.IsStageCleared);
        }

        [Test]
        public void 클리어_이후_획득은_무시된다()
        {
            _stage.NotifyGoalCollected();
            _stage.NotifyGoalCollected();
            _stage.NotifyGoalCollected();

            Assert.AreEqual(2, _stage.AcquiredGoalItemCount, "클리어 후에도 수량이 증가했습니다.");
        }

        [Test]
        public void 진행_문자열은_획득_슬래시_요구_형식이다()
        {
            Assert.AreEqual("0 / 2", _stage.GoalProgressText);

            _stage.NotifyGoalCollected();

            Assert.AreEqual("1 / 2", _stage.GoalProgressText);
        }
    }
}
