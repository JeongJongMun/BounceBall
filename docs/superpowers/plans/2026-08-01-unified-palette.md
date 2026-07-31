# 통합 타일 팔레트 (특수 타일 + 마커 브러시) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 일반 타일, 특수 타일, 기믹/체크포인트/아이템 프리팹을 전부 하나의 타일 팔레트에서 브러시 전환 없이 배치한다. 특수 타일은 플레이어의 현재 성질에 따라 반응하는 데이터 구조를 갖춘다 (구체 효과는 기믹 구현서 이후, 파이프라인 검증용 JumpMultiplier 1종만 구현).

**Architecture:**
- `SpecialTile : Tile` — tileId + 반응 테이블(propertyTag → effectId, value). propertyTag 빈 문자열 = 기본 반응
- `PrefabMarkerTile : TileBase` — 프리팹 참조 + 팔레트 표시용 스프라이트/색. 실제 스테이지 타일맵에는 남지 않음
- `StageBrush : GridBrush` (통합 브러시) — 페인트 시 셀 타일이 마커면 프리팹 스폰(셀당 1개, Gimmicks 컨테이너), 아니면 기본 타일 페인트. 지우개는 타일+프리팹 모두 제거. 기존 PrefabPaletteBrush와 브러시 에셋 3종은 삭제·대체
- `StageTiles` — 착지 지점 → 타일맵 셀 → SpecialTile 조회 (타일맵 캐시 + 무효화 API)
- `PlayerBounce` — 착지 시 SpecialTile 조회 → 현재 성질 태그(`Player.PropertyTag`, Phase C 전까지 "")로 반응 조회 → JumpMultiplier 적용
- 생성기 — 마커 타일 3종, 샘플 특수 타일(BouncyTile, 점프 1.6배), StageBrush 에셋 생성 + 팔레트에 전 항목 등록(항상 갱신)

## Global Constraints

- main 직접 커밋, 한국어 커밋, Co-Authored-By 금지
- 특수 타일 구체 효과는 범위 외 — 데이터 구조 + 조회 파이프라인 + 시연용 JumpMultiplier만
- 클리어는 목표 아이템 수집 방식 유지 (골인 지점 없음)

## Tasks

1. **SpecialTile + StageTiles + 테스트**: 반응 조회(태그 일치 → 기본 반응 폴백 → null), 월드 좌표 → SpecialTile 조회 (EditMode에서 Grid+Tilemap 구성해 검증)
2. **PrefabMarkerTile + StageBrush**: 마커 판별 페인트/삭제/박스 처리, Undo 지원. PrefabPaletteBrush 및 브러시 에셋 3종 삭제
3. **PlayerBounce 연동**: `Player.PropertyTag` 추가, 착지 셀 특수 타일 반응 적용 (JumpMultiplier)
4. **생성기 갱신 + README**: 마커/샘플 특수 타일/StageBrush 생성, 팔레트 전 항목 등록(재실행 시 항상 갱신), README를 통합 브러시 워크플로로 수정

## 검증

1. 컴파일 0, 전체 테스트 + 신규 테스트 통과
2. CLI Setup Stage Tooling → 마커 3종·BouncyTile·StageBrush 존재, 구 브러시 에셋 없음, 팔레트에 6항목
3. 수동: 팔레트에서 브러시 전환 없이 타일/특수타일/아이템 배치, BouncyTile 밟으면 더 높이 튀는지 확인
