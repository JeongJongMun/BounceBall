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
        private const string PropertiesDir = "Assets/Game/Properties";
        private const string WhiteTexPath = "Assets/Game/Tiles/WhiteTile.png";
        private const string GroundTilePath = "Assets/Game/Tiles/GroundTile.asset";
        private const string PalettePath = "Assets/Game/Tiles/StagePalette.prefab";

        // 에셋은 레포에 커밋되어 있어 평소 실행할 일이 없다. 복구는 git, 재생성은 CLI:
        // Unity.exe -batchmode -executeMethod Game.EditorTools.StageToolingGenerator.GenerateAll
        public static void GenerateAll()
        {
            EnsureFolder(TilesDir);
            EnsureFolder(BrushesDir);
            EnsureFolder(PrefabsDir);
            EnsureFolder(PropertiesDir);

            var tile = CreateGroundTile();

            var defaultProperty = CreateProperty("DefaultProperty", PlayerPropertyType.Default, "기본", DefaultColor);
            var jellyProperty = CreateProperty("JellyProperty", PlayerPropertyType.Jelly, "젤리", JellyColor);
            var iceProperty = CreateProperty("IceProperty", PlayerPropertyType.Ice, "얼음", IceColor);

            var jellyTile = CreatePropertyTile("JellyTile", TilePropertyType.Jelly, JellyColor);
            var iceTile = CreatePropertyTile("IceTile", TilePropertyType.Ice, IceColor);

            // 사망 발판 3종 (기믹 문서 §4). 성질별로 면제 대상이 다르다.
            var deathTile = CreateDeathTile("DeathTile_Default", TilePropertyType.Default, DeathDefaultColor);
            var jellyDeathTile = CreateDeathTile("DeathTile_Jelly", TilePropertyType.Jelly, DeathTint(JellyColor));
            var iceDeathTile = CreateDeathTile("DeathTile_Ice", TilePropertyType.Ice, DeathTint(IceColor));

            // 기존 PropertyItem.prefab을 젤리용으로 개명한다. GUID가 보존되어 이미 배치된 인스턴스가 살아남는다.
            RenameLegacyPropertyItem();

            var defaultItem = CreatePropertyItemPrefab("PropertyItem_Default", defaultProperty, DefaultColor);
            var jellyItem = CreatePropertyItemPrefab("PropertyItem_Jelly", jellyProperty, JellyColor);
            var iceItem = CreatePropertyItemPrefab("PropertyItem_Ice", iceProperty, IceColor);

            var goalItem = CreateItemPrefab("GoalItem", GoalColor, typeof(GoalItem));
            var checkpoint = CreateItemPrefab("Checkpoint", CheckpointColor, typeof(Checkpoint));
            RecolorItemPrefab(checkpoint, CheckpointColor);

            // 기믹 발판 2종 (기믹 문서 §3.1, §3.2)
            var disposable = CreatePlatformPrefab("DisposablePlatform", DisposableColor, typeof(DisposablePlatform));
            var superJump = CreatePlatformPrefab("SuperJumpPlatform", SuperJumpColor, typeof(SuperJumpPlatform));

            var defaultItemMarker = CreateMarkerTile("PropertyItemDefaultMarker", defaultItem, DefaultColor);
            var jellyItemMarker = CreateMarkerTile("PropertyItemJellyMarker", jellyItem, JellyColor);
            var iceItemMarker = CreateMarkerTile("PropertyItemIceMarker", iceItem, IceColor);
            var goalMarker = CreateMarkerTile("GoalItemMarker", goalItem, GoalColor);
            var checkpointMarker = CreateMarkerTile("CheckpointMarker", checkpoint, CheckpointColor);
            var disposableMarker = CreateMarkerTile("DisposablePlatformMarker", disposable, DisposableColor);
            var superJumpMarker = CreateMarkerTile("SuperJumpPlatformMarker", superJump, SuperJumpColor);

            CreateStageBrushAsset();
            UpdatePalette(tile, jellyTile, iceTile,
                deathTile, jellyDeathTile, iceDeathTile,
                defaultItemMarker, jellyItemMarker, iceItemMarker, goalMarker, checkpointMarker,
                disposableMarker, superJumpMarker);

            CreatePlayerPrefab(defaultProperty);

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

        private static readonly Color DefaultColor = new(0.4f, 0.85f, 0.35f);
        private static readonly Color JellyColor = new(0.55f, 0.3f, 0.95f);
        private static readonly Color IceColor = new(0.45f, 0.85f, 1f);
        private static readonly Color GoalColor = new(1f, 0.85f, 0.2f);
        // 기본 성질 아이템이 초록이라 체크포인트는 분홍으로 구분한다 (팔레트에서 헷갈리지 않도록).
        private static readonly Color CheckpointColor = new(1f, 0.4f, 0.75f);

        // 사망 발판은 성질 색을 붉게 눌러 "같은 성질이지만 위험"이라는 걸 팔레트에서 바로 읽히게 한다.
        private static readonly Color DeathDefaultColor = new(0.8f, 0.15f, 0.15f);
        private static Color DeathTint(Color property) => Color.Lerp(property, DeathDefaultColor, 0.55f);

        // 기믹 발판은 성질 색과 겹치지 않는 주황/청록으로 잡는다.
        private static readonly Color DisposableColor = new(0.95f, 0.6f, 0.2f);
        private static readonly Color SuperJumpColor = new(0.2f, 0.9f, 0.7f);

        private static PropertyData CreateProperty(string name, PlayerPropertyType type, string displayName, Color color)
        {
            var path = $"{PropertiesDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PropertyData>(path);
            if (existing != null)
            {
                // 구 포맷(문자열 id + 배율)으로 만들어진 에셋은 propertyType이 직렬화돼 있지 않아
                // 전부 Default로 역직렬화된다. 기대값과 다르면 마이그레이션으로 간주하고 다시 채운다.
                if (existing.PropertyType != type)
                {
                    existing.SetData(type, displayName, color);
                    EditorUtility.SetDirty(existing);
                    Debug.Log($"[Game] {name}을(를) 새 성질 포맷으로 마이그레이션했습니다 → {type}");
                }
                return existing;
            }

            var data = ScriptableObject.CreateInstance<PropertyData>();
            data.SetData(type, displayName, color);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        // 성질을 가진 타일 (기획 §1.3). 플레이어 성질과의 조합으로 반응이 결정된다.
        private static SpecialTile CreatePropertyTile(string name, TilePropertyType tileProperty, Color color)
        {
            var path = $"{TilesDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SpecialTile>(path);
            if (existing != null) return existing;

            var tile = ScriptableObject.CreateInstance<SpecialTile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteTexPath);
            tile.color = color;
            tile.colliderType = Tile.ColliderType.Grid;
            tile.SetTileProperty(tileProperty);
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        // 접촉 시 사망하는 발판 (기믹 문서 §4). 면제 성질은 타일 자신의 성질에서 나온다.
        private static SpecialTile CreateDeathTile(string name, TilePropertyType tileProperty, Color color)
        {
            var path = $"{TilesDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SpecialTile>(path);
            if (existing != null)
            {
                // 사망 플래그가 빠진 채 만들어진 에셋 보정
                if (!existing.IsDeadly)
                {
                    existing.SetDeadly(true);
                    EditorUtility.SetDirty(existing);
                }
                return existing;
            }

            var tile = ScriptableObject.CreateInstance<SpecialTile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteTexPath);
            tile.color = color;
            tile.colliderType = Tile.ColliderType.Grid;
            tile.SetTileProperty(tileProperty);
            tile.SetDeadly(true);
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        // 성질이 3종이 되면서 아이템도 3종으로 갈라졌다. 기존 단일 프리팹을 젤리용으로 개명해
        // 이미 스테이지에 배치된 인스턴스를 살린다 (RenameAsset은 GUID를 보존한다).
        private static void RenameLegacyPropertyItem()
        {
            const string legacyPath = PrefabsDir + "/PropertyItem.prefab";
            const string newPath = PrefabsDir + "/PropertyItem_Jelly.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(legacyPath) == null) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(newPath) != null) return;

            var error = AssetDatabase.RenameAsset(legacyPath, "PropertyItem_Jelly");
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[Game] PropertyItem 프리팹 개명 실패: {error}");
                return;
            }
            Debug.Log("[Game] PropertyItem.prefab → PropertyItem_Jelly.prefab 개명 (배치된 인스턴스 유지)");
        }

        // 접촉 즉시 획득이므로 아이템 시각 크기와 같은 충돌 범위를 쓴다 (기획 §11.1).
        private const float PropertyItemContactRadius = 0.35f;

        private static GameObject CreatePropertyItemPrefab(string name, PropertyData property, Color color)
        {
            var prefab = CreateItemPrefab(name, color, typeof(PropertyItem));
            ConfigurePropertyItem(prefab, property, color);
            return prefab;
        }

        // PropertyItem 프리팹에 성질 데이터를 연결하고, 접촉 콜라이더와 색을 맞춘다.
        // E 프롬프트를 쓰던 시절의 자식과 넓은 감지 반경이 남아 있으면 함께 걷어낸다.
        private static void ConfigurePropertyItem(GameObject propertyItemPrefabAsset, PropertyData itemProperty, Color color)
        {
            var path = AssetDatabase.GetAssetPath(propertyItemPrefabAsset);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;

                var collider = contents.GetComponent<CircleCollider2D>();
                if (!Mathf.Approximately(collider.radius, PropertyItemContactRadius))
                {
                    collider.radius = PropertyItemContactRadius;
                    changed = true;
                }

                var renderer = contents.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.color != color)
                {
                    renderer.color = color;
                    changed = true;
                }

                var legacyPrompt = contents.transform.Find("Prompt");
                if (legacyPrompt != null)
                {
                    Object.DestroyImmediate(legacyPrompt.gameObject);
                    changed = true;
                }

                // 성질 데이터가 비어 있으면(개명된 레거시 프리팹 등) 채워준다.
                var item = contents.GetComponent<PropertyItem>();
                if (item.PropertyData == null)
                {
                    item.SetData(itemProperty);
                    changed = true;
                }

                if (changed) PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // 팔레트를 만들고(없으면), 배치 가능한 전 항목을 항상 등록한다.
        // 1행: 타일(기본/젤리/얼음) / 2행: 프리팹 마커(성질 아이템 3종/목표/체크포인트)
        // 3행: 사망 발판(기본/젤리/얼음) + 기믹 발판(일회성/슈퍼점프)
        private static void UpdatePalette(
            Tile ground, SpecialTile jellyTile, SpecialTile iceTile,
            SpecialTile deathTile, SpecialTile jellyDeathTile, SpecialTile iceDeathTile,
            PrefabMarkerTile defaultItemMarker, PrefabMarkerTile jellyItemMarker, PrefabMarkerTile iceItemMarker,
            PrefabMarkerTile goalMarker, PrefabMarkerTile checkpointMarker,
            PrefabMarkerTile disposableMarker, PrefabMarkerTile superJumpMarker)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath) == null)
            {
                var root = new GameObject("StagePalette");
                root.AddComponent<Grid>();
                var layer = new GameObject("Layer1");
                layer.transform.SetParent(root.transform, false);
                layer.AddComponent<Tilemap>();
                layer.AddComponent<TilemapRenderer>();

                var prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
                Object.DestroyImmediate(root);

                var palette = ScriptableObject.CreateInstance<GridPalette>();
                palette.name = "Palette Settings";
                palette.cellSizing = GridPalette.CellSizing.Automatic;
                AssetDatabase.AddObjectToAsset(palette, prefabAsset);
            }

            var contents = PrefabUtility.LoadPrefabContents(PalettePath);
            try
            {
                var tilemap = contents.GetComponentInChildren<Tilemap>();

                // 그냥 덮어쓰면 타일맵이 셀의 기존 색/스프라이트 캐시 슬롯을 재사용해
                // 팔레트에 옛 색이 그대로 남는다. 비우고 다시 깔아야 GetTileData를 다시 묻는다.
                tilemap.ClearAllTiles();

                tilemap.SetTile(new Vector3Int(0, 0, 0), ground);
                tilemap.SetTile(new Vector3Int(1, 0, 0), jellyTile);
                tilemap.SetTile(new Vector3Int(2, 0, 0), iceTile);
                tilemap.SetTile(new Vector3Int(0, 1, 0), defaultItemMarker);
                tilemap.SetTile(new Vector3Int(1, 1, 0), jellyItemMarker);
                tilemap.SetTile(new Vector3Int(2, 1, 0), iceItemMarker);
                tilemap.SetTile(new Vector3Int(3, 1, 0), goalMarker);
                tilemap.SetTile(new Vector3Int(4, 1, 0), checkpointMarker);
                tilemap.SetTile(new Vector3Int(0, -1, 0), deathTile);
                tilemap.SetTile(new Vector3Int(1, -1, 0), jellyDeathTile);
                tilemap.SetTile(new Vector3Int(2, -1, 0), iceDeathTile);
                tilemap.SetTile(new Vector3Int(3, -1, 0), disposableMarker);
                tilemap.SetTile(new Vector3Int(4, -1, 0), superJumpMarker);

                tilemap.RefreshAllTiles();
                PrefabUtility.SaveAsPrefabAsset(contents, PalettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // 이미 만들어진 아이템 프리팹의 색만 갱신한다 (팔레트 색 충돌 수정 등).
        private static void RecolorItemPrefab(GameObject prefabAsset, Color color)
        {
            var path = AssetDatabase.GetAssetPath(prefabAsset);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderer = contents.GetComponent<SpriteRenderer>();
                if (renderer == null || renderer.color == color) return;
                renderer.color = color;
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static PrefabMarkerTile CreateMarkerTile(string name, GameObject prefab, Color color)
        {
            EnsureFolder("Assets/Game/Tiles/Markers");
            var path = $"Assets/Game/Tiles/Markers/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrefabMarkerTile>(path);
            if (existing != null)
            {
                // 프리팹/색이 바뀌었으면 갱신한다 (팔레트 미리보기가 실제 배치물과 어긋나지 않도록).
                existing.SetData(prefab, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"), color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var marker = ScriptableObject.CreateInstance<PrefabMarkerTile>();
            marker.SetData(prefab, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"), color);
            AssetDatabase.CreateAsset(marker, path);
            return marker;
        }

        private static void CreateStageBrushAsset()
        {
            const string path = "Assets/Game/Tiles/Brushes/StageBrush.asset";
            if (AssetDatabase.LoadAssetAtPath<StageBrush>(path) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<StageBrush>(), path);
        }

        // 기믹 발판 (기믹 문서 §3). 타일이 아니라 프리팹이어야 칸마다 재생성 시간·배율을 따로 줄 수 있다.
        // 흰 타일 텍스처가 PPU 16의 16×16이라 스프라이트 한 장이 정확히 한 셀을 채운다.
        private static GameObject CreatePlatformPrefab(string name, Color color, System.Type gimmickComponent)
        {
            var path = $"{PrefabsDir}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteTexPath);
            sr.color = color;
            sr.sortingOrder = 5; // 타일 위, 아이템 아래
            var box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one; // 발판이므로 트리거가 아니다
            go.AddComponent(gimmickComponent);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
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

        private static void CreatePlayerPrefab(PropertyData defaultProperty)
        {
            const string resourcesDir = "Assets/Game/Resources";
            const string playerPath = "Assets/Game/Resources/Player.prefab";
            const string materialPath = "Assets/Game/Resources/PlayerPhysics.physicsMaterial2D";

            EnsureFolder(resourcesDir);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(playerPath) != null)
            {
                EnsurePlayerPropertyComponents(playerPath, defaultProperty);
                return;
            }

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

            var property = go.AddComponent<PlayerProperty>();
            property.SetDefaultProperty(defaultProperty);
            go.AddComponent<PlayerJellyAttach>();
            go.AddComponent<PlayerIceSlide>();
            go.AddComponent<PlayerHazardContact>();

            PrefabUtility.SaveAsPrefabAsset(go, playerPath);
            Object.DestroyImmediate(go);
        }

        // 기존 Player.prefab에 성질 시스템 컴포넌트가 없으면 추가한다 (이미 배포된 프리팹 보정용).
        private static void EnsurePlayerPropertyComponents(string playerPath, PropertyData defaultProperty)
        {
            var contents = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                bool changed = false;

                var property = contents.GetComponent<PlayerProperty>();
                if (property == null)
                {
                    property = contents.AddComponent<PlayerProperty>();
                    changed = true;
                }

                if (property.DefaultProperty == null)
                {
                    property.SetDefaultProperty(defaultProperty);
                    changed = true;
                }

                // E키 상호작용 컴포넌트를 쓰던 시절의 잔재 정리 (접촉 즉시 획득으로 전환, 기획 §11.1)
                if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(contents) > 0) changed = true;

                if (contents.GetComponent<PlayerJellyAttach>() == null)
                {
                    contents.AddComponent<PlayerJellyAttach>();
                    changed = true;
                }

                if (contents.GetComponent<PlayerIceSlide>() == null)
                {
                    contents.AddComponent<PlayerIceSlide>();
                    changed = true;
                }

                if (contents.GetComponent<PlayerHazardContact>() == null)
                {
                    contents.AddComponent<PlayerHazardContact>();
                    changed = true;
                }

                if (changed) PrefabUtility.SaveAsPrefabAsset(contents, playerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
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
