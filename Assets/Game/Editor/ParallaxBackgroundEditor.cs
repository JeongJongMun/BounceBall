using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    // 배경 프롭 배치를 시드로 다시 만든다. 마음에 드는 그림이 나올 때까지 시드만 바꾸면 된다.
    [CustomEditor(typeof(ParallaxBackground))]
    public class ParallaxBackgroundEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var background = (ParallaxBackground)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("배경 다시 생성", GUILayout.Height(24)))
                Rebuild(background);

            EditorGUILayout.HelpBox(
                "지평선은 실행할 때 스테이지 경계에 맞춰 자동 정렬됩니다.\n" +
                "시드를 바꾸고 [배경 다시 생성]을 누르면 배치가 새로 만들어집니다.",
                MessageType.Info);
        }

        public static void Rebuild(ParallaxBackground background)
        {
            Undo.RegisterFullObjectHierarchyUndo(background.gameObject, "배경 다시 생성");

            // 기존 레이어를 지우고 새로 만든다
            for (int i = background.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(background.transform.GetChild(i).gameObject);

            int layerIndex = 0;
            foreach (var config in background.Layers)
            {
                BuildLayer(background.transform, config, background.Seed + layerIndex * 977);
                layerIndex++;
            }

            EditorUtility.SetDirty(background);
        }

        private static void BuildLayer(Transform parent, ParallaxBackground.LayerConfig config, int seed)
        {
            var layerObject = new GameObject(config.layerName);
            layerObject.transform.SetParent(parent, false);

            var layer = layerObject.AddComponent<ParallaxLayer>();
            layer.SetLayout(config.horizontalFactor, config.verticalFactor, config.repeatWidth, config.autoScrollSpeed);

            if (config.sprites == null || config.sprites.Length == 0 || config.count <= 0) return;

            var random = new System.Random(seed);
            var props = new List<(float x, float y, float scale, Sprite sprite)>();

            for (int i = 0; i < config.count; i++)
            {
                float x = ParallaxBackground.PropX(i, config.count, config.repeatWidth, (float)random.NextDouble());
                float y = config.baseY + ((float)random.NextDouble() * 2f - 1f) * config.yJitter;
                float scale = config.baseScale;
                var sprite = config.sprites[random.Next(config.sprites.Length)];
                props.Add((x, y, scale, sprite));
            }

            // 아래에 있는 프롭일수록 앞에 그려야 겹침이 자연스럽다
            props.Sort((a, b) => b.y.CompareTo(a.y));

            for (int i = 0; i < props.Count; i++)
            {
                var p = props[i];
                if (p.sprite == null) continue;

                var propObject = new GameObject(config.layerName + "_" + i);
                propObject.transform.SetParent(layerObject.transform, false);
                propObject.transform.localPosition = new Vector3(p.x, p.y, 0f);
                propObject.transform.localScale = Vector3.one * p.scale;

                var renderer = propObject.AddComponent<SpriteRenderer>();
                renderer.sprite = p.sprite;
                renderer.sortingOrder = config.sortingOrder + i;
            }
        }
    }
}
