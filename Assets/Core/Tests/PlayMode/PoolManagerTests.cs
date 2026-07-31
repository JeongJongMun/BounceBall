using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Core.Tests
{
    public class PoolManagerTests
    {
        private PoolManager _pool;
        private GameObject _prefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _pool = new GameObject("Pool").AddComponent<PoolManager>();
            _prefab = new GameObject("Prefab");
            _prefab.SetActive(false); // 씬 오브젝트를 프리팹 대용으로 사용
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_pool.gameObject);
            Object.Destroy(_prefab);
            yield return null;
        }

        [Test]
        public void Spawn하면_활성_인스턴스가_생성된다()
        {
            var instance = _pool.Spawn(_prefab, Vector3.one, Quaternion.identity);
            Assert.IsTrue(instance.activeSelf);
            Assert.AreEqual(Vector3.one, instance.transform.position);
        }

        [Test]
        public void Despawn_후_다시_Spawn하면_같은_인스턴스_재사용()
        {
            var first = _pool.Spawn(_prefab, Vector3.zero, Quaternion.identity);
            _pool.Despawn(first);
            Assert.IsFalse(first.activeSelf);
            var second = _pool.Spawn(_prefab, Vector3.zero, Quaternion.identity);
            Assert.AreSame(first, second);
        }

        [Test]
        public void 풀에_없는_오브젝트_Despawn해도_예외_없다()
        {
            var stray = new GameObject("Stray");
            Assert.DoesNotThrow(() => _pool.Despawn(stray));
        }
    }
}
