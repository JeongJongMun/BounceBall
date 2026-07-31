# 메인 메뉴 씬 + 스테이지 선택 흐름 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 전용 MainMenu 씬에서 스테이지를 선택해 진입하는 씬 흐름을 만든다. Core의 오버레이 메인 메뉴는 제거하고, 스테이지 목록은 StageDatabase가 단일 출처로 관리한다.

**Architecture:** MainMenu 씬(StageDatabase 기반 버튼 런타임 생성) → SceneLoader.Load(스테이지) → StageController.Start()가 StartGame() 호출 → Playing. Pause/Result의 "Menu"는 GameManager.BackToMenu()가 menuSceneName 씬을 로드(미설정 시 기존처럼 Reload — Core 템플릿 하위호환). Systems.prefab에서 MainMenu 화면 제거는 재실행 가능한 에디터 셋업 메서드로 처리.

## Global Constraints

- main 브랜치 직접 커밋, 커밋 한국어, Co-Authored-By 금지
- Core 수정은 템플릿 범용성 유지 (menuSceneName 미설정 시 기존 동작과 동일)
- 스테이지 목록·Build Settings는 StageDatabase 동기화가 단일 관리 지점

## Tasks

### Task 1: Core를 씬 기반 메뉴에 대응시키기
- `UIManager`: 모든 화면 참조 null-safe (mainMenu 없는 Systems.prefab 허용)
- `GameManager`: `[SerializeField] string menuSceneName` 추가. `BackToMenu()` — 상태 Ready 전환 후 menuSceneName 설정 시 해당 씬 Load, 미설정 시 Reload, SceneLoader 없으면(테스트) 상태만 변경
- `PauseScreen`/`ResultScreen`: Menu 버튼 → `GameManager.Instance.BackToMenu()` 한 줄로 통일

### Task 2: StageDatabase + 동기화 에디터
- `Game/Scripts/Stage/StageDatabase.cs` — StageEntry(sceneName, displayName) 목록, `GetNextStageScene(current)`, `SetStages()` 
- 에셋 위치: `Assets/Game/Resources/StageDatabase.asset` (UI가 Resources.Load)
- `Game/Editor/StageDatabaseTools.cs` — 메뉴 `Game > Sync Stage Database`: Stages 폴더 씬 스캔(기존 displayName 보존) + Build Settings를 [MainMenu, ...스테이지들]로 재구성 (SampleScene 등 잔재 자동 제거)
- EditMode 테스트: GetNextStageScene(다음/마지막→null/미등록→null)

### Task 3: MainMenu 씬 + UI
- `Game/Scripts/UI/MainMenuUI.cs` — StageDatabase 로드 → 스테이지 버튼 생성(템플릿 복제), 클릭 시 SceneLoader.Load, Quit 버튼
- `Game/Editor/MenuSceneGenerator.cs` — 메뉴 `Game > Create Main Menu Scene`: 카메라+캔버스+타이틀+버튼 컨테이너+템플릿+Quit 구성, `Assets/Game/Scenes/MainMenu.unity` 저장

### Task 4: 게임용 Systems 셋업 + 스테이지 자동 시작
- `Game/Editor/GameFlowSetup.cs` — 메뉴 `Game > Apply Game Flow Setup` (CLI 진입점 `ApplyAll`):
  1. Systems.prefab에서 MainMenu 화면 제거 + UIManager.mainMenu 해제
  2. GameManager.menuSceneName = "MainMenu" 바인딩
  3. MainMenu 씬 생성 + StageDatabase 동기화 호출
- `StageController.Start()`: 플레이어 스폰 후 `GameManager.Instance.StartGame()` (스테이지 씬 단독 Play 지원 유지)

### 검증
1. 컴파일 0, 전체 테스트(기존 28 + 신규) 통과
2. CLI로 ApplyAll 실행 → Systems.prefab에 MainMenu 없음, MainMenu.unity 존재, Build Settings = [MainMenu, StageTest]
3. 수동: MainMenu 씬 Play → 스테이지 버튼 클릭 → 페이드 전환 → 자동 바운스 시작 → ESC 일시정지 → Menu → 메뉴 씬 복귀
