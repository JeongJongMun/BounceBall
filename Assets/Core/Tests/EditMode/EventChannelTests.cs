using Core.Events;
using NUnit.Framework;
using UnityEngine;

namespace Core.Tests
{
    public class EventChannelTests
    {
        [Test]
        public void VoidChannel_Raise하면_구독자가_호출된다()
        {
            var channel = ScriptableObject.CreateInstance<VoidEventChannel>();
            int callCount = 0;
            channel.OnRaised += () => callCount++;
            channel.Raise();
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void IntChannel_값이_전달된다()
        {
            var channel = ScriptableObject.CreateInstance<IntEventChannel>();
            int received = -1;
            channel.OnRaised += v => received = v;
            channel.Raise(42);
            Assert.AreEqual(42, received);
        }

        [Test]
        public void FloatChannel_값이_전달된다()
        {
            var channel = ScriptableObject.CreateInstance<FloatEventChannel>();
            float received = -1f;
            channel.OnRaised += v => received = v;
            channel.Raise(0.5f);
            Assert.AreEqual(0.5f, received);
        }

        [Test]
        public void 구독자_없이_Raise해도_예외_없다()
        {
            var channel = ScriptableObject.CreateInstance<VoidEventChannel>();
            Assert.DoesNotThrow(() => channel.Raise());
        }
    }
}
