using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    // 터치 축의 누적 규칙을 고정한다. 키보드 쪽은 실제 장치가 필요해 EditMode에서 검증할 수 없으므로,
    // 키가 눌리지 않은 상태(= 키보드 축 0)에서 터치가 그대로 나오는 것까지만 확인한다.
    public class GameInputTests
    {
        [SetUp]
        public void SetUp() => GameInput.ClearTouch();

        [TearDown]
        public void TearDown() => GameInput.ClearTouch();

        [Test]
        public void 터치가_없으면_축은_0이다()
        {
            Assert.AreEqual(0f, GameInput.TouchHorizontal);
        }

        [Test]
        public void 한쪽을_누르면_그_방향이_나온다()
        {
            GameInput.AddTouchDirection(-1f);
            Assert.AreEqual(-1f, GameInput.TouchHorizontal);

            GameInput.ClearTouch();
            GameInput.AddTouchDirection(1f);
            Assert.AreEqual(1f, GameInput.TouchHorizontal);
        }

        // 키보드의 "동시 입력 = 0" 규칙(기획 §8.2)과 같게 동작해야 한다
        [Test]
        public void 좌우를_동시에_누르면_0이다()
        {
            GameInput.AddTouchDirection(-1f);
            GameInput.AddTouchDirection(1f);

            Assert.AreEqual(0f, GameInput.TouchHorizontal);
        }

        [Test]
        public void 한쪽을_떼면_남은_방향이_살아난다()
        {
            GameInput.AddTouchDirection(-1f);
            GameInput.AddTouchDirection(1f);

            GameInput.AddTouchDirection(-1f); // 오른쪽에서 손을 뗀다

            Assert.AreEqual(-1f, GameInput.TouchHorizontal);
        }

        // 신호를 놓쳐 누적이 어긋나도 이동 속도가 폭주하지 않아야 한다
        [Test]
        public void 축은_항상_마이너스1과_1_사이다()
        {
            GameInput.AddTouchDirection(1f);
            GameInput.AddTouchDirection(1f);
            GameInput.AddTouchDirection(1f);

            Assert.AreEqual(1f, GameInput.TouchHorizontal);
        }

        [Test]
        public void 키가_눌리지_않았으면_터치가_그대로_축이_된다()
        {
            GameInput.AddTouchDirection(1f);
            Assert.AreEqual(1f, GameInput.Horizontal());
        }

        [Test]
        public void 초기화하면_남은_입력이_끊긴다()
        {
            GameInput.AddTouchDirection(-1f);
            GameInput.ClearTouch();

            Assert.AreEqual(0f, GameInput.TouchHorizontal);
            Assert.AreEqual(0f, GameInput.Horizontal());
        }
    }
}
