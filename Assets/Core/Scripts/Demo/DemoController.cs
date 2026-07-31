using UnityEngine;

namespace Core.Demo
{
    public class DemoController : MonoBehaviour
    {
        [SerializeField] private AudioClip demoSfx;
        [SerializeField] private AudioClip demoBgm;
        [SerializeField] private GameObject demoPrefab;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 220, 400));
            GUILayout.Label($"State: {GameManager.Instance.State}  Score: {GameManager.Instance.Score}");
            if (GUILayout.Button("SFX 재생")) AudioManager.Instance.PlaySFX(demoSfx);
            if (GUILayout.Button("BGM 재생")) AudioManager.Instance.PlayBGM(demoBgm);
            if (GUILayout.Button("BGM 정지")) AudioManager.Instance.StopBGM();
            if (GUILayout.Button("풀 스폰") && demoPrefab != null)
            {
                var go = PoolManager.Instance.Spawn(demoPrefab,
                    Random.insideUnitCircle * 3f, Quaternion.identity);
                if (go.TryGetComponent<PooledObject>(out var pooled)) pooled.Despawn(1.5f);
            }
            if (GUILayout.Button("점수 +10")) GameManager.Instance.AddScore(10);
            if (GUILayout.Button("게임 오버")) GameManager.Instance.GameOver();
            if (GUILayout.Button("씬 리로드")) SceneLoader.Instance.Reload();
            GUILayout.EndArea();
        }
    }
}
