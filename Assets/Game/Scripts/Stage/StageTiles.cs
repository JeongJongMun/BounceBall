using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game
{
    // 월드 좌표에서 스테이지 타일을 조회하는 헬퍼. 착지 반응 판정에 사용한다.
    public static class StageTiles
    {
        private static readonly List<Tilemap> Cache = new();
        private static bool _cacheValid;

        static StageTiles()
        {
            SceneManager.sceneLoaded += (_, _) => InvalidateCache();
            SceneManager.sceneUnloaded += _ => InvalidateCache();
        }

        public static void InvalidateCache() => _cacheValid = false;

        // 접촉 지점에서 노멀 반대 방향(타일 내부)으로 살짝 들어간 위치의 특수 타일을 반환한다.
        public static SpecialTile GetSpecialTileAt(Vector2 contactPoint, Vector2 contactNormal)
        {
            var probe = contactPoint - contactNormal * 0.25f;
            foreach (var tilemap in GetTilemaps())
            {
                if (tilemap == null) continue;
                var cell = tilemap.WorldToCell(probe);
                if (tilemap.GetTile(cell) is SpecialTile special) return special;
            }
            return null;
        }

        // 씬에 깔린 타일 전체를 감싸는 월드 영역. 타일이 하나도 없으면 false.
        // 스테이지 경계(여백 포함)와 달리 실제로 타일이 있는 범위만 잡는다.
        public static bool TryGetWorldBounds(out Bounds bounds)
        {
            bounds = default;
            bool found = false;

            foreach (var tilemap in GetTilemaps())
            {
                if (tilemap == null) continue;

                tilemap.CompressBounds(); // 지운 타일까지 세지 않도록 실제 범위로 줄인다
                var local = tilemap.localBounds;
                if (local.size.x <= 0f || local.size.y <= 0f) continue;

                var min = tilemap.transform.TransformPoint(local.min);
                var max = tilemap.transform.TransformPoint(local.max);

                if (!found)
                {
                    bounds = new Bounds(min, Vector3.zero);
                    found = true;
                }

                bounds.Encapsulate(min);
                bounds.Encapsulate(max);
            }

            return found;
        }

        private static List<Tilemap> GetTilemaps()
        {
            if (!_cacheValid)
            {
                Cache.Clear();
                Cache.AddRange(Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None));
                _cacheValid = true;
            }
            return Cache;
        }
    }
}
