using NUnit.Framework;

namespace Game.Tests
{
    // 장치 유무가 아니라 "마지막으로 실제로 쓴 입력"으로 갈린다는 규칙을 고정한다.
    // 데스크톱 브라우저에서도 Touchscreen 장치가 잡히기 때문에 장치로 판별하면 오판한다.
    public class InputModeTests
    {
        [SetUp]
        public void SetUp() => InputMode.ResetForTest();

        [TearDown]
        public void TearDown() => InputMode.ResetForTest();

        [Test]
        public void 키보드를_쓰면_터치_모드가_해제된다()
        {
            InputMode.ReportTouch();
            Assert.IsTrue(InputMode.IsTouch);

            InputMode.ReportKeyboard();
            Assert.IsFalse(InputMode.IsTouch);
        }

        [Test]
        public void 터치를_쓰면_다시_터치_모드가_된다()
        {
            InputMode.ReportKeyboard();
            InputMode.ReportTouch();

            Assert.IsTrue(InputMode.IsTouch);
        }

        [Test]
        public void 모드가_바뀔_때만_변경_알림이_온다()
        {
            InputMode.ReportKeyboard(); // 기본값(터치)에서 키보드로 — 여기서 이미 한 번 바뀐다

            int calls = 0;
            System.Action handler = () => calls++;
            InputMode.Changed += handler;

            InputMode.ReportKeyboard(); // 같은 모드 반복 — 알림 없음
            Assert.AreEqual(0, calls);

            InputMode.ReportTouch();    // 실제 전환 — 알림 1회
            Assert.AreEqual(1, calls);

            InputMode.ReportTouch();    // 다시 같은 모드 — 알림 없음
            Assert.AreEqual(1, calls);

            InputMode.Changed -= handler;
        }

        // 첫 조작 전 기본값 규칙. 실제 장치 유무는 실행 환경마다 달라(에디터에는 키보드가 있다)
        // 규칙 자체를 검증한다. 어긋나더라도 첫 조작에서 교정되는 구조다.
        [Test]
        public void 키보드가_없으면_터치로_시작한다()
        {
            Assert.IsTrue(InputMode.ComputeDefaultIsTouch(hasKeyboard: false));
            Assert.IsFalse(InputMode.ComputeDefaultIsTouch(hasKeyboard: true));
        }
    }
}
