using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Game.Tests
{
    // 가시 타일 접촉 방향별 사망 판정 (기믹 문서 §4).
    // EditMode 쪽 SpecialTileTests는 노멀을 직접 넣어 로직만 본다 — 여기서는 실제 물리 접촉에서
    // 나온 노멀로 같은 판정을 돌려, 노멀 방향 규약과 타일 조회(StageTiles)까지 함께 검증한다.
    public class SpikeTileHazardTests
    {
        private GameObject _gridGo;
        private GameObject _playerGo;
        private Tilemap _tilemap;
        private Rigidbody2D _body;
        private ContactSpy _spy;

        // 실제 충돌에서 나온 접촉점을 그대로 모아두는 헬퍼.
        private class ContactSpy : MonoBehaviour
        {
            public readonly List<ContactPoint2D> Contacts = new();

            private void OnCollisionEnter2D(Collision2D collision)
            {
                for (int i = 0; i < collision.contactCount; i++) Contacts.Add(collision.GetContact(i));
            }
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                Object.Destroy(systems);
                yield return null;
            }

            _gridGo = new GameObject("Grid");
            _gridGo.AddComponent<Grid>();
            var mapGo = new GameObject("Ground");
            mapGo.transform.SetParent(_gridGo.transform);
            _tilemap = mapGo.AddComponent<Tilemap>();
            mapGo.AddComponent<TilemapRenderer>();
            mapGo.AddComponent<TilemapCollider2D>();

            _playerGo = new GameObject("Player");
            _body = _playerGo.AddComponent<Rigidbody2D>();
            _body.freezeRotation = true;
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _spy = _playerGo.AddComponent<ContactSpy>();

            StageTiles.InvalidateCache();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_playerGo);
            Object.Destroy(_gridGo);
            yield return null;
            StageTiles.InvalidateCache();
        }

        // 실제 가시 타일과 같은 설정: 사망 발판이되 아랫면 접촉은 살려준다.
        private static SpecialTile CreateSpikeTile()
        {
            var tile = ScriptableObject.CreateInstance<SpecialTile>();
            tile.colliderType = Tile.ColliderType.Grid;
            tile.SetTileProperty(TilePropertyType.Default);
            tile.SetDeadly(true, applySurfaceEffect: true, fromBelow: false);
            return tile;
        }

        // 한 줄을 깔고 플레이어를 원하는 속도로 밀어 첫 접촉이 잡힐 때까지 기다린다.
        private IEnumerator Collide(SpecialTile tile, int tileRow, Vector3 startPosition, Vector2 velocity, float gravity)
        {
            for (int x = -3; x <= 3; x++) _tilemap.SetTile(new Vector3Int(x, tileRow, 0), tile);
            StageTiles.InvalidateCache();
            yield return new WaitForFixedUpdate();

            _playerGo.transform.position = startPosition;
            _body.gravityScale = gravity;
            _body.linearVelocity = velocity;
            _spy.Contacts.Clear();

            float deadline = Time.time + 3f;
            while (Time.time < deadline && _spy.Contacts.Count == 0) yield return new WaitForFixedUpdate();

            Assert.IsNotEmpty(_spy.Contacts, "타일에 닿지 않았습니다 — 테스트 설정을 확인하세요.");
        }

        // 접촉 하나라도 사망 판정이면 PlayerHazardContact가 죽인다 — 같은 방식으로 집계한다.
        private bool AnyContactLethal(PlayerPropertyType property)
        {
            foreach (var contact in _spy.Contacts)
            {
                var tile = StageTiles.GetSpecialTileAt(contact.point, contact.normal);
                if (tile != null && tile.IsLethalOnContact(property, contact.normal)) return true;
            }
            return false;
        }

        [UnityTest]
        public IEnumerator 가시_윗면에_착지하면_죽는다()
        {
            var spike = CreateSpikeTile();
            yield return Collide(spike, tileRow: -2, startPosition: new Vector3(0.5f, 0.5f, 0f),
                velocity: Vector2.zero, gravity: 1f);

            Assert.IsTrue(AnyContactLethal(PlayerPropertyType.Default),
                "가시 윗면에 착지했는데 사망 판정이 나지 않았습니다 (기믹 문서 §4).");
        }

        [UnityTest]
        public IEnumerator 가시_아랫면을_천장으로_받으면_죽지_않는다()
        {
            var spike = CreateSpikeTile();
            // 타일 줄을 머리 위에 깔고 위로 쏘아 올려 밑면에 부딪히게 한다.
            yield return Collide(spike, tileRow: 2, startPosition: new Vector3(0.5f, 0.5f, 0f),
                velocity: new Vector2(0f, 8f), gravity: 0f);

            // 타일을 못 찾아서 통과하는 것이 아님을 먼저 못박는다 — 아랫면 접촉이 실제로 잡혔고,
            // 그 접촉에서 가시 타일도 조회됐는데, 방향 때문에 살아남아야 한다.
            bool sawSpikeFromBelow = false;
            foreach (var contact in _spy.Contacts)
            {
                if (contact.normal.y > -0.5f) continue;
                if (StageTiles.GetSpecialTileAt(contact.point, contact.normal) == spike) sawSpikeFromBelow = true;
            }
            Assert.IsTrue(sawSpikeFromBelow,
                "아랫면 접촉에서 가시 타일이 조회되지 않았습니다 — 테스트가 헛돌고 있습니다.");

            Assert.IsFalse(AnyContactLethal(PlayerPropertyType.Default),
                "가시 아랫면을 천장으로 받았는데 사망했습니다 — 가시는 위·옆에만 돋아 있습니다.");
        }

        [UnityTest]
        public IEnumerator 가시_옆면에_부딪히면_죽는다()
        {
            var spike = CreateSpikeTile();
            // 한 칸만 깔고 옆에서 밀어붙인다.
            _tilemap.SetTile(new Vector3Int(0, 0, 0), spike);
            StageTiles.InvalidateCache();
            yield return new WaitForFixedUpdate();

            _playerGo.transform.position = new Vector3(2.5f, 0.5f, 0f);
            _body.gravityScale = 0f;
            _body.linearVelocity = new Vector2(-4f, 0f);
            _spy.Contacts.Clear();

            float deadline = Time.time + 3f;
            while (Time.time < deadline && _spy.Contacts.Count == 0) yield return new WaitForFixedUpdate();
            Assert.IsNotEmpty(_spy.Contacts, "타일에 닿지 않았습니다 — 테스트 설정을 확인하세요.");

            Assert.IsTrue(AnyContactLethal(PlayerPropertyType.Default),
                "가시 옆면에 부딪혔는데 사망 판정이 나지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 생존_성질은_가시_윗면에서도_안전하다()
        {
            var spike = CreateSpikeTile();
            spike.SetTileProperty(TilePropertyType.Jelly); // 젤리 가시 → 젤리만 생존 (기믹 문서 §5)

            yield return Collide(spike, tileRow: -2, startPosition: new Vector3(0.5f, 0.5f, 0f),
                velocity: Vector2.zero, gravity: 1f);

            Assert.IsFalse(AnyContactLethal(PlayerPropertyType.Jelly), "젤리 가시에서 젤리가 죽었습니다.");
            Assert.IsTrue(AnyContactLethal(PlayerPropertyType.Default), "젤리 가시에서 기본 성질이 살았습니다.");
        }
    }
}
