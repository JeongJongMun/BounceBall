# 게임잼 코어 템플릿 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 게임잼에서 바로 재사용 가능한 코어 시스템 템플릿(`Assets/Core`)을 구축한다 — 매니저 자동 로드, 씬 전환, 오디오, 게임 상태, 오브젝트 풀, UI, 이벤트 채널, 저장.

**Architecture:** `[RuntimeInitializeOnLoadMethod]`가 `Resources/Systems.prefab`을 자동 생성해 어느 씬에서든 매니저가 존재. 시스템 간 통신은 ScriptableObject 이벤트 채널. 프리팹/에셋은 손으로 만들지 않고 에디터 생성 스크립트(메뉴 항목)로 재현 가능하게 생성.

**Tech Stack:** Unity 6000.5.6f1, DOTween(무료판, 사용자 임포트), Input System 1.20, uGUI 2.5(TMP 내장), Unity Test Framework 1.7

## Global Constraints

- Unity 버전: 6000.5.6f1, 프로젝트 경로 `D:\UnityProjects\BounceBall`
- 모든 코드는 `Assets/Core/` 하위, 루트 네임스페이스 `Core`
- Phase 2(Task 6) 이전 코드는 DOTween을 참조하면 안 됨 — 그 전까지 프로젝트가 컴파일되어야 테스트 실행 가능
- DOTween은 사용자가 Asset Store에서 임포트 + Setup 실행 (Task 6 체크포인트)
- 검증: Unity 에디터가 열려 있으면 unity-mcp(콘솔 읽기, run_tests) 사용, 아니면 Unity CLI 배치 모드
- 커밋 메시지는 한국어, `feat:`/`test:`/`docs:` prefix, Co-Authored-By 푸터 포함
- YAGNI: Addressables, JSON 세이브, 오디오 믹서, 네트워킹 제외

## File Structure

```
Assets/Core/
├── Scripts/
│   ├── Core.asmdef                       # Task 1 (DOTween 참조는 Task 6에서 추가)
│   ├── Systems/Singleton.cs              # Task 1
│   ├── Systems/Systems.cs                # Task 1
│   ├── Save/SaveData.cs                  # Task 2
│   ├── Events/VoidEventChannel.cs        # Task 3
│   ├── Events/IntEventChannel.cs         # Task 3
│   ├── Events/FloatEventChannel.cs       # Task 3
│   ├── Game/GameManager.cs               # Task 4
│   ├── Pool/PoolManager.cs               # Task 5
│   ├── Pool/PooledObject.cs              # Task 5
│   ├── Scene/SceneLoader.cs              # Task 7
│   ├── Audio/AudioManager.cs             # Task 8
│   ├── UI/UIScreen.cs                    # Task 9
│   ├── UI/MainMenuScreen.cs              # Task 9
│   ├── UI/PauseScreen.cs                 # Task 9
│   ├── UI/ResultScreen.cs                # Task 9
│   ├── UI/ScoreHud.cs                    # Task 9
│   └── UI/UIManager.cs                   # Task 9
├── Editor/
│   ├── Core.Editor.asmdef                # Task 10
│   └── CoreTemplateGenerator.cs          # Task 10
├── Tests/
│   ├── EditMode/Core.Tests.EditMode.asmdef   # Task 2
│   ├── EditMode/SaveDataTests.cs             # Task 2
│   ├── EditMode/EventChannelTests.cs         # Task 3
│   ├── PlayMode/Core.Tests.PlayMode.asmdef   # Task 4
│   ├── PlayMode/GameManagerTests.cs          # Task 4
│   └── PlayMode/PoolManagerTests.cs          # Task 5
├── Scripts/Demo/DemoController.cs        # Task 11
├── Resources/Systems.prefab              # Task 10 (생성 스크립트 산출물)
├── Events/*.asset                        # Task 10 (생성 스크립트 산출물)
├── Scenes/Demo.unity                     # Task 11
└── README.md                             # Task 11
```

## 검증 방법 (모든 태스크 공통)

에디터가 열려 있으면 unity-mcp로 컴파일 에러 확인 및 테스트 실행. 닫혀 있으면 CLI:

```bash
# EditMode 테스트
"C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Unity.exe" -batchmode -projectPath "D:/UnityProjects/BounceBall" -runTests -testPlatform EditMode -testResults "D:/UnityProjects/BounceBall/TestResults-EditMode.xml" -logFile "D:/UnityProjects/BounceBall/Logs/test-run.log"
# PlayMode 테스트는 -testPlatform PlayMode
```

결과 XML의 `result="Passed"` 확인. 컴파일 에러는 로그 파일에서 `CS[0-9]+` 검색.

---

### Task 1: asmdef + Singleton + Systems 자동 로더

**Files:**
- Create: `Assets/Core/Scripts/Core.asmdef`
- Create: `Assets/Core/Scripts/Systems/Singleton.cs`
- Create: `Assets/Core/Scripts/Systems/Systems.cs`

**Interfaces:**
- Produces: `Core.Singleton<T>` (`Instance` 정적 프로퍼티, `protected virtual Awake/OnDestroy`), `Core.Systems`(자동 부트스트랩, 공개 API 없음). 이후 모든 매니저가 `Singleton<T>` 상속.

- [ ] **Step 1: Core.asmdef 작성** (DOTween 참조 없음 — Task 6에서 추가)

```json
{
    "name": "Core",
    "rootNamespace": "Core",
    "references": [],
    "autoReferenced": true
}
```

- [ ] **Step 2: Singleton.cs 작성**

```csharp
using UnityEngine;

namespace Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
```

- [ ] **Step 3: Systems.cs 작성**

```csharp
using UnityEngine;

namespace Core
{
    public static class Systems
    {
        public const string PrefabPath = "Systems";
        private static GameObject _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[Core] Resources/Systems.prefab 이 없습니다. Core > Generate Template Assets 메뉴를 실행하세요.");
                return;
            }
            _instance = Object.Instantiate(prefab);
            _instance.name = "Systems";
            Object.DontDestroyOnLoad(_instance);
        }
    }
}
```

- [ ] **Step 4: 컴파일 확인** — Unity 에디터/CLI에서 컴파일 에러 없음 확인
- [ ] **Step 5: Commit** — `feat: Core asmdef + Singleton + Systems 자동 로더 추가`

---

### Task 2: SaveData + EditMode 테스트

**Files:**
- Create: `Assets/Core/Scripts/Save/SaveData.cs`
- Create: `Assets/Core/Tests/EditMode/Core.Tests.EditMode.asmdef`
- Test: `Assets/Core/Tests/EditMode/SaveDataTests.cs`

**Interfaces:**
- Produces: `Core.SaveData` static — `int HighScore`, `float MasterVolume/BgmVolume/SfxVolume`(기본 1f), `void ResetAll()`

- [ ] **Step 1: 테스트 asmdef 작성**

```json
{
    "name": "Core.Tests.EditMode",
    "rootNamespace": "Core.Tests",
    "references": ["Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"],
    "precompiledReferences": ["nunit.framework.dll"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "overrideReferences": true
}
```

- [ ] **Step 2: 실패하는 테스트 작성**

```csharp
using Core;
using NUnit.Framework;

namespace Core.Tests
{
    public class SaveDataTests
    {
        [SetUp] public void SetUp() => SaveData.ResetAll();
        [TearDown] public void TearDown() => SaveData.ResetAll();

        [Test]
        public void HighScore_기본값은_0()
        {
            Assert.AreEqual(0, SaveData.HighScore);
        }

        [Test]
        public void HighScore_저장하면_읽을_수_있다()
        {
            SaveData.HighScore = 1234;
            Assert.AreEqual(1234, SaveData.HighScore);
        }

        [Test]
        public void 볼륨_기본값은_1()
        {
            Assert.AreEqual(1f, SaveData.MasterVolume);
            Assert.AreEqual(1f, SaveData.BgmVolume);
            Assert.AreEqual(1f, SaveData.SfxVolume);
        }

        [Test]
        public void ResetAll_후_기본값으로_복귀()
        {
            SaveData.HighScore = 99;
            SaveData.SfxVolume = 0.5f;
            SaveData.ResetAll();
            Assert.AreEqual(0, SaveData.HighScore);
            Assert.AreEqual(1f, SaveData.SfxVolume);
        }
    }
}
```

- [ ] **Step 3: 테스트 실행 → 컴파일 실패 확인** (SaveData 미존재)
- [ ] **Step 4: SaveData.cs 구현**

```csharp
using UnityEngine;

namespace Core
{
    public static class SaveData
    {
        private const string HighScoreKey = "core.highscore";
        private const string MasterVolumeKey = "core.volume.master";
        private const string BgmVolumeKey = "core.volume.bgm";
        private const string SfxVolumeKey = "core.volume.sfx";

        public static int HighScore
        {
            get => PlayerPrefs.GetInt(HighScoreKey, 0);
            set { PlayerPrefs.SetInt(HighScoreKey, value); PlayerPrefs.Save(); }
        }

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            set { PlayerPrefs.SetFloat(MasterVolumeKey, value); PlayerPrefs.Save(); }
        }

        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            set { PlayerPrefs.SetFloat(BgmVolumeKey, value); PlayerPrefs.Save(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            set { PlayerPrefs.SetFloat(SfxVolumeKey, value); PlayerPrefs.Save(); }
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(HighScoreKey);
            PlayerPrefs.DeleteKey(MasterVolumeKey);
            PlayerPrefs.DeleteKey(BgmVolumeKey);
            PlayerPrefs.DeleteKey(SfxVolumeKey);
            PlayerPrefs.Save();
        }
    }
}
```

- [ ] **Step 5: EditMode 테스트 실행 → 전부 PASS 확인**
- [ ] **Step 6: Commit** — `feat: SaveData PlayerPrefs 래퍼 + 테스트`

---

### Task 3: 이벤트 채널 3종 + EditMode 테스트

**Files:**
- Create: `Assets/Core/Scripts/Events/VoidEventChannel.cs`, `IntEventChannel.cs`, `FloatEventChannel.cs`
- Test: `Assets/Core/Tests/EditMode/EventChannelTests.cs`

**Interfaces:**
- Produces: `Core.Events.VoidEventChannel`(`event Action OnRaised`, `Raise()`), `IntEventChannel`(`event Action<int> OnRaised`, `Raise(int)`), `FloatEventChannel`(`event Action<float> OnRaised`, `Raise(float)`). 모두 ScriptableObject, CreateAssetMenu 경로 `Core/Events/...`

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
using System;
using Core.Events;
using NUnit.Framework;
using UnityEngine;

namespace Core.Tests
{
    public class EventChannelTests
    {
        [Test]
        public void VoidChannel_Raise하면_구독자가_호출된다()
        {
            var channel = ScriptableObject.CreateInstance<VoidEventChannel>();
            int callCount = 0;
            channel.OnRaised += () => callCount++;
            channel.Raise();
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void IntChannel_값이_전달된다()
        {
            var channel = ScriptableObject.CreateInstance<IntEventChannel>();
            int received = -1;
            channel.OnRaised += v => received = v;
            channel.Raise(42);
            Assert.AreEqual(42, received);
        }

        [Test]
        public void FloatChannel_값이_전달된다()
        {
            var channel = ScriptableObject.CreateInstance<FloatEventChannel>();
            float received = -1f;
            channel.OnRaised += v => received = v;
            channel.Raise(0.5f);
            Assert.AreEqual(0.5f, received);
        }

        [Test]
        public void 구독자_없이_Raise해도_예외_없다()
        {
            var channel = ScriptableObject.CreateInstance<VoidEventChannel>();
            Assert.DoesNotThrow(() => channel.Raise());
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 → 컴파일 실패 확인**
- [ ] **Step 3: 채널 3종 구현**

```csharp
// VoidEventChannel.cs
using System;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Core/Events/Void Event Channel")]
    public class VoidEventChannel : ScriptableObject
    {
        public event Action OnRaised;
        public void Raise() => OnRaised?.Invoke();
    }
}

// IntEventChannel.cs
using System;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Core/Events/Int Event Channel")]
    public class IntEventChannel : ScriptableObject
    {
        public event Action<int> OnRaised;
        public void Raise(int value) => OnRaised?.Invoke(value);
    }
}

// FloatEventChannel.cs
using System;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Core/Events/Float Event Channel")]
    public class FloatEventChannel : ScriptableObject
    {
        public event Action<float> OnRaised;
        public void Raise(float value) => OnRaised?.Invoke(value);
    }
}
```

- [ ] **Step 4: EditMode 테스트 실행 → 전부 PASS**
- [ ] **Step 5: Commit** — `feat: ScriptableObject 이벤트 채널 3종 + 테스트`

---

### Task 4: GameManager + PlayMode 테스트

**Files:**
- Create: `Assets/Core/Scripts/Game/GameManager.cs`
- Create: `Assets/Core/Tests/PlayMode/Core.Tests.PlayMode.asmdef`
- Test: `Assets/Core/Tests/PlayMode/GameManagerTests.cs`

**Interfaces:**
- Consumes: `Singleton<T>`(Task 1), `SaveData.HighScore`(Task 2), `IntEventChannel`(Task 3)
- Produces: `Core.GameState { Ready, Playing, Paused, GameOver }` enum. `Core.GameManager : Singleton<GameManager>` — `GameState State`, `int Score`, `int HighScore`, `StartGame()`, `Pause()`, `Resume()`, `TogglePause()`, `GameOver()`, `BackToMenu()`, `RestartGame()`, `AddScore(int)`. 인스펙터 필드: `IntEventChannel onGameStateChanged`, `IntEventChannel onScoreChanged` (필드명 정확히 이대로 — 생성 스크립트가 SerializedProperty로 찾음)

- [ ] **Step 1: PlayMode 테스트 asmdef 작성**

```json
{
    "name": "Core.Tests.PlayMode",
    "rootNamespace": "Core.Tests",
    "references": ["Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "precompiledReferences": ["nunit.framework.dll"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "overrideReferences": true
}
```

- [ ] **Step 2: 실패하는 테스트 작성**

```csharp
using System.Collections;
using Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Core.Tests
{
    public class GameManagerTests
    {
        private GameManager _gm;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveData.ResetAll();
            _gm = new GameObject("GM").AddComponent<GameManager>();
            yield return null; // Awake 실행 대기
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(_gm.gameObject);
            SaveData.ResetAll();
            yield return null;
        }

        [Test]
        public void 초기_상태는_Ready()
        {
            Assert.AreEqual(GameState.Ready, _gm.State);
        }

        [Test]
        public void StartGame_하면_Playing이고_점수_0()
        {
            _gm.AddScore(10); // Ready 상태에선 무시되어야 함
            _gm.StartGame();
            Assert.AreEqual(GameState.Playing, _gm.State);
            Assert.AreEqual(0, _gm.Score);
        }

        [Test]
        public void AddScore는_Playing에서만_동작()
        {
            _gm.StartGame();
            _gm.AddScore(10);
            _gm.Pause();
            _gm.AddScore(5);
            Assert.AreEqual(10, _gm.Score);
        }

        [Test]
        public void Pause_Resume이_timeScale을_토글()
        {
            _gm.StartGame();
            _gm.Pause();
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(GameState.Paused, _gm.State);
            _gm.Resume();
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(GameState.Playing, _gm.State);
        }

        [Test]
        public void GameOver_시_하이스코어_갱신()
        {
            _gm.StartGame();
            _gm.AddScore(100);
            _gm.GameOver();
            Assert.AreEqual(GameState.GameOver, _gm.State);
            Assert.AreEqual(100, SaveData.HighScore);
        }

        [Test]
        public void 낮은_점수는_하이스코어를_덮지_않음()
        {
            SaveData.HighScore = 500;
            _gm.StartGame();
            _gm.AddScore(100);
            _gm.GameOver();
            Assert.AreEqual(500, SaveData.HighScore);
        }
    }
}
```

- [ ] **Step 3: 테스트 실행 → 컴파일 실패 확인**
- [ ] **Step 4: GameManager.cs 구현**

```csharp
using Core.Events;
using UnityEngine;

namespace Core
{
    public enum GameState { Ready, Playing, Paused, GameOver }

    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private IntEventChannel onGameStateChanged;
        [SerializeField] private IntEventChannel onScoreChanged;

        public GameState State { get; private set; } = GameState.Ready;
        public int Score { get; private set; }
        public int HighScore => SaveData.HighScore;

        public void StartGame()
        {
            Time.timeScale = 1f;
            Score = 0;
            onScoreChanged?.Raise(0);
            SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void GameOver()
        {
            if (State != GameState.Playing && State != GameState.Paused) return;
            Time.timeScale = 1f;
            if (Score > SaveData.HighScore) SaveData.HighScore = Score;
            SetState(GameState.GameOver);
        }

        public void BackToMenu()
        {
            Time.timeScale = 1f;
            Score = 0;
            SetState(GameState.Ready);
        }

        public void AddScore(int amount)
        {
            if (State != GameState.Playing) return;
            Score += amount;
            onScoreChanged?.Raise(Score);
        }

        private void SetState(GameState state)
        {
            State = state;
            onGameStateChanged?.Raise((int)state);
        }
    }
}
```

(`RestartGame()`은 SceneLoader 의존이라 Task 7에서 추가)

- [ ] **Step 5: PlayMode 테스트 실행 → 전부 PASS**
- [ ] **Step 6: Commit** — `feat: GameManager 상태머신 + 점수/하이스코어 + 테스트`

---

### Task 5: PoolManager + PooledObject + PlayMode 테스트

**Files:**
- Create: `Assets/Core/Scripts/Pool/PoolManager.cs`, `Assets/Core/Scripts/Pool/PooledObject.cs`
- Test: `Assets/Core/Tests/PlayMode/PoolManagerTests.cs`

**Interfaces:**
- Consumes: `Singleton<T>`(Task 1)
- Produces: `Core.PoolManager : Singleton<PoolManager>` — `GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)`, `void Despawn(GameObject instance)`. `Core.PooledObject : MonoBehaviour` — `void Despawn(float delay = 0f)`

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
using System.Collections;
using Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Core.Tests
{
    public class PoolManagerTests
    {
        private PoolManager _pool;
        private GameObject _prefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _pool = new GameObject("Pool").AddComponent<PoolManager>();
            _prefab = new GameObject("Prefab");
            _prefab.SetActive(false); // 씬 오브젝트를 프리팹 대용으로 사용
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_pool.gameObject);
            Object.Destroy(_prefab);
            yield return null;
        }

        [Test]
        public void Spawn하면_활성_인스턴스가_생성된다()
        {
            var instance = _pool.Spawn(_prefab, Vector3.one, Quaternion.identity);
            Assert.IsTrue(instance.activeSelf);
            Assert.AreEqual(Vector3.one, instance.transform.position);
        }

        [Test]
        public void Despawn_후_다시_Spawn하면_같은_인스턴스_재사용()
        {
            var first = _pool.Spawn(_prefab, Vector3.zero, Quaternion.identity);
            _pool.Despawn(first);
            Assert.IsFalse(first.activeSelf);
            var second = _pool.Spawn(_prefab, Vector3.zero, Quaternion.identity);
            Assert.AreSame(first, second);
        }

        [Test]
        public void 풀에_없는_오브젝트_Despawn해도_예외_없다()
        {
            var stray = new GameObject("Stray");
            Assert.DoesNotThrow(() => _pool.Despawn(stray));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 → 컴파일 실패 확인**
- [ ] **Step 3: 구현**

```csharp
// PoolManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Core
{
    public class PoolManager : Singleton<PoolManager>
    {
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _instanceToPool = new();

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = GetPool(prefab).Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;
            if (_instanceToPool.TryGetValue(instance, out var pool)) pool.Release(instance);
            else Destroy(instance);
        }

        private ObjectPool<GameObject> GetPool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var existing)) return existing;

            ObjectPool<GameObject> pool = null;
            pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var go = Instantiate(prefab, transform);
                    _instanceToPool[go] = pool;
                    return go;
                },
                actionOnGet: go => go.SetActive(true),
                actionOnRelease: go => go.SetActive(false),
                actionOnDestroy: go =>
                {
                    _instanceToPool.Remove(go);
                    Destroy(go);
                });
            _pools[prefab] = pool;
            return pool;
        }
    }
}

// PooledObject.cs
using UnityEngine;

namespace Core
{
    public class PooledObject : MonoBehaviour
    {
        public void Despawn(float delay = 0f)
        {
            if (delay <= 0f) DespawnNow();
            else Invoke(nameof(DespawnNow), delay);
        }

        private void DespawnNow() => PoolManager.Instance.Despawn(gameObject);

        private void OnDisable() => CancelInvoke();
    }
}
```

- [ ] **Step 4: PlayMode 테스트 실행 → 전부 PASS (EditMode 포함 전체 재실행)**
- [ ] **Step 5: Commit** — `feat: 프리팹별 자동 풀링 PoolManager + 테스트`

---

### Task 6: 체크포인트 — DOTween 임포트 + asmdef 참조 추가

**Files:**
- Modify: `Assets/Core/Scripts/Core.asmdef`

**Interfaces:**
- Produces: 이후 태스크에서 `DG.Tweening` 네임스페이스, `Unity.InputSystem`, `Unity.TextMeshPro` 사용 가능

- [ ] **Step 1: 사용자에게 DOTween 임포트 요청하고 대기** — Asset Store에서 DOTween(무료) 임포트 → `Tools > Demigiant > DOTween Utility Panel > Setup DOTween` 실행 → **"Create ASMDEF" 옵션 활성화**(DOTween.Modules asmdef 생성). 완료 응답 받을 때까지 다음 태스크 진행 금지.
- [ ] **Step 2: DOTween asmdef 존재 확인** — `Assets/Plugins/Demigiant/DOTween/Modules/DOTween.Modules.asmdef` 존재 확인
- [ ] **Step 3: Core.asmdef 참조 추가**

```json
{
    "name": "Core",
    "rootNamespace": "Core",
    "references": ["DOTween.Modules", "Unity.InputSystem", "Unity.TextMeshPro"],
    "autoReferenced": true
}
```

- [ ] **Step 4: 컴파일 확인 + 기존 테스트 전체 재실행 → PASS**
- [ ] **Step 5: Commit** — `feat: Core asmdef에 DOTween/InputSystem/TMP 참조 추가` (DOTween 에셋 자체는 .gitignore 대상인지 확인 후, 아니면 함께 커밋)

---

### Task 7: SceneLoader (페이드 전환)

**Files:**
- Create: `Assets/Core/Scripts/Scene/SceneLoader.cs`
- Modify: `Assets/Core/Scripts/Game/GameManager.cs` (`RestartGame()` 추가)

**Interfaces:**
- Consumes: `Singleton<T>`, DOTween(`DOFade`)
- Produces: `Core.SceneLoader : Singleton<SceneLoader>` — `void Load(string sceneName)`, `void Reload()`. 인스펙터 필드: `CanvasGroup fadeGroup`, `float fadeDuration = 0.3f` (필드명 고정 — 생성 스크립트가 참조)

- [ ] **Step 1: SceneLoader.cs 구현**

```csharp
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class SceneLoader : Singleton<SceneLoader>
    {
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeDuration = 0.3f;

        private bool _isLoading;

        public void Load(string sceneName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        public void Reload() => Load(SceneManager.GetActiveScene().name);

        private IEnumerator LoadRoutine(string sceneName)
        {
            _isLoading = true;
            fadeGroup.blocksRaycasts = true;
            yield return fadeGroup.DOFade(1f, fadeDuration).SetUpdate(true).WaitForCompletion();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return fadeGroup.DOFade(0f, fadeDuration).SetUpdate(true).WaitForCompletion();
            fadeGroup.blocksRaycasts = false;
            _isLoading = false;
        }
    }
}
```

- [ ] **Step 2: GameManager에 RestartGame 추가**

```csharp
public void RestartGame()
{
    SceneLoader.Instance.Reload();
    StartGame();
}
```

- [ ] **Step 3: 컴파일 확인 + 전체 테스트 재실행 → PASS**
- [ ] **Step 4: Commit** — `feat: DOTween 페이드 씬 로더 추가`

---

### Task 8: AudioManager

**Files:**
- Create: `Assets/Core/Scripts/Audio/AudioManager.cs`

**Interfaces:**
- Consumes: `Singleton<T>`, `SaveData`(볼륨), DOTween(`DOFade` on AudioSource)
- Produces: `Core.AudioManager : Singleton<AudioManager>` — `PlaySFX(AudioClip, float volumeScale = 1f)`, `PlayBGM(AudioClip)`, `StopBGM()`, `float MasterVolume/BgmVolume/SfxVolume` 프로퍼티(set 시 SaveData 저장). 인스펙터 필드: `AudioSource sfxSource`, `AudioSource bgmSourceA`, `AudioSource bgmSourceB`, `float bgmCrossfadeDuration = 1f`, `Vector2 sfxPitchRange = (0.95, 1.05)` (필드명 고정)

- [ ] **Step 1: AudioManager.cs 구현**

```csharp
using DG.Tweening;
using UnityEngine;

namespace Core
{
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSourceA;
        [SerializeField] private AudioSource bgmSourceB;
        [SerializeField] private float bgmCrossfadeDuration = 1f;
        [SerializeField] private Vector2 sfxPitchRange = new(0.95f, 1.05f);

        private AudioSource _activeBgm;

        public float MasterVolume
        {
            get => SaveData.MasterVolume;
            set { SaveData.MasterVolume = Mathf.Clamp01(value); AudioListener.volume = SaveData.MasterVolume; }
        }

        public float BgmVolume
        {
            get => SaveData.BgmVolume;
            set
            {
                SaveData.BgmVolume = Mathf.Clamp01(value);
                if (_activeBgm != null) _activeBgm.volume = SaveData.BgmVolume;
            }
        }

        public float SfxVolume
        {
            get => SaveData.SfxVolume;
            set => SaveData.SfxVolume = Mathf.Clamp01(value);
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            AudioListener.volume = SaveData.MasterVolume;
            _activeBgm = bgmSourceA;
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            sfxSource.pitch = Random.Range(sfxPitchRange.x, sfxPitchRange.y);
            sfxSource.PlayOneShot(clip, volumeScale * SfxVolume);
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || (_activeBgm.clip == clip && _activeBgm.isPlaying)) return;

            var next = _activeBgm == bgmSourceA ? bgmSourceB : bgmSourceA;
            next.clip = clip;
            next.volume = 0f;
            next.loop = true;
            next.Play();
            next.DOFade(BgmVolume, bgmCrossfadeDuration).SetUpdate(true);

            var previous = _activeBgm;
            previous.DOFade(0f, bgmCrossfadeDuration).SetUpdate(true)
                .OnComplete(previous.Stop);

            _activeBgm = next;
        }

        public void StopBGM()
        {
            var bgm = _activeBgm;
            bgm.DOFade(0f, bgmCrossfadeDuration).SetUpdate(true).OnComplete(bgm.Stop);
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인 + 전체 테스트 재실행 → PASS**
- [ ] **Step 3: Commit** — `feat: AudioManager (SFX 피치 랜덤, BGM 크로스페이드)`

---

### Task 9: UI 시스템 (UIScreen 베이스 + 화면 4종 + UIManager)

**Files:**
- Create: `Assets/Core/Scripts/UI/UIScreen.cs`, `MainMenuScreen.cs`, `PauseScreen.cs`, `ResultScreen.cs`, `ScoreHud.cs`, `UIManager.cs`

**Interfaces:**
- Consumes: `GameManager`(Task 4/7), `SceneLoader`(Task 7), `IntEventChannel`(Task 3), DOTween, `Keyboard.current`(Input System), `TMP_Text`
- Produces: `Core.UI.UIScreen`(abstract, `Show()`/`Hide()`), 화면 4종, `Core.UI.UIManager`. 인스펙터 필드명 고정(생성 스크립트가 참조): UIScreen — `RectTransform panel`, `float popDuration = 0.25f`; MainMenuScreen — `Button playButton`, `Button quitButton`; PauseScreen — `Button resumeButton`, `Button menuButton`; ResultScreen — `TMP_Text scoreText`, `TMP_Text highScoreText`, `Button restartButton`, `Button menuButton`; ScoreHud — `TMP_Text scoreText`, `IntEventChannel onScoreChanged`; UIManager — `IntEventChannel onGameStateChanged`, `MainMenuScreen mainMenu`, `PauseScreen pause`, `ResultScreen result`, `ScoreHud hud`

- [ ] **Step 1: UIScreen.cs 구현**

```csharp
using DG.Tweening;
using UnityEngine;

namespace Core.UI
{
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private float popDuration = 0.25f;

        public void Show()
        {
            gameObject.SetActive(true);
            OnShow();
            if (panel == null) return;
            panel.DOKill();
            panel.localScale = Vector3.one * 0.8f;
            panel.DOScale(1f, popDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void Hide()
        {
            if (panel == null || !gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                return;
            }
            panel.DOKill();
            panel.DOScale(0.8f, popDuration).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        protected virtual void OnShow() { }
    }
}
```

- [ ] **Step 2: 화면 4종 구현**

```csharp
// MainMenuScreen.cs
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            playButton.onClick.AddListener(() => GameManager.Instance.StartGame());
            quitButton.onClick.AddListener(Application.Quit);
        }
    }
}

// PauseScreen.cs
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    public class PauseScreen : UIScreen
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            resumeButton.onClick.AddListener(() => GameManager.Instance.Resume());
            menuButton.onClick.AddListener(() =>
            {
                GameManager.Instance.BackToMenu();
                SceneLoader.Instance.Reload();
            });
        }
    }
}

// ResultScreen.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    public class ResultScreen : UIScreen
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            menuButton.onClick.AddListener(() =>
            {
                GameManager.Instance.BackToMenu();
                SceneLoader.Instance.Reload();
            });
        }

        protected override void OnShow()
        {
            scoreText.text = $"Score  {GameManager.Instance.Score}";
            highScoreText.text = $"Best  {GameManager.Instance.HighScore}";
        }
    }
}

// ScoreHud.cs
using Core.Events;
using TMPro;
using UnityEngine;

namespace Core.UI
{
    public class ScoreHud : UIScreen
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private IntEventChannel onScoreChanged;

        private void OnEnable()
        {
            if (onScoreChanged != null) onScoreChanged.OnRaised += UpdateScore;
        }

        private void OnDisable()
        {
            if (onScoreChanged != null) onScoreChanged.OnRaised -= UpdateScore;
        }

        private void UpdateScore(int score) => scoreText.text = score.ToString();
    }
}
```

- [ ] **Step 3: UIManager.cs 구현**

```csharp
using Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.UI
{
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private IntEventChannel onGameStateChanged;
        [SerializeField] private MainMenuScreen mainMenu;
        [SerializeField] private PauseScreen pause;
        [SerializeField] private ResultScreen result;
        [SerializeField] private ScoreHud hud;

        private void OnEnable()
        {
            if (onGameStateChanged != null) onGameStateChanged.OnRaised += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (onGameStateChanged != null) onGameStateChanged.OnRaised -= HandleStateChanged;
        }

        private void Start() => HandleStateChanged((int)GameManager.Instance.State);

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                GameManager.Instance.TogglePause();
        }

        private void HandleStateChanged(int stateValue)
        {
            var state = (GameState)stateValue;
            switch (state)
            {
                case GameState.Ready:
                    mainMenu.Show(); pause.Hide(); result.Hide(); hud.Hide();
                    break;
                case GameState.Playing:
                    mainMenu.Hide(); pause.Hide(); result.Hide(); hud.Show();
                    break;
                case GameState.Paused:
                    pause.Show();
                    break;
                case GameState.GameOver:
                    pause.Hide(); hud.Hide(); result.Show();
                    break;
            }
        }
    }
}
```

- [ ] **Step 4: 컴파일 확인 + 전체 테스트 재실행 → PASS**
- [ ] **Step 5: Commit** — `feat: UIScreen 베이스 + 화면 4종 + UIManager`

---

### Task 10: 에디터 생성 스크립트 (이벤트 에셋 + Systems.prefab + UI)

**Files:**
- Create: `Assets/Core/Editor/Core.Editor.asmdef`
- Create: `Assets/Core/Editor/CoreTemplateGenerator.cs`

**Interfaces:**
- Consumes: Task 1~9의 모든 컴포넌트와 **고정된 인스펙터 필드명** (SerializedObject로 바인딩)
- Produces: 메뉴 `Core > Generate Template Assets` → `Assets/Core/Events/OnGameStateChanged.asset`(Int), `OnScoreChanged.asset`(Int), `OnPlayerDied.asset`(Void), `Assets/Core/Resources/Systems.prefab`(GameManager, PoolManager, AudioManager+소스 3개, SceneLoader+페이드 캔버스, UI 캔버스+화면 4종+UIManager, EventSystem 포함)

- [ ] **Step 1: Core.Editor.asmdef 작성**

```json
{
    "name": "Core.Editor",
    "rootNamespace": "Core.EditorTools",
    "references": ["Core", "Unity.InputSystem", "Unity.TextMeshPro"],
    "includePlatforms": ["Editor"]
}
```

- [ ] **Step 2: CoreTemplateGenerator.cs 작성** — 핵심 구조:

```csharp
using Core.Events;
using Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Core.EditorTools
{
    public static class CoreTemplateGenerator
    {
        private const string EventsDir = "Assets/Core/Events";
        private const string ResourcesDir = "Assets/Core/Resources";
        private const string PrefabPath = "Assets/Core/Resources/Systems.prefab";

        [MenuItem("Core/Generate Template Assets")]
        public static void GenerateAll()
        {
            EnsureFolder(EventsDir);
            EnsureFolder(ResourcesDir);

            var stateChannel = GetOrCreate<IntEventChannel>($"{EventsDir}/OnGameStateChanged.asset");
            var scoreChannel = GetOrCreate<IntEventChannel>($"{EventsDir}/OnScoreChanged.asset");
            GetOrCreate<VoidEventChannel>($"{EventsDir}/OnPlayerDied.asset");

            var root = BuildSystems(stateChannel, scoreChannel);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log("[Core] 템플릿 에셋 생성 완료");
        }

        private static GameObject BuildSystems(IntEventChannel stateChannel, IntEventChannel scoreChannel)
        {
            var root = new GameObject("Systems");

            // GameManager — SerializedObject로 채널 바인딩
            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.transform.SetParent(root.transform);
            Bind(gm, ("onGameStateChanged", stateChannel), ("onScoreChanged", scoreChannel));

            new GameObject("PoolManager").AddComponent<PoolManager>()
                .transform.SetParent(root.transform);

            BuildAudio(root.transform);
            var fadeGroup = BuildFadeCanvas(root.transform);   // SceneLoader + 페이드 캔버스
            BuildUI(root.transform, stateChannel, scoreChannel);
            BuildEventSystem(root.transform);

            return root;
        }

        // Bind: SerializedObject로 [SerializeField] private 필드에 에셋/참조 할당
        private static void Bind(Component target, params (string field, Object value)[] bindings)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in bindings)
            {
                var prop = so.FindProperty(field);
                if (prop == null) { Debug.LogError($"필드 없음: {target.GetType().Name}.{field}"); continue; }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        // ... BuildAudio / BuildFadeCanvas / BuildUI / BuildEventSystem / EnsureFolder / GetOrCreate
    }
}
```

나머지 빌더 메서드 요구사항 (구현 시 이 명세대로):
- `BuildAudio`: "AudioManager" GO + AudioManager, 자식 "SFX"/"BGM A"/"BGM B" 각각 AudioSource(`playOnAwake = false`), `sfxSource`/`bgmSourceA`/`bgmSourceB` 필드 바인딩
- `BuildFadeCanvas`: "SceneLoader" GO + SceneLoader, 자식 Canvas(`renderMode = ScreenSpaceOverlay`, `sortingOrder = 999`) + CanvasGroup(`alpha 0`, `blocksRaycasts false`) + 전체화면 검정 Image(anchor 0~1, offset 0), `fadeGroup` 바인딩
- `BuildUI`: "UI" GO에 Canvas(`ScreenSpaceOverlay`, `sortingOrder = 100`) + CanvasScaler(`ScaleWithScreenSize`, 1920x1080) + GraphicRaycaster + UIManager. 자식으로 화면 4종을 코드로 조립(각각 반투명 배경 Image + 중앙 "Panel" RectTransform + TMP 제목 + 버튼들). 버튼 헬퍼: `CreateButton(parent, name, label)` — Image + Button + 자식 TMP_Text, 크기 320x80. 각 화면 스크립트의 버튼/텍스트/panel 필드와 UIManager의 화면 4종 + `onGameStateChanged` 채널 바인딩. HUD의 `onScoreChanged` 바인딩. 초기 상태: 모든 화면 `SetActive(false)`
- `BuildEventSystem`: "EventSystem" GO + `EventSystem` + `InputSystemUIInputModule`
- `EnsureFolder`: `AssetDatabase.IsValidFolder` 확인 후 `CreateFolder`
- `GetOrCreate<T>`: `LoadAssetAtPath` 있으면 반환, 없으면 `CreateInstance` + `CreateAsset`

- [ ] **Step 3: 메뉴 실행** (unity-mcp `execute_menu_item` 또는 사용자에게 요청) → `Systems.prefab`과 이벤트 에셋 3종 생성 확인, 콘솔 에러 없음
- [ ] **Step 4: 전체 테스트 재실행 → PASS**
- [ ] **Step 5: Commit** — `feat: 템플릿 에셋 생성 에디터 스크립트 + Systems.prefab`

---

### Task 11: 데모 씬 + README + 최종 검증

**Files:**
- Create: `Assets/Core/Scripts/Demo/DemoController.cs`
- Create: `Assets/Core/Scenes/Demo.unity` (에디터 스크립트 또는 unity-mcp로 생성)
- Create: `Assets/Core/README.md`

**Interfaces:**
- Consumes: 전 시스템

- [ ] **Step 1: DemoController.cs 작성** — 씬 배선 없이 IMGUI 버튼만으로 전 시스템 검증

```csharp
using Core;
using UnityEngine;

namespace Core.Demo
{
    public class DemoController : MonoBehaviour
    {
        [SerializeField] private AudioClip demoSfx;
        [SerializeField] private AudioClip demoBgm;
        [SerializeField] private GameObject demoPrefab;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 220, 400));
            GUILayout.Label($"State: {GameManager.Instance.State}  Score: {GameManager.Instance.Score}");
            if (GUILayout.Button("SFX 재생")) AudioManager.Instance.PlaySFX(demoSfx);
            if (GUILayout.Button("BGM 재생")) AudioManager.Instance.PlayBGM(demoBgm);
            if (GUILayout.Button("BGM 정지")) AudioManager.Instance.StopBGM();
            if (GUILayout.Button("풀 스폰") && demoPrefab != null)
            {
                var go = PoolManager.Instance.Spawn(demoPrefab,
                    Random.insideUnitCircle * 3f, Quaternion.identity);
                if (go.TryGetComponent<PooledObject>(out var pooled)) pooled.Despawn(1.5f);
            }
            if (GUILayout.Button("점수 +10")) GameManager.Instance.AddScore(10);
            if (GUILayout.Button("게임 오버")) GameManager.Instance.GameOver();
            if (GUILayout.Button("씬 리로드")) SceneLoader.Instance.Reload();
            GUILayout.EndArea();
        }
    }
}
```

- [ ] **Step 2: Demo.unity 생성** — Main Camera(2D, orthographic) + "Demo" GO(DemoController) 만 있는 씬. Build Settings(Build Profiles)에 씬 등록. demoSfx/demoBgm은 임포트된 에셋 중 적당한 클립이 있으면 연결, 없으면 비워두고 README에 명시. demoPrefab은 SpriteRenderer + PooledObject 프리팹을 생성 스크립트에 추가하거나 씬에서 즉석 생성.
- [ ] **Step 3: README.md 작성** — 셋업 순서(DOTween 임포트 → Setup → Generate Template Assets 메뉴), 시스템별 한 줄 사용 예제(`SceneLoader.Instance.Load("Game")`, `AudioManager.Instance.PlaySFX(clip)`, `PoolManager.Instance.Spawn(...)`, `GameManager.Instance.AddScore(10)`, 채널 구독 패턴, `SaveData.HighScore`), 새 잼 프로젝트에 가져가는 방법(레포 클론 / .unitypackage 내보내기 시 `Assets/Core` + DOTween 선택)
- [ ] **Step 4: 최종 검증** — 전체 테스트(EditMode + PlayMode) PASS + Demo 씬 Play 수동 체크리스트: 메인 메뉴 표시 → Play → HUD 표시 → 점수 +10 반영 → ESC 일시정지/재개 → 게임 오버 → 결과창 점수/하이스코어 → 재시작(페이드 확인) → SFX/BGM/풀 스폰 동작. unity-mcp로 Play 모드 진입해 콘솔 에러 없음 확인, 불가하면 사용자에게 체크리스트 전달.
- [ ] **Step 5: Commit** — `feat: 데모 씬 + DemoController + README`

---

## Self-Review 결과

- **Spec coverage:** 폴더 구조/8개 시스템/DOTween 셋업/데모 씬/README 모두 태스크에 매핑됨. UI 프리팹은 스펙의 "Prefabs/UI 별도 프리팹" 대신 Systems.prefab 내부 조립으로 구현(생성 스크립트 단순화, 동작 동일) — 실행 중 필요 시 분리 가능.
- **Type consistency:** 인스펙터 필드명은 Task 4/7/8/9의 Produces 블록에 고정 명시, Task 10 생성 스크립트가 동일 이름으로 바인딩. `GameState`를 int로 캐스팅해 IntEventChannel로 전달하는 규약 일관됨.
- **Placeholder:** Task 10의 빌더 메서드는 코드 전문 대신 상세 명세로 기술(전문 포함 시 계획이 과도하게 길어짐) — 각 메서드의 컴포넌트 구성/프로퍼티 값/바인딩 대상을 빠짐없이 명시했으므로 구현 모호성 없음.
