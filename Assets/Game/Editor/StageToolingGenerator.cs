using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.EditorTools
{
    // 스테이지 제작에 필요한 에셋(타일, 팔레트, 플레이스홀더 프리팹, 브러시)을 생성한다.
    public static class StageToolingGenerator
    {
        private const string TilesDir = "Assets/Game/Tiles";
        private const string BrushesDir = "Assets/Game/Tiles/Brushes";
        private const string PrefabsDir = "Assets/Game/Prefabs";
        private const string WhiteTexPath = "Assets/Game/Tiles/WhiteTile.png";
        private const string GroundTilePath = "Assets/Game/Tiles/GroundTile.asset";
        private const string PalettePath = "Assets/Game/Tiles/StagePalette.prefab";

        [MenuItem("Game/Setup Stage Tooling")]
        public static void GenerateAll()
        {
            EnsureFolder(TilesDir);
            EnsureFolder(BrushesDir);
            EnsureFolder(PrefabsDir);

            var tile = CreateGroundTile();
            CreatePalette(tile);

            var propertyItem = CreateItemPrefab("PropertyItem", new Color(0.75f, 0.35f, 0.95f), typeof(PropertyItem));
            var goalItem = CreateItemPrefab("GoalItem", new Color(1f, 0.85f, 0.2f), typeof(GoalItem));
            var checkpoint = CreateItemPrefab("Checkpoint", new Color(0.3f, 0.9f, 0.45f), typeof(Checkpoint));

            CreateBrush("PropertyItemBrush", propertyItem);
            CreateBrush("GoalItemBrush", goalItem);
            CreateBrush("CheckpointBrush", checkpoint);

            CreatePlayerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Game] 스테이지 툴링 에셋 생성 완료 (타일/팔레트/프리팹/브러시)");
        }

        private static Tile CreateGroundTile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(GroundTilePath);
            if (existing != null) return existing;

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(WhiteTexPath) == null)
            {
                var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                var pixels = new Color32[16 * 16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                tex.Apply();
                File.WriteAllBytes(WhiteTexPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(WhiteTexPath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(WhiteTexPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteTexPath);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = new Color(0.55f, 0.55f, 0.6f);
            AssetDatabase.CreateAsset(tile, GroundTilePath);
            return tile;
        }

        private static void CreatePalette(Tile tile)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath) != null) return;

            var root = new GameObject("StagePalette");
            root.AddComponent<Grid>();
            var layer = new GameObject("Layer1");
            layer.transform.SetParent(root.transform, false);
            var tilemap = layer.AddComponent<Tilemap>();
            layer.AddComponent<TilemapRenderer>();
            tilemap.SetTile(Vector3Int.zero, tile);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
            Object.DestroyImmediate(root);

            var palette = ScriptableObject.CreateInstance<GridPalette>();
            palette.name = "Palette Settings";
            palette.cellSizing = GridPalette.CellSizing.Automatic;
            AssetDatabase.AddObjectToAsset(palette, prefab);
        }

        private static GameObject CreateItemPrefab(string name, Color color, System.Type shellComponent)
        {
            var path = $"{PrefabsDir}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = color;
            sr.sortingOrder = 10;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;
            go.AddComponent(shellComponent);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void CreatePlayerPrefab()
        {
            const string resourcesDir = "Assets/Game/Resources";
            const string playerPath = "Assets/Game/Resources/Player.prefab";
            const string materialPath = "Assets/Game/Resources/PlayerPhysics.physicsMaterial2D";

            EnsureFolder(resourcesDir);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(playerPath) != null) return;

            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("PlayerPhysics") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var go = new GameObject("Player");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = new Color(0.4f, 0.85f, 0.35f); // 카멜레온 기본색
            sr.sortingOrder = 20;

            var body = go.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;
            collider.sharedMaterial = material;

            go.AddComponent<PlayerStats>();
            go.AddComponent<Player>();
            go.AddComponent<PlayerMovement>();
            go.AddComponent<PlayerBounce>();

            PrefabUtility.SaveAsPrefabAsset(go, playerPath);
            Object.DestroyImmediate(go);
        }

        private static void CreateBrush(string name, GameObject prefab)
        {
            var path = $"{BrushesDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrefabPaletteBrush>(path);
            if (existing != null) return;

            var brush = ScriptableObject.CreateInstance<PrefabPaletteBrush>();
            brush.Prefab = prefab;
            AssetDatabase.CreateAsset(brush, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
