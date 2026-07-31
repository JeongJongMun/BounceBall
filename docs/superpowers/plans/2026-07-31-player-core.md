# Phase B: 플레이어 코어 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 기획 문서 7~9장(자동 바운스, A/D 이동, 물리값 계산)과 16장(데드존 카메라)을 구현한다. 어느 스테이지 씬이든 Play만 누르면 플레이어가 스폰되어 플레이 가능해야 한다.

**Architecture:** 기획의 "이동/성질/상호작용 분리" 요구에 따라 컴포넌트 분리: `PlayerStats`(기본값×배율, Phase C 성질 훅), `PlayerMovement`(A/D 가감속·공중조작), `PlayerBounce`(자동 바운스·중복 방지), `Player`(상태 관리 파사드). `StageController`가 런타임에 `Resources/Player.prefab`을 StartPosition에 스폰하고 CameraFollow를 자동 연결 → 씬에 수동 배치 불필요. 카메라는 축 독립 데드존(1/3~2/3) + 스테이지 경계 클램프, 클램프 계산은 순수 함수로 분리해 EditMode 테스트.

**Tech Stack:** Rigidbody2D 기반 (physics material 미사용, 속도 직접 제어), Input System 직접 읽기(Keyboard.current), Core 이벤트 채널(OnPlayerBounced/OnPlayerLanded 선택 연결)

## Global Constraints

- 기획 문서의 데이터 명칭 준수: BaseJumpForce, GravityScale, MaxFallSpeed, LandingVelocityThreshold, BounceCooldown / MoveSpeed, Acceleration, Deceleration, AirControl, MaxHorizontalSpeed, DirectionChangePower
- 최종 물리값 = 기본값 × 배율 (배율 기본 1, Phase C에서 성질이 설정) — 기본값 직접 덮어쓰기 금지
- A+D 동시 입력 = 0
- 상태는 Airborne / GroundContact / Disabled 3종만 (기획 §6 초기 범위)
- 커밋 한국어, Co-Authored-By 금지

## Tasks

### Task 1: PlayerStats — 기본값 × 배율 분리
- `Assets/Game/Scripts/Player/PlayerStats.cs`
- 인스펙터 기본값 + 코드 설정 배율(기본 1) + 최종값 프로퍼티(JumpForce, MoveSpeed, GravityScale, AirControl, MaxFallSpeed...)
- `SetMultipliers(jump, move, gravity, airControl)` — Phase C 성질 적용 지점
- EditMode 테스트: 배율 1일 때 기본값 그대로, 배율 적용 시 곱셈 결과, SetMultipliers 후 갱신

### Task 2: Player + PlayerMovement + PlayerBounce
- `Player.cs`: PlayerState(Airborne/GroundContact/Disabled) 관리, SetDisabled API, 컴포넌트 참조 허브
- `PlayerMovement.cs`: FixedUpdate에서 A/D 입력(Keyboard.current, 동시 입력=0) → 가속/감속/공중조작/방향전환 보정. 테스트용 `SetInput(float)` 공개 (키보드 입력은 자동 모드에서만)
- `PlayerBounce.cs`: OnCollisionEnter2D/Stay2D에서 하단 접촉(normal.y>0.5) + 쿨타임 + Disabled 아님 → 수직 속도를 JumpForce로 설정. MaxFallSpeed 클램프. 선택 연결 이벤트 채널(onPlayerBounced, onPlayerLanded)
- PlayMode 테스트: 낙하 후 자동 바운스로 상승 속도 발생, SetInput(1)로 수평 가속, Disabled 시 바운스 정지

### Task 3: CameraFollow — 데드존 + 경계 클램프
- `Assets/Game/Scripts/Stage/CameraFollow.cs`
- 순수 함수 분리: `ComputeAxisTarget(camPos, playerPos, deadzoneHalf)` (데드존 밖일 때만 이동), `ClampAxis(pos, min, max, halfExtent)` (스테이지가 화면보다 작으면 중앙)
- LateUpdate에서 축 독립 SmoothDamp, `Init(target, stage)` API
- EditMode 테스트: 데드존 안 정지/밖 추적, 경계 클램프, 좁은 스테이지 중앙 고정

### Task 4: 런타임 스폰 + Player 프리팹 생성
- `StageController.Start()`: `Resources.Load("Player")` → StartPosition에 스폰, Camera.main에 CameraFollow 보장 + Init. 플레이어 프리팹 없으면 경고
- `StageToolingGenerator`에 Player.prefab 생성 추가 (`Assets/Game/Resources/Player.prefab`): 초록 원 스프라이트, Rigidbody2D(회전 고정, Continuous), CircleCollider2D, 마찰 0 PhysicsMaterial2D, Player+PlayerMovement+PlayerBounce+PlayerStats

### 검증
1. 컴파일 0 에러, 신규 EditMode/PlayMode 테스트 + 기존 17개 전부 통과
2. CLI로 Setup Stage Tooling 재실행 → Player.prefab 생성 확인
3. 수동: StageTest 씬 Play → 자동 바운스 + A/D 이동 + 카메라 데드존/경계 동작 확인 (기획 §31 검증 항목 중 이동/카메라 섹션)
