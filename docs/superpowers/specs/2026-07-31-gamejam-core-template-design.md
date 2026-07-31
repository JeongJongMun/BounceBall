# 게임잼 코어 템플릿 설계

**날짜:** 2026-07-31
**프로젝트:** BounceBall (Unity 6000.5.6f1, 2D + URP + Cinemachine 3.1 + Input System 1.20)
**목표:** 게임잼에서 바로 재사용 가능한 코어 시스템 템플릿을 `Assets/Core`에 구축

## 결정 사항

- **형태:** 이 프로젝트 안 `Assets/Core` 폴더. 잼에서는 레포 클론 또는 .unitypackage 내보내기로 재사용.
- **포함 범위:** 기본 4종(Singleton, SceneLoader, AudioManager, GameManager) + 오브젝트 풀 + UI 템플릿 + 이벤트 채널 + 저장 래퍼. 전부 포함.
- **DOTween:** 필수 의존성. 사용자가 Asset Store에서 직접 임포트. 코드는 DOTween API 직접 사용, 임포트 전 컴파일 에러는 정상.
- **매니저 생명주기:** `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`로 `Resources/Systems.prefab` 자동 생성 + DontDestroyOnLoad. 어느 씬에서든 바로 Play 가능하면서 인스펙터 설정도 가능한 구조.

## 폴더 구조

```
Assets/Core/
├── Scripts/
│   ├── Core.asmdef              # DOTween asmdef 참조 포함
│   ├── Systems/                 # Systems(자동 로드 진입점), Singleton<T>
│   ├── Scene/                   # SceneLoader (페이드 전환)
│   ├── Audio/                   # AudioManager
│   ├── Game/                    # GameManager + GameState
│   ├── Pool/                    # PoolManager, PooledObject
│   ├── UI/                      # UIManager, 각 화면 스크립트
│   ├── Events/                  # ScriptableObject 이벤트 채널
│   └── Save/                    # SaveData 래퍼
├── Prefabs/
│   └── UI/                      # 메뉴/일시정지/결과/HUD 프리팹
├── Resources/
│   └── Systems.prefab           # 자동 로드되는 매니저 루트
├── Events/                      # 이벤트 채널 .asset 파일
└── Scenes/
    └── Demo.unity               # 전체 시스템 검증용 데모 씬
```

네임스페이스: `Core` (하위: `Core.Audio`, `Core.UI` 등).

## 시스템별 설계

### Systems + Singleton\<T\>
- `Systems`: `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`에서 `Resources.Load("Systems")` → Instantiate → DontDestroyOnLoad. 이미 존재하면 스킵.
- `Singleton<T> : MonoBehaviour`: `Instance` 정적 접근. Awake에서 중복 인스턴스 자기 파괴.
- 매니저 초기화 순서는 Systems 프리팹의 자식 계층 순서로 제어.

### SceneLoader
- API: `SceneLoader.Instance.Load(string sceneName)`.
- 흐름: CanvasGroup 페이드아웃(DOTween `DOFade`) → `LoadSceneAsync` → 페이드인.
- 로딩 중 재호출 무시(중복 방지 플래그).
- 페이드용 전체 화면 캔버스는 SceneLoader 자신이 보유(Systems 프리팹 하위).

### AudioManager
- API: `PlaySFX(AudioClip)`, `PlayBGM(AudioClip)`, `StopBGM()`.
- SFX: 피치 랜덤 0.95~1.05 기본 적용. AudioSource 원샷 재생.
- BGM: 두 AudioSource 교대로 DOTween 크로스페이드.
- 볼륨: Master/BGM/SFX 3종. 변경 시 `SaveData`에 저장, 시작 시 로드.

### GameManager
- 상태: `enum GameState { Ready, Playing, Paused, GameOver }`.
- 상태 변경 시 `OnGameStateChanged` 이벤트 채널로 브로드캐스트.
- 점수: `AddScore(int)`, `Score`, `HighScore`(갱신 시 `SaveData` 자동 저장), `OnScoreChanged` 채널 브로드캐스트.
- 일시정지 시 `Time.timeScale = 0`, 해제 시 1. DOTween UI 연출은 unscaled 업데이트 사용.

### PoolManager
- Unity 내장 `UnityEngine.Pool.ObjectPool<GameObject>` 래핑.
- API: `PoolManager.Instance.Spawn(prefab, pos, rot)` / `Despawn(instance)`.
- 프리팹별 풀 자동 생성(사전 등록 불필요). 인스턴스→풀 역매핑 딕셔너리 유지.
- `PooledObject` 컴포넌트: `Despawn(delay)` 헬퍼(파티클·이펙트용).

### 이벤트 채널
- `VoidEventChannel`, `IntEventChannel`, `FloatEventChannel` 3종 ScriptableObject.
- API: `Raise(...)` / C# `event` 구독.
- 기본 제공 에셋: `OnGameStateChanged`(Int로 상태 전달), `OnScoreChanged`(Int), `OnPlayerDied`(Void).

### SaveData
- PlayerPrefs 래핑 static 클래스.
- 타입드 프로퍼티: `HighScore`, `MasterVolume`, `BgmVolume`, `SfxVolume`.
- JSON 직렬화 없음(잼 규모에 불필요).

### UI 템플릿
- `UIManager`(Systems 하위): 화면 프리팹 참조 보유, `Show/Hide` 관리.
- 화면 4종 프리팹: 메인 메뉴(시작/종료), 일시정지(재개/메뉴로, ESC 토글), 결과창(점수/하이스코어/재시작/메뉴로), 점수 HUD.
- 열림/닫힘: DOTween 스케일 팝업(`SetUpdate(true)`로 timeScale 무시).
- 입력: Input System UI 액션 + ESC 키 일시정지 토글.

## DOTween 셋업 순서 (README에 명시)

1. Asset Store에서 DOTween(무료) 임포트
2. Tools > Demigiant > DOTween Utility Panel > Setup DOTween 실행 (asmdef 생성 옵션 포함)
3. `Core.asmdef`가 `DOTween.Modules`를 참조하므로 이후 컴파일 정상화

## 검증

- `Demo.unity` 씬 하나에서 전 시스템 검증: SFX/BGM 재생 버튼, 풀 스폰 버튼, 점수 추가, ESC 일시정지, 게임오버 → 결과창 → 씬 리로드.
- `Assets/Core/README.md`에 시스템별 한 줄 사용 예제.

## 범위 제외 (YAGNI)

- Addressables, 로컬라이제이션, 오디오 믹서 그래프, JSON 세이브, 네트워킹, 씬 관리 스택 구조 등은 잼 템플릿 범위 밖.
