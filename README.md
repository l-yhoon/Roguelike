# Roguelike

**이영훈 readme 생성** <br>
**김종건 이름추가** <br>
**김다솔 이름추가** <br>
**변지호 이름추가** <br>

## 프로젝트

Unity로 만드는 2D 덱빌딩 로그라이크 학습 프로젝트입니다. Slay the Spire의 1막 기본 흐름을 참고해 시작 화면, 맵, 카드 전투와 보상, 일부 카드·유물을 구현합니다. 학기 중 목표는 11월 내 기본 플레이 흐름 완성입니다.

## 처음 실행하기

1. Unity Hub에서 **Unity 6000.3.15f1**을 설치합니다. 팀원 모두 같은 버전을 사용합니다.
2. 이 GitHub 저장소를 clone합니다.
3. Unity Hub의 프로젝트 추가에서 저장소 안의 **`SlaytheSpire` 폴더**를 선택합니다. 저장소 루트 `Roguelike`가 아닙니다.
4. 패키지와 에셋 가져오기가 끝나면 `Assets/_Game/Scenes/Battle.unity`를 열고 Play를 누릅니다.

현재 Battle 씬에는 아이언클래드와 후보 목록에서 선택한 적, 체력·방어도·에너지·첫 행동 의도 표시가 구현돼 있습니다. `NEW ENCOUNTER`로 적을 다시 선택하고 전투 상태를 초기화할 수 있습니다. 카드와 턴 진행은 다음 단계이며, 화면의 `END TURN`은 안내 표시입니다. UI와 이미지는 임시 리소스입니다. 기존 SampleScene도 유지합니다.

## 전투 데이터 수정과 적 추가

- 플레이어: `Assets/_Game/Data/Characters/Ironclad.asset`에서 이름·이미지·최대 체력·기본 에너지를 수정합니다.
- 초기 적: `Assets/_Game/Data/Enemies/TrainingSlime.asset`은 테스트용 일반 적입니다. 원작 적의 수치·행동을 재현한 에셋은 아닙니다.
- 새 적: Project 창에서 Create → Roguelike → Enemy를 선택하고 ID, 이름, 이미지, 최대 체력, Actions를 설정합니다. 첫 행동이 화면에 의도로 표시됩니다. 행동은 아직 실행하지 않습니다.
- 후보 등록: `Act1NormalEnemyPool.asset`의 Candidates에 새 적을 추가합니다. 유효한 서로 다른 에셋을 동일 확률로 선택하며, 누락·잘못된 후보는 제외하고 중복 참조는 한 번만 셉니다. 유효한 후보가 없으면 오류를 표시하고 전투 시작을 중단합니다.
- 원본 설정과 현재 체력·방어도·에너지는 분리돼 있습니다. `NEW ENCOUNTER`는 현재 체력까지 초기화하는 개발 확인용 버튼입니다. 추후 연속 전투에서는 진행 상태의 체력을 별도로 전달해야 합니다.
- 플레이어·적 표시: `Prefabs/Combat/Player.prefab`, `Enemy.prefab`에서 수정합니다. `Battle Setup` 오브젝트의 Inspector에 설정·UI 참조가 연결돼 있습니다.
- 씬은 에디터에서 직접 열어 실행합니다. 기존 빌드 씬 목록은 변경하지 않았으므로 실행 파일을 만들 때 Battle 씬을 빌드 목록에 추가해야 합니다.

에셋 생성 도구는 `Roguelike → Create Initial Battle Assets`에 있습니다. 기존 Battle 씬이 있으면 덮어쓰지 않고 중단합니다. 초기 생성용이며 평소에는 씬과 에셋을 직접 수정합니다.

## 첫 구현 목표

맵에서 전투 노드 선택 → 카드로 전투 → 승리 보상으로 카드 선택 → 맵 복귀.

우선 임시 이미지와 소수의 카드·적으로 이 흐름을 연결하고, 이후 1막 맵과 콘텐츠를 늘립니다.

## 파일 위치

| 위치 | 용도 |
| --- | --- |
| `SlaytheSpire/Assets/_Game/Scenes` | 새 게임 씬 |
| `SlaytheSpire/Assets/_Game/Scripts` | Core, Combat, Cards, UI, Progression 코드 |
| `SlaytheSpire/Assets/_Game/Prefabs` | 카드, 전투, UI, 맵의 재사용 오브젝트 |
| `SlaytheSpire/Assets/_Game/Data` | 카드·적·유물의 ScriptableObject 설정 에셋 |
| `SlaytheSpire/Assets/_Game/Art` | 카드·캐릭터·배경·UI 이미지 |
| `SlaytheSpire/Assets/_Game/Audio` | 음악·효과음 |
| `Docs/Collaboration.md` | 담당 영역과 협업 규칙 |

새 게임 파일은 `_Game` 안에 만듭니다. 기존 템플릿 씬·입력·렌더링 설정은 원래 위치에 있습니다.

## 역할과 협업

네 명의 코딩 작업은 전투 / 카드 / UI / 진행 영역으로 나누는 것을 제안합니다. 상세 범위는 [협업 문서](Docs/Collaboration.md)에 정리했습니다.

작업 전 최신 main을 받고, `feature/기능명` 브랜치에서 수정한 뒤 Pull Request(PR)를 올립니다. 한 명이 확인하고 main에 합칩니다. 같은 씬·Prefab을 동시에 편집하기 전에는 담당자끼리 공유합니다.

## Git에 올릴 파일

`Assets`와 해당 `.meta`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings`, 문서를 함께 올립니다. `Library`, `Temp`, `Logs`, `UserSettings`, 빌드 결과와 IDE 생성 파일은 `.gitignore`로 제외합니다.
