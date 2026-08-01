using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Game.Tests
{
    // 젤리 부착 실동작 (기획 §5)
    public class JellyAttachTests
    {
        private GameObject _gridGo;
        private GameObject _playerGo;
        private Player _player;
        private PlayerJellyAttach _attach;
        private Tilemap _tilemap;
        private SpecialTile _jellyTile;

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

            _jellyTile = ScriptableObject.CreateInstance<SpecialTile>();
            _jellyTile.colliderType = Tile.ColliderType.Grid;
            _jellyTile.SetTileProperty(TilePropertyType.Jelly);

            _playerGo = new GameObject("Player");
            _playerGo.AddComponent<Rigidbody2D>().freezeRotation = true;
            _playerGo.AddComponent<CircleCollider2D>().radius = 0.35f;
            _playerGo.AddComponent<PlayerStats>();
            _player = _playerGo.AddComponent<Player>();
            _playerGo.AddComponent<PlayerBounce>();
            _attach = _playerGo.AddComponent<PlayerJellyAttach>();
            _attach.ReadKeyboard = false;

            _player.PropertyType = PlayerPropertyType.Jelly;

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

        private void PaintRow(int y, int fromX, int toX)
        {
            for (int x = fromX; x <= toX; x++) _tilemap.SetTile(new Vector3Int(x, y, 0), _jellyTile);
            StageTiles.InvalidateCache();
        }

        private void PaintColumn(int x, int fromY, int toY)
        {
            for (int y = fromY; y <= toY; y++) _tilemap.SetTile(new Vector3Int(x, y, 0), _jellyTile);
            StageTiles.InvalidateCache();
        }

        private IEnumerator SettleFor(float seconds)
        {
            float deadline = Time.time + seconds;
            while (Time.time < deadline) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator 젤리_바닥에_붙고_중력이_멈춘다()
        {
            PaintRow(y: -2, fromX: -3, toX: 3);
            _playerGo.transform.position = new Vector3(0.5f, 0.2f, 0f);

            yield return SettleFor(1.5f);

            Assert.IsTrue(_attach.IsAttached, "젤리 바닥에 부착하지 않았습니다.");
            Assert.AreEqual(JellyAttachDirection.Floor, _attach.AttachDirection);
            Assert.AreEqual(PlayerState.Attached, _player.State);
            Assert.AreEqual(0f, _player.Body.gravityScale, "부착 중 중력이 0이어야 합니다.");
        }

        [UnityTest]
        public IEnumerator 젤리_천장에_붙는다()
        {
            PaintRow(y: 2, fromX: -3, toX: 3);
            _playerGo.transform.position = new Vector3(0.5f, 1.2f, 0f);
            _player.Body.linearVelocity = new Vector2(0f, 12f); // 위로 쏘아 올려 천장에 접촉시킨다

            yield return SettleFor(1.5f);

            Assert.IsTrue(_attach.IsAttached, "젤리 천장에 부착하지 않았습니다.");
            Assert.AreEqual(JellyAttachDirection.Ceiling, _attach.AttachDirection);
        }

        [UnityTest]
        public IEnumerator 젤리_벽에_붙고_AD가_상하_이동이_된다()
        {
            PaintColumn(x: 2, fromY: -3, toY: 3);
            _playerGo.transform.position = new Vector3(1.2f, 0.5f, 0f);
            _player.Body.linearVelocity = new Vector2(8f, 0f); // 벽으로 밀어붙인다

            yield return SettleFor(1.5f);

            Assert.IsTrue(_attach.IsAttached, "젤리 벽에 부착하지 않았습니다.");
            Assert.AreEqual(JellyAttachDirection.RightWall, _attach.AttachDirection);

            float startY = _playerGo.transform.position.y;
            _attach.SetInput(1f); // D = 위 (기획 §5.4)
            yield return SettleFor(0.5f);

            Assert.Greater(_playerGo.transform.position.y, startY + 0.2f, "벽에서 D 입력이 위로 이동시키지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 성질이_바뀌면_즉시_해제되고_떨어진다()
        {
            PaintRow(y: -2, fromX: -3, toX: 3);
            _playerGo.transform.position = new Vector3(0.5f, 0.2f, 0f);
            yield return SettleFor(1.5f);
            Assert.IsTrue(_attach.IsAttached, "사전 조건: 먼저 부착돼야 합니다.");

            _player.PropertyType = PlayerPropertyType.Default; // 기획 §7.2
            yield return SettleFor(0.3f);

            Assert.IsFalse(_attach.IsAttached, "성질이 바뀌었는데 부착이 유지됐습니다.");
            Assert.AreNotEqual(PlayerState.Attached, _player.State);
            Assert.AreNotEqual(0f, _player.Body.gravityScale, "중력이 복구되지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 부착_해제_순간에는_자동_점프가_없다()
        {
            PaintRow(y: -2, fromX: -3, toX: 3);
            _playerGo.transform.position = new Vector3(0.5f, 0.2f, 0f);
            yield return SettleFor(1.5f);
            Assert.IsTrue(_attach.IsAttached);

            _attach.Release();
            yield return new WaitForFixedUpdate();

            // 기획 §5.6: 해제 순간 자동 점프를 발생시키지 않는다 (위로 튀면 안 됨)
            Assert.LessOrEqual(_player.Body.linearVelocity.y, 0.1f, "해제 순간 자동 점프가 발생했습니다.");
        }

        [UnityTest]
        public IEnumerator 젤리_바닥_끝을_지나면_해제되고_떨어진다()
        {
            // 기획 §5.6: 젤리 타일의 끝을 벗어나면 부착을 해제한다
            PaintRow(y: -2, fromX: 0, toX: 2);
            _playerGo.transform.position = new Vector3(0.5f, 0.2f, 0f);
            yield return SettleFor(1.5f);
            Assert.IsTrue(_attach.IsAttached, "사전 조건: 먼저 바닥에 부착돼야 합니다.");

            _attach.SetInput(1f); // 오른쪽 끝(x=3)을 향해 계속 이동
            yield return SettleFor(2.5f);

            Assert.IsFalse(_attach.IsAttached, "타일 끝을 지났는데도 부착이 유지됩니다 (모서리를 감아 돌고 있음).");
            Assert.Less(_playerGo.transform.position.y, -1.5f, "해제 후 낙하하지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 젤리_벽_위쪽_끝을_지나면_해제된다()
        {
            // 기둥 꼭대기를 넘어 올라가면 벗어나야 한다 (기획 §5.6)
            PaintColumn(x: 2, fromY: -1, toY: 1);
            _playerGo.transform.position = new Vector3(1.2f, 0.5f, 0f);
            _player.Body.linearVelocity = new Vector2(8f, 0f);
            yield return SettleFor(1.5f);
            Assert.IsTrue(_attach.IsAttached, "사전 조건: 먼저 벽에 부착돼야 합니다.");
            Assert.AreEqual(JellyAttachDirection.RightWall, _attach.AttachDirection);

            _attach.SetInput(1f); // 벽에서 D = 위
            yield return SettleFor(2.5f);

            Assert.IsFalse(_attach.IsAttached, "벽 위쪽 끝을 지났는데도 부착이 유지됩니다.");
        }

        [UnityTest]
        public IEnumerator 젤리_천장_끝을_지나면_해제된다()
        {
            PaintRow(y: 2, fromX: 0, toX: 2);
            _playerGo.transform.position = new Vector3(0.5f, 1.2f, 0f);
            _player.Body.linearVelocity = new Vector2(0f, 12f);
            yield return SettleFor(1.5f);
            Assert.IsTrue(_attach.IsAttached, "사전 조건: 먼저 천장에 부착돼야 합니다.");
            Assert.AreEqual(JellyAttachDirection.Ceiling, _attach.AttachDirection);

            _attach.SetInput(1f); // 천장에서 D = 오른쪽 (기획 §5.4)
            yield return SettleFor(2.5f);

            Assert.IsFalse(_attach.IsAttached, "천장 끝을 지났는데도 부착이 유지됩니다.");
            Assert.Less(_playerGo.transform.position.y, 1.9f, "해제 후 낙하하지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator 고립된_젤리_타일을_무한히_맴돌지_않는다()
        {
            // 한 칸짜리 젤리 타일에 붙어 계속 이동하면 결국 벗어나야 한다
            _tilemap.SetTile(new Vector3Int(0, -2, 0), _jellyTile);
            StageTiles.InvalidateCache();
            _playerGo.transform.position = new Vector3(0.5f, 0.2f, 0f);
            yield return SettleFor(1.5f);
            Assert.IsTrue(_attach.IsAttached, "사전 조건: 먼저 부착돼야 합니다.");

            _attach.SetInput(1f);
            yield return SettleFor(2.5f);

            Assert.IsFalse(_attach.IsAttached, "한 칸짜리 타일 주위를 계속 맴돌고 있습니다.");
        }

        [UnityTest]
        public IEnumerator 기본_성질은_젤리_타일에_붙지_않는다()
        {
            _player.PropertyType = PlayerPropertyType.Default;
            PaintRow(y: -2, fromX: -3, toX: 3);
            _playerGo.transform.position = new Vector3(0.5f, 0.2f, 0f);

            yield return SettleFor(1.5f);

            Assert.IsFalse(_attach.IsAttached, "기본 성질인데 부착했습니다.");
            Assert.AreNotEqual(0f, _player.Body.gravityScale);
        }

        // 벽의 반대쪽 면에서는 상하가 뒤집혀야 한다.
        // enum LeftWall = 벽이 플레이어의 왼쪽 = 플레이어는 벽의 우측면에 붙어 있다.
        [UnityTest]
        public IEnumerator 벽_우측면에서는_D가_아래로_내려간다()
        {
            PaintColumn(x: -2, fromY: -3, toY: 3);
            _playerGo.transform.position = new Vector3(-0.2f, 0.5f, 0f);
            _player.Body.linearVelocity = new Vector2(-8f, 0f); // 벽으로 밀어붙인다

            yield return SettleFor(1.5f);

            Assert.IsTrue(_attach.IsAttached, "젤리 벽에 부착하지 않았습니다.");
            Assert.AreEqual(JellyAttachDirection.LeftWall, _attach.AttachDirection);

            float startY = _playerGo.transform.position.y;
            _attach.SetInput(1f); // D
            yield return SettleFor(0.5f);
            Assert.Less(_playerGo.transform.position.y, startY - 0.2f,
                "벽 우측면에서 D는 내려가야 합니다.");

            startY = _playerGo.transform.position.y;
            _attach.SetInput(-1f); // A
            yield return SettleFor(0.5f);
            Assert.Greater(_playerGo.transform.position.y, startY + 0.2f,
                "벽 우측면에서 A는 올라가야 합니다.");
        }

        // 부착 중에는 PlayerMovement가 빠지므로, 기어가는 방향으로 직접 바라봐야 한다.
        [UnityTest]
        public IEnumerator 바닥을_기어갈_때_이동_방향을_바라본다()
        {
            PaintRow(y: -2, fromX: -3, toX: 3);
            _playerGo.transform.position = new Vector3(0.5f, 0.2f, 0f);
            yield return SettleFor(1.5f);
            Assert.IsTrue(_attach.IsAttached, "젤리 바닥에 부착하지 않았습니다.");

            _player.FacingDirection = -1f; // 반대쪽을 보게 해두고 시작
            _attach.SetInput(1f);
            yield return SettleFor(0.3f);
            Assert.AreEqual(1f, _player.FacingDirection, "오른쪽으로 기어가는데 방향이 바뀌지 않았습니다.");

            _attach.SetInput(-1f);
            yield return SettleFor(0.3f);
            Assert.AreEqual(-1f, _player.FacingDirection, "왼쪽으로 기어가는데 방향이 바뀌지 않았습니다.");
        }

        // 천장은 바닥과 같은 화면 기준 좌우를 쓴다.
        [UnityTest]
        public IEnumerator 천장을_기어갈_때도_이동_방향을_바라본다()
        {
            PaintRow(y: 2, fromX: -3, toX: 3);
            _playerGo.transform.position = new Vector3(0.5f, 1.2f, 0f);
            _player.Body.linearVelocity = new Vector2(0f, 12f);
            yield return SettleFor(1.5f);
            Assert.AreEqual(JellyAttachDirection.Ceiling, _attach.AttachDirection);

            _player.FacingDirection = -1f;
            _attach.SetInput(1f);
            yield return SettleFor(0.3f);
            Assert.AreEqual(1f, _player.FacingDirection, "천장에서 D는 오른쪽을 봐야 합니다.");
        }

        // 벽에서는 상하 이동뿐이라 좌우 방향을 바꿀 근거가 없다 — 직전 방향을 유지해야 한다.
        [UnityTest]
        public IEnumerator 벽에서는_바라보는_방향이_유지된다()
        {
            PaintColumn(x: 2, fromY: -3, toY: 3);
            _playerGo.transform.position = new Vector3(1.2f, 0.5f, 0f);
            _player.Body.linearVelocity = new Vector2(8f, 0f);
            yield return SettleFor(1.5f);
            Assert.AreEqual(JellyAttachDirection.RightWall, _attach.AttachDirection);

            _player.FacingDirection = 1f;
            _attach.SetInput(1f);
            yield return SettleFor(0.3f);

            Assert.AreEqual(1f, _player.FacingDirection, "벽에서 상하로 움직였는데 방향이 바뀌었습니다.");
        }
    }
}
