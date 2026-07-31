using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Core
{
    public class PoolManager : Singleton<PoolManager>
    {
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _instanceToPool = new();

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = GetPool(prefab).Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;
            if (_instanceToPool.TryGetValue(instance, out var pool)) pool.Release(instance);
            else Destroy(instance);
        }

        private ObjectPool<GameObject> GetPool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var existing)) return existing;

            ObjectPool<GameObject> pool = null;
            pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var go = Instantiate(prefab, transform);
                    _instanceToPool[go] = pool;
                    return go;
                },
                actionOnGet: go => go.SetActive(true),
                actionOnRelease: go => go.SetActive(false),
                actionOnDestroy: go =>
                {
                    _instanceToPool.Remove(go);
                    Destroy(go);
                });
            _pools[prefab] = pool;
            return pool;
        }
    }
}
