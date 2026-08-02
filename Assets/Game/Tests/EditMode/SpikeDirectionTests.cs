using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class SpikeDirectionTests
    {
        private static SpecialTile MakeSpike(TilePropertyType property, bool allDirections)
        {
            var tile = ScriptableObject.CreateInstance<SpecialTile>();
            tile.SetTileProperty(property);
            tile.SetDeadly(true, applySurfaceEffect: true, fromBelow: allDirections);
            return tile;
        }

        // ── 등면 판정 (가시 반대면만 안전) ──

        [Test]
        public void 위_가시는_밟으면_죽고_아래서_받으면_산다()
        {
            // 원본 배치: 가시 위. 위에서 밟은 접촉 법선은 위, 아래서 받으면 아래
            Assert.IsFalse(SpecialTile.IsBackContact(Vector2.up, Vector2.up));    // 가시면 → 사망
            Assert.IsTrue(SpecialTile.IsBackContact(Vector2.down, Vector2.up));   // 등면 → 생존
        }

        [Test]
        public void 천장_가시는_점프해서_받으면_죽고_위를_밟으면_산다()
        {
            // 180도 회전 배치: 가시 아래
            Assert.IsFalse(SpecialTile.IsBackContact(Vector2.down, Vector2.down)); // 아래서 받음 → 사망
            Assert.IsTrue(SpecialTile.IsBackContact(Vector2.up, Vector2.down));    // 등면(윗면) → 생존
        }

        [Test]
        public void 옆_가시는_달려들면_죽고_등면은_산다()
        {
            // 90도 회전 배치: 가시 왼쪽 — 오른쪽에서 달려온 플레이어의 접촉 법선은 왼쪽
            Assert.IsFalse(SpecialTile.IsBackContact(Vector2.left, Vector2.left)); // 가시면 → 사망
            Assert.IsTrue(SpecialTile.IsBackContact(Vector2.right, Vector2.left)); // 등면 → 생존
        }

        [Test]
        public void 가시_빗면의_대각_접촉도_사망_쪽이다()
        {
            // 천장 가시의 삼각 빗면: 법선이 대각 (0.7, -0.7)
            var slope = new Vector2(0.7f, -0.7f).normalized;
            Assert.IsFalse(SpecialTile.IsBackContact(slope, Vector2.down));
        }

        [Test]
        public void 가시_옆면_접촉은_사망_쪽이다()
        {
            // 위 가시 타일의 좌우 측면 — 기존 동작과 동일하게 사망 유지
            Assert.IsFalse(SpecialTile.IsBackContact(Vector2.left, Vector2.up));
            Assert.IsFalse(SpecialTile.IsBackContact(Vector2.right, Vector2.up));
        }

        // ── IsLethalOnContact 통합 ──

        [Test]
        public void 천장_가시_사망_판정_통합()
        {
            var tile = MakeSpike(TilePropertyType.Default, allDirections: false);
            try
            {
                Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.down, Vector2.down));
                Assert.IsFalse(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.up, Vector2.down));
            }
            finally { Object.DestroyImmediate(tile); }
        }

        [Test]
        public void 전방향_사망_타일은_등면에서도_죽는다()
        {
            var tile = MakeSpike(TilePropertyType.Default, allDirections: true);
            try
            {
                Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.up, Vector2.down));
            }
            finally { Object.DestroyImmediate(tile); }
        }

        [Test]
        public void 면제_성질은_방향과_무관하게_생존한다()
        {
            var tile = MakeSpike(TilePropertyType.Jelly, allDirections: false);
            try
            {
                Assert.IsFalse(tile.IsLethalOnContact(PlayerPropertyType.Jelly, Vector2.up, Vector2.up));
                Assert.IsTrue(tile.IsLethalOnContact(PlayerPropertyType.Default, Vector2.up, Vector2.up));
            }
            finally { Object.DestroyImmediate(tile); }
        }

        // ── 셀 회전 행렬 → 가시 방향 ──

        [Test]
        public void 회전_행렬이_가시_방향을_돌린다()
        {
            var up = StageTiles.SpikeDirectionFrom(Matrix4x4.identity);
            var down = StageTiles.SpikeDirectionFrom(Matrix4x4.Rotate(Quaternion.Euler(0, 0, 180)));
            var left = StageTiles.SpikeDirectionFrom(Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90)));
            var right = StageTiles.SpikeDirectionFrom(Matrix4x4.Rotate(Quaternion.Euler(0, 0, -90)));

            Assert.Less(Vector2.Distance(up, Vector2.up), 0.001f);
            Assert.Less(Vector2.Distance(down, Vector2.down), 0.001f);
            Assert.Less(Vector2.Distance(left, Vector2.left), 0.001f);
            Assert.Less(Vector2.Distance(right, Vector2.right), 0.001f);
        }

        [Test]
        public void 상하_반전_배치도_가시가_아래를_향한다()
        {
            var flipped = StageTiles.SpikeDirectionFrom(Matrix4x4.Scale(new Vector3(1, -1, 1)));
            Assert.Less(Vector2.Distance(flipped, Vector2.down), 0.001f);
        }
    }
}
