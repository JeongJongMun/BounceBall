using UnityEngine;

namespace Core
{
    public static class Systems
    {
        public const string PrefabPath = "Systems";
        private static GameObject _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[Core] Resources/Systems.prefab 이 없습니다. Core > Generate Template Assets 메뉴를 실행하세요.");
                return;
            }
            _instance = Object.Instantiate(prefab);
            _instance.name = "Systems";
            Object.DontDestroyOnLoad(_instance);
        }
    }
}
