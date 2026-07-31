# Core — 게임잼 코어 템플릿

게임잼에서 바로 재사용하는 코어 시스템 모음. 어느 씬에서든 Play를 누르면 `Resources/Systems.prefab`이 자동 로드되어 모든 매니저가 존재합니다.

## 셋업 (새 프로젝트에 가져갈 때)

1. **DOTween 설치**: Asset Store에서 DOTween(무료) 임포트 → `Tools > Demigiant > DOTween Utility Panel > Setup DOTween` 실행 → "Create ASMDEF" 활성화 (`DOTween.Modules.asmdef` 필요)
2. `Assets/Core` 폴더 복사 (또는 이 레포 클론)
3. 메뉴 **`Core > Generate Template Assets`** 실행 → `Systems.prefab` + 이벤트 채널 에셋 생성
4. (선택) **`Core > Generate Demo Scene`** 실행 → 전 시스템 검증용 데모 씬 생성

필요 패키지: Input System, uGUI(TMP 내장), DOTween

## 시스템별 사용법

| 시스템 | 한 줄 예제 |
|---|---|
| 씬 전환 (페이드) | `SceneLoader.Instance.Load("Game");` / `SceneLoader.Instance.Reload();` |
| SFX (피치 랜덤) | `AudioManager.Instance.PlaySFX(clip);` |
| BGM (크로스페이드) | `AudioManager.Instance.PlayBGM(clip);` / `StopBGM();` |
| 볼륨 (자동 저장) | `AudioManager.Instance.MasterVolume = 0.5f;` |
| 게임 시작/상태 | `GameManager.Instance.StartGame();` → `Playing`, ESC로 일시정지 토글 |
| 점수 | `GameManager.Instance.AddScore(10);` (Playing 상태에서만 반영) |
| 게임 오버 | `GameManager.Instance.GameOver();` → 하이스코어 자동 갱신 + 결과창 |
| 오브젝트 풀 | `PoolManager.Instance.Spawn(prefab, pos, rot);` / `Despawn(go);` |
| 풀 자동 반환 | 프리팹에 `PooledObject` 붙이고 `pooled.Despawn(1.5f);` |
| 저장 | `SaveData.HighScore`, `SaveData.MasterVolume` (PlayerPrefs 래퍼) |

## 이벤트 채널 (시스템 간 통신)

`Assets/Core/Events/`의 ScriptableObject 채널로 직접 참조 없이 통신합니다.

```csharp
[SerializeField] private IntEventChannel onScoreChanged;

private void OnEnable()  => onScoreChanged.OnRaised += HandleScore;
private void OnDisable() => onScoreChanged.OnRaised -= HandleScore;
```

- `OnGameStateChanged` (Int): `GameState` enum 값이 int로 전달됨
- `OnScoreChanged` (Int): 현재 점수
- `OnPlayerDied` (Void): 게임에서 직접 Raise해서 사용

새 채널: `Create > Core > Events > ...` 메뉴로 생성.

## UI

`Systems.prefab` 안에 메인 메뉴 / 일시정지(ESC) / 결과창 / 점수 HUD가 포함되어 있고 `GameState`에 따라 자동 전환됩니다. 디자인 수정은 `Resources/Systems.prefab`의 `UI` 하위를 직접 편집하세요.

## 게임에 붙이는 최소 코드

```csharp
// 플레이어 사망 시
GameManager.Instance.GameOver();

// 코인 획득 시
GameManager.Instance.AddScore(1);
AudioManager.Instance.PlaySFX(coinClip);
```

이게 전부입니다. 메뉴 → 플레이 → 게임오버 → 재시작 루프는 템플릿이 처리합니다.

## 테스트

`Assets/Core/Tests/`에 EditMode(SaveData, 이벤트 채널) + PlayMode(GameManager, PoolManager) 테스트 포함. Test Runner 또는 CLI로 실행.
