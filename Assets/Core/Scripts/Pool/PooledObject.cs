using UnityEngine;

namespace Core
{
    public class PooledObject : MonoBehaviour
    {
        public void Despawn(float delay = 0f)
        {
            if (delay <= 0f) DespawnNow();
            else Invoke(nameof(DespawnNow), delay);
        }

        private void DespawnNow() => PoolManager.Instance.Despawn(gameObject);

        private void OnDisable() => CancelInvoke();
    }
}
