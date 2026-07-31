# Phase A: 스테이지 에디터 툴 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 아트/기획자가 그리드 기반으로 기본 타일과 프리팹(성질 아이템, 목표 아이템, 체크포인트, 추후 기믹)을 배치할 수 있는 에디터 툴 + 스테이지 뼈대를 만든다.

**Architecture:** Unity Tilemap/Tile Palette 워크플로를 기반으로 확장. 기본 타일은 표준 타일 팔레트 페인팅, 프리팹은 커스텀 `PrefabPaletteBrush`(GridBrushBase 상속)로 같은 팔레트 UI에서 셀 단위 배치. `Game > New Stage` 메뉴로 씬 뼈대 자동 생성, StageController 인스펙터에서 경계 자동 계산 + 검증. 에셋(타일, 팔레트, 플레이스홀더 프리팹, 브러시)은 Core 템플릿과 같은 패턴으로 에디터 생성 스크립트가 만든다.

**Tech Stack:** com.unity.2d.tilemap(.extras) 설치 확인됨. Game.asmdef(runtime, Core 참조) + Game.Editor.asmdef(Unity.2D.Tilemap.Editor 참조).

## Global Constraints

- 게임 레이어 코드는 `Assets/Game/` 하위, 네임스페이스 `Game` / `Game.EditorTools`
- 기믹 세부 구현 제외 — 배치 인프라와 셸(빈 컴포넌트)만
- 그리드 셀 = 1 유닛, 셀당 프리팹 1개
- 검증: 컴파일 + 생성 메뉴 CLI 실행 + 씬 생성 확인 (에디터 툴 특성상 유닛 테스트 대신 생성 결과 검증)
- 커밋 메시지 한국어, Co-Authored-By 금지

## Tasks

### Task 1: Game asmdef + 런타임 셸 컴포넌트
- `Assets/Game/Scripts/Game.asmdef` (refs: Core)
- `Items/PropertyItem.cs`, `Items/GoalItem.cs`, `Items/Checkpoint.cs` — Phase C에서 구현할 빈 셸 (프리팹 컴포넌트 교체를 피하기 위해 지금 부착)
- `Stage/GimmickContainer.cs` — 브러시 배치 대상 컨테이너 마커
- `Stage/StageController.cs` — StageID, startPosition, stageMinX/MaxX/MinY/MaxY, stageFallLimitY, totalGoalItemCount, requiredGoalItemCount + OnDrawGizmos(경계 사각형, 낙사선, 시작 위치)

### Task 2: PrefabPaletteBrush (에디터)
- `Assets/Game/Editor/Game.Editor.asmdef` (refs: Game, Core, Unity.2D.Tilemap.Editor)
- `PrefabPaletteBrush.cs` — GridBrushBase 상속, CreateAssetMenu로 프리팹별 브러시 에셋 생성
  - Paint: 셀 중앙 월드 좌표에 PrefabUtility.InstantiatePrefab, GimmickContainer 하위 배치, 셀 중복 방지, Undo 지원
  - Erase: 해당 셀의 기존 인스턴스 삭제 (Undo 지원)
  - BoxFill/BoxErase: 영역 반복 처리
- 브러시는 Tile Palette 창의 브러시 드롭다운에서 선택

### Task 3: 스테이지 스캐폴딩 + 에셋 생성기 (에디터)
- `StageToolingGenerator.cs` — 메뉴 `Game > Setup Stage Tooling`:
  - 흰색 16x16 스프라이트 + 기본 Tile 에셋 + StagePalette(팔레트 프리팹+GridPalette 서브에셋) 생성
  - 플레이스홀더 프리팹 3종 생성: PropertyItem(보라), GoalItem(노랑), Checkpoint(초록) — 스프라이트 + 트리거 콜라이더 + 셸 컴포넌트
  - 프리팹별 PrefabPaletteBrush 에셋 생성
- `StageScaffolder.cs` — 메뉴 `Game > New Stage...`:
  - 저장 경로 입력 → 씬 생성: Grid(Ground 타일맵+콜라이더+Composite, Deco 타일맵), Gimmicks 컨테이너, StageController+StartPosition, Main Camera
  - CLI 검증용 `CreateStageForTest()` 진입점
- `StageControllerEditor.cs` — 인스펙터 버튼:
  - [경계 자동 계산]: Ground 타일맵 bounds → 카메라 경계 + 낙사선(minY-2)
  - [스테이지 검증]: StartPosition 존재/낙사선 위 여부, GoalItem 수량 ≥ requiredGoalItemCount, 낙사선 아래 배치물 경고 → 콘솔 출력

### Task 4: 팀용 가이드 문서
- `Assets/Game/README.md` — 스테이지 만들기, 타일 페인팅, 프리팹 브러시 사용법, 경계/검증 버튼 설명 (아트/기획자 대상, 스크린샷 없이 단계별 텍스트)

### 검증 (전체)
1. 컴파일 에러 0
2. CLI로 `Setup Stage Tooling` 실행 → 타일/팔레트/프리팹/브러시 에셋 존재 확인
3. CLI로 테스트 스테이지 씬 생성 → 씬 파일 존재 + 콘솔 에러 0
4. 기존 테스트(EditMode+PlayMode) 회귀 없음
5. 수동: 팀원이 Tile Palette에서 타일 페인팅 + 브러시로 프리팹 배치 확인
