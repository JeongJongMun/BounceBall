using NUnit.Framework;

namespace Game.Tests
{
    public class KoreanParticleTests
    {
        [Test]
        public void 받침이_있으면_을을_쓴다()
        {
            Assert.AreEqual("을", KoreanParticle.Object("젤리 성질 아이템"));
            Assert.AreEqual("을", KoreanParticle.Object("얼음"));
        }

        [Test]
        public void 받침이_없으면_를을_쓴다()
        {
            Assert.AreEqual("를", KoreanParticle.Object("젤리"));
            Assert.AreEqual("를", KoreanParticle.Object("사과"));
        }

        [Test]
        public void 주격_조사도_받침에_따라_고른다()
        {
            Assert.AreEqual("이", KoreanParticle.Subject("얼음"));
            Assert.AreEqual("가", KoreanParticle.Subject("젤리"));
        }

        [Test]
        public void 조사를_붙인_문자열을_만든다()
        {
            Assert.AreEqual("얼음 성질 아이템을", KoreanParticle.WithObject("얼음 성질 아이템"));
            Assert.AreEqual("젤리를", KoreanParticle.WithObject("젤리"));
        }

        [Test]
        public void 한글이_아니거나_비어_있으면_기본값을_쓴다()
        {
            Assert.AreEqual("를", KoreanParticle.Object("Potion"));
            Assert.AreEqual("를", KoreanParticle.Object(""));
            Assert.AreEqual("를", KoreanParticle.Object(null));
        }
    }
}
