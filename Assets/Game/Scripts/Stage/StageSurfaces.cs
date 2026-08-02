using UnityEngine;

namespace Game
{
    // 접촉·레이캐스트가 닿은 표면의 성질 정보. 타일맵 타일과 프리팹 콜라이더를 하나의 답으로 통일한다.
    public readonly struct SurfaceInfo
    {
        public readonly TilePropertyType Property;
        private readonly bool _isDeadly;
        private readonly bool _applySurfaceEffectWhenSafe;

        public SurfaceInfo(TilePropertyType property, bool isDeadly, bool applySurfaceEffectWhenSafe)
        {
            Property = property;
            _isDeadly = isDeadly;
            _applySurfaceEffectWhenSafe = applySurfaceEffectWhenSafe;
        }

        // 부착·미끄러짐 같은 표면 효과를 적용할지. SpecialTile.AppliesSurfaceEffectFor와 같은 규칙.
        public bool AppliesSurfaceEffectFor(PlayerPropertyType player)
            => !_isDeadly
               || (_applySurfaceEffectWhenSafe && SpecialTile.IsSafeCombination(Property, player));
    }

    // 표면 성질 조회의 단일 진입점.
    // 프리팹 콜라이더(HazardSurface·SurfaceProperty)가 우선이고, 없으면 타일맵을 조회한다.
    public static class StageSurfaces
    {
        public static SurfaceInfo Resolve(Collider2D collider, Vector2 contactPoint, Vector2 contactNormal)
        {
            if (collider != null)
            {
                var hazard = collider.GetComponent<HazardSurface>();
                if (hazard != null)
                    return new SurfaceInfo(hazard.TileProperty, isDeadly: true, hazard.ApplySurfaceEffectWhenSafe);

                var surface = collider.GetComponent<SurfaceProperty>();
                if (surface != null)
                    return new SurfaceInfo(surface.TileProperty, isDeadly: false, applySurfaceEffectWhenSafe: true);
            }

            var tile = StageTiles.GetSpecialTileAt(contactPoint, contactNormal);
            if (tile != null)
                return new SurfaceInfo(tile.TileProperty, tile.IsDeadly, tile.ApplySurfaceEffectWhenSafe);

            return new SurfaceInfo(TilePropertyType.Default, isDeadly: false, applySurfaceEffectWhenSafe: true);
        }
    }
}
