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
