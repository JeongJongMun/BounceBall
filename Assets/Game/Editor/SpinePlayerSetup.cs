using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    // Player.prefab에 스파인 뷰(Default/Jelly 스켈레톤 + PlayerSpineView)를 조립한다.
    // 재실행 가능 — 기존 뷰 자식을 지우고 다시 만든다.
    // CLI: Unity.exe -batchmode -executeMethod Game.EditorTools.SpinePlayerSetup.Apply
    public static class SpinePlayerSetup
    {
        private const string PlayerPrefabPath = "Assets/Game/Resources/Player.prefab";
        private const string DefaultDataPath = "Assets/Arts/Character/Default/Player_Kameleon_SkeletonData.asset";
        private const string JellyDataPath = "Assets/Arts/Character/Jelly/Player_Kameleon_Jelly_SkeletonData.asset";
        private const string IceDataPath = "Assets/Arts/Character/Ice/Kameleon_Ice_SkeletonData.asset";
        private const string DefaultTonguePath = "Assets/Arts/Character/Default/Kameleon_Tongue.png";
        private const string JellyTonguePath = "Assets/Arts/Character/Jelly/Kameleon_Jelly_Tongue.png";
        private const string IceTonguePath = "Assets/Arts/Character/Ice/Kameleon_Ice_Tongue.png";

        // 스켈레톤 원본 높이 ~1257px, SkeletonData 기본 스케일 0.01 → 약 12.6유닛.
        // 플레이어 콜라이더(반지름 0.35)에 맞춰 시각 높이 ~1유닛으로 축소한다.
        private const float ViewScale = 0.08f;

        // 적용 완료됨 — 얼음 스켈레톤 추가, 애니메이션 이름 변경, 스케일 기준 변경 시에만 재실행.
        public static void Apply()
        {
            var defaultData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(DefaultDataPath);
            var jellyData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(JellyDataPath);
            var iceData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(IceDataPath);
            if (defaultData == null || jellyData == null || iceData == null)
            {
                Debug.LogError("[Game] SkeletonData 에셋을 찾을 수 없습니다. Assets/Arts를 Reimport 했는지 확인하세요.");
                return;
            }

            var defaultTongue = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultTonguePath);
            var jellyTongue = AssetDatabase.LoadAssetAtPath<Sprite>(JellyTonguePath);
            var iceTongue = AssetDatabase.LoadAssetAtPath<Sprite>(IceTonguePath);
            if (defaultTongue == null || jellyTongue == null || iceTongue == null)
            {
                Debug.LogError("[Game] 혀 스프라이트를 찾을 수 없습니다. Assets/Arts를 Reimport 했는지 확인하세요.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                // 플레이스홀더 원 스프라이트 제거 (성질 색상은 스켈레톤 틴트로 대체)
                var sprite = root.GetComponent<SpriteRenderer>();
                if (sprite != null) Object.DestroyImmediate(sprite);

                var defaultSkeleton = RebuildView(root.transform, "DefaultView", defaultData);
                var jellySkeleton = RebuildView(root.transform, "JellyView", jellyData);
                var iceSkeleton = RebuildView(root.transform, "IceView", iceData);
                var tongue = RebuildTongue(root.transform);

                var view = root.GetComponent<PlayerSpineView>();
                if (view == null) view = root.AddComponent<PlayerSpineView>();

                var so = new SerializedObject(view);
                BindViewSet(so, "defaultView", defaultSkeleton, defaultTongue,
                    "Kameleon_Idle", "Kameleon_Jump", "Kameleon_Uwaa", crawl: "", slide: "", die: "Kameleon_Die");
                BindViewSet(so, "jellyView", jellySkeleton, jellyTongue,
                    "Kameleon_Jelly_Idle", "Kameleon_Jelly_Jump", "Kameleon_Jelly_Uwaa", crawl: "Kameleon_Jelly_Slime", slide: "", die: "Kameleon_Jelly_Die");
                // 얼음은 아직 사망 애니메이션이 없다 — PlayDeath()가 스케일 아웃으로 대체한다.
                BindViewSet(so, "iceView", iceSkeleton, iceTongue,
                    "Kameleon_Ice_Idle", "Kameleon_Jump", "Kameleon_Ice_Uwaa", crawl: "", slide: "Kameleon_Ice_Slide", die: "");
                so.FindProperty("tongue").objectReferenceValue = tongue;
                so.ApplyModifiedPropertiesWithoutUndo();

                jellySkeleton.gameObject.SetActive(false); // 기본 성질로 시작
                iceSkeleton.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Game] Player.prefab 스파인 뷰 적용 완료");
        }

        private static SkeletonAnimation RebuildView(Transform parent, string name, SkeletonDataAsset data)
        {
            var existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var skeleton = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
            skeleton.gameObject.name = name;
            skeleton.transform.SetParent(parent, false);
            skeleton.transform.localPosition = new Vector3(0f, -0.35f, 0f); // 발이 콜라이더 하단에 오도록
            skeleton.transform.localScale = Vector3.one * ViewScale;
            skeleton.Initialize(false);

            var meshRenderer = skeleton.GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.sortingOrder = 20; // 기존 스프라이트와 동일한 정렬 순서

            return skeleton;
        }

        // 아이템 방향으로 뻗는 혀. 세 성질이 렌더러 하나를 공유하고 스프라이트만 바꿔 낀다.
        private static SpriteRenderer RebuildTongue(Transform parent)
        {
            var existing = parent.Find("Tongue");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("Tongue");
            go.SetActive(true); // GameObject는 항상 켜둔다 — 껐다 켜는 건 SpriteRenderer.enabled로만
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 25; // 스켈레톤(20)보다 앞
            renderer.enabled = false;
            return renderer;
        }

        private static void BindViewSet(SerializedObject so, string field,
            SkeletonAnimation skeleton, Sprite tongueSprite, string idle, string jump, string eat, string crawl, string slide, string die)
        {
            so.FindProperty($"{field}.tongueSprite").objectReferenceValue = tongueSprite;
            so.FindProperty($"{field}.skeleton").objectReferenceValue = skeleton;
            so.FindProperty($"{field}.idle").stringValue = idle;
            so.FindProperty($"{field}.jump").stringValue = jump;
            so.FindProperty($"{field}.eat").stringValue = eat;
            so.FindProperty($"{field}.crawl").stringValue = crawl;
            so.FindProperty($"{field}.slide").stringValue = slide;
            so.FindProperty($"{field}.die").stringValue = die;
        }
    }
}
