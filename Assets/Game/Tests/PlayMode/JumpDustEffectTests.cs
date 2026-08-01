using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    // 점프 먼지는 파티클이 아니라 발밑에서 한 번 재생되는 프레임 애니메이션이다.
    public class JumpDustEffectTests
    {
        private const float Duration = 0.3f;

        private GameObject _go;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            yield return null;
        }

        private static Sprite[] CreateFrames(int count)
        {
            var texture = new Texture2D(64, 16);
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = Sprite.Create(texture, new Rect(0, 0, 64, 16), Vector2.one * 0.5f, 100f);
                frames[i].name = "Effect_Jump " + i;
            }
            return frames;
        }

        private JumpDustEffect Create(Sprite[] frames)
        {
            _go = new GameObject("Player");
            _go.SetActive(false);
            _go.AddComponent<CircleCollider2D>().radius = 0.35f;
            var dust = _go.AddComponent<JumpDustEffect>();
            dust.SetData(frames, Duration);
            _go.SetActive(true);
            return dust;
        }

        private static SpriteRenderer FindDust()
        {
            var go = GameObject.Find("JumpDust");
            return go != null ? go.GetComponent<SpriteRenderer>() : null;
        }

        [UnityTest]
        public IEnumerator 점프하면_프레임을_순서대로_재생한다()
        {
            var frames = CreateFrames(3);
            var dust = Create(frames);
            yield return null;

            dust.Play();
            yield return null;

            var sr = FindDust();
            Assert.IsNotNull(sr, "점프 먼지가 만들어지지 않았습니다.");
            Assert.IsTrue(sr.gameObject.activeSelf, "먼지가 켜지지 않았습니다.");
            Assert.AreSame(frames[0], sr.sprite, "첫 프레임부터 시작해야 합니다.");

            // 중간 지점에서는 뒤쪽 프레임으로 넘어가 있어야 한다.
            yield return new WaitForSeconds(Duration * 0.6f);
            Assert.AreNotSame(frames[0], sr.sprite, "시간이 지났는데 첫 프레임에 머물러 있습니다.");
        }

        [UnityTest]
        public IEnumerator 재생이_끝나면_사라진다()
        {
            var dust = Create(CreateFrames(3));
            yield return null;

            dust.Play();
            yield return new WaitForSeconds(Duration + 0.2f);

            var sr = FindDust();
            Assert.IsFalse(sr != null && sr.gameObject.activeSelf, "재생이 끝났는데 먼지가 남아 있습니다.");
            Assert.IsFalse(dust.IsPlaying);
        }

        // 튄 자리에 남아야 한다 — 플레이어를 따라가면 안 된다.
        [UnityTest]
        public IEnumerator 먼지는_튄_자리에_남는다()
        {
            var dust = Create(CreateFrames(3));
            yield return null;

            dust.Play();
            yield return null;

            var sr = FindDust();
            float before = sr.transform.position.x;

            _go.transform.position += new Vector3(10f, 0f, 0f);
            yield return null;

            Assert.AreEqual(before, sr.transform.position.x, 0.01f,
                "플레이어가 이동하자 먼지가 함께 끌려갔습니다.");
        }

        // 발밑(접촉면)에서 나와야 한다.
        [UnityTest]
        public IEnumerator 먼지는_발밑에서_나온다()
        {
            var dust = Create(CreateFrames(3));
            _go.transform.position = new Vector3(0f, 4f, 0f);
            yield return null;

            dust.Play();
            yield return null;

            var sr = FindDust();
            float below = _go.transform.position.y - sr.transform.position.y;
            Assert.Greater(below, 0.15f, "먼지가 몸 중심 근처에서 나왔습니다 (아래로 " + below + ").");
        }

        // 원본이 6유닛대로 크다. 지정한 폭으로 줄여야 화면을 덮지 않는다.
        [UnityTest]
        public IEnumerator 지정한_폭으로_줄여서_그린다()
        {
            var dust = Create(CreateFrames(3));
            yield return null;

            dust.Play();
            yield return null;

            var sr = FindDust();
            Assert.Less(sr.bounds.size.x, 3f, "먼지가 너무 큽니다: " + sr.bounds.size.x);
            Assert.Greater(sr.bounds.size.x, 0.2f, "먼지가 너무 작습니다: " + sr.bounds.size.x);
        }

        // 슈퍼 점프 발판은 전용 먼지를 쓴다.
        [UnityTest]
        public IEnumerator 슈퍼_점프는_전용_먼지를_쓴다()
        {
            var normal = CreateFrames(3);
            var dust = Create(normal);
            var super = CreateFrames(4);
            for (int i = 0; i < super.Length; i++) super[i].name = "Effect_Superjump " + i;
            dust.SetSuperData(super, Duration);
            yield return null;

            dust.PlaySuper();
            yield return null;

            var sr = FindDust();
            Assert.IsNotNull(sr);
            CollectionAssert.Contains(super, sr.sprite, "슈퍼 점프에서 일반 먼지가 나왔습니다.");
            CollectionAssert.DoesNotContain(normal, sr.sprite);
        }

        // 슈퍼 점프 먼지는 일반보다 크다.
        [UnityTest]
        public IEnumerator 슈퍼_점프_먼지가_일반보다_크다()
        {
            var dust = Create(CreateFrames(3));
            dust.SetSuperData(CreateFrames(4), Duration);
            yield return null;

            dust.Play();
            yield return null;
            float normalWidth = FindDust().bounds.size.x;

            yield return new WaitForSeconds(Duration + 0.1f);

            dust.PlaySuper();
            yield return null;
            float superWidth = FindDust().bounds.size.x;

            Assert.Greater(superWidth, normalWidth,
                "슈퍼 점프 먼지가 더 커야 합니다 (일반 " + normalWidth + ", 슈퍼 " + superWidth + ").");
        }

        // 위치 보정은 인스펙터에서 조절한다 — 기본값은 발밑보다 조금 더 아래다.
        [UnityTest]
        public IEnumerator 먼지_위치가_발밑보다_아래로_보정된다()
        {
            var dust = Create(CreateFrames(3));
            _go.transform.position = new Vector3(0f, 4f, 0f);
            yield return null;

            dust.Play();
            yield return null;

            float radius = _go.GetComponent<CircleCollider2D>().radius;
            float dustY = FindDust().transform.position.y;
            Assert.Less(dustY, _go.transform.position.y - radius,
                "먼지가 발밑선보다 아래에 있어야 합니다 (발밑 "
                + (_go.transform.position.y - radius) + ", 먼지 " + dustY + ").");
        }

        [UnityTest]
        public IEnumerator 프레임이_없으면_아무것도_하지_않는다()
        {
            var dust = Create(new Sprite[0]);
            yield return null;

            dust.Play();
            yield return null;

            Assert.IsFalse(dust.IsPlaying);
        }
    }
}
