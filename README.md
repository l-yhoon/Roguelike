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

현재 Battle 씬은 아이언클래드의 턴으로 시작합니다. `턴 종료`를 누르면 적이 머리 위에 표시된 행동을 실행하고, 전투가 계속되면 다음 플레이어 턴이 시작됩니다. 각 진영의 방어도는 자신의 턴 시작에 초기화되며, 플레이어는 이때 기본 에너지를 회복합니다. 피해는 방어도부터 차감합니다. 체력이 0이 되면 화면에 승패를 표시하고 전투를 종료합니다. 제공받은 참고 이미지를 바탕으로 배경·캐릭터·UI 리소스를 정리하고 한글 HUD를 적용했습니다. 기본 적은 임시 슬라임입니다. 카드 사용은 후속 단계입니다.

## 전투 HUD

적의 행동 의도는 해당 적 오브젝트의 자식이며, 화면 고정 좌표를 사용하지 않습니다. `CombatantView`가 스프라이트의 보이는 범위와 Image의 종횡비를 반영해 머리 위치를 계산하고, `EnemyIntentView`가 머리 위 10 UI 단위의 여백을 유지합니다. 적 이동·이미지 크기 변경·스프라이트 교체 시에도 따라갑니다. 위쪽 투명 여백을 제외하려면 적 스프라이트는 Tight 메시 또는 투명 여백을 제거한 스프라이트를 사용합니다.

전투 영역은 16:9 비율을 유지하며 창 안에 전부 들어오도록 자동 조절됩니다. 1920×1080·1280×720에서는 화면을 채우고, 16:10·4:3·울트라와이드 등에서는 상하 또는 좌우 여백을 둡니다. 배경과 UI를 함께 같은 비율로 축소하므로 아이콘이나 배경이 찌그러지지 않습니다.

`Battle Canvas`는 Canvas Scaler의 `Scale With Screen Size / Expand`를 사용하고, 자식 `Battle Viewport`는 `Aspect Ratio Fitter / Fit In Parent / 16:9`를 사용합니다. 하단 더미·에너지·턴 종료는 각각 좌하단·우하단 앵커로 고정하며 숫자 배지까지 가장자리 여백 안에 배치합니다. 상단 정보는 왼쪽, 층수는 중앙, 지도·덱·설정은 오른쪽 앵커를 사용합니다. 위치 수정은 `Battle Viewport` 아래에서 합니다.

Game 뷰에서 전체 결과를 보려면 Scale을 Fit 또는 해당 뷰에 맞는 배율로 사용하세요. 화면보다 크게 확대하면 Unity 에디터의 Game 뷰 자체가 일부를 가릴 수 있습니다.

`Roguelike → Fix Battle Layout Anchors`는 현재 에셋과 버튼 연결을 유지하면서 앵커 배치만 다시 적용합니다. UI 생성 도구에도 같은 배치가 연결돼 있습니다. 캡처 도구는 실제 Canvas Scaler 설정을 반영해 지정 해상도로 렌더링합니다.

- 상단: 플레이어 이름·캐릭터 종류·현재/최대 체력·골드·포션 슬롯 3칸·현재 층수·지도·전체 덱 수·설정을 표시합니다.
- 하단: 에너지, 뽑을 카드 수, 버린 카드 수, 턴 종료 버튼을 표시합니다. 숫자는 이미지에 포함하지 않고 데이터와 연결합니다.
- `Battle Setup`의 `Run State`에서 플레이어 이름, 초기 골드·층수, 포션 슬롯, 보유 덱을 설정합니다. 초기 덱은 타격 5장·수비 4장·강타 1장입니다.
- 전투를 시작하면 보유 덱을 복사해 섞고, 매 플레이어 턴 시작에 5장을 뽑습니다. 시작 화면에는 손패 5장·뽑을 카드 5장·버린 카드 0장이 표시됩니다. 턴을 끝내면 손패를 버리고, 뽑을 카드가 떨어지면 버린 카드를 다시 섞습니다.
- 손패는 화면 아래 중앙에서 카드 수에 맞춰 부채꼴로 펼쳐집니다. 마우스를 올리면 카드가 확대되고, 강타에는 취약 설명이 함께 표시됩니다. 카드 사용과 효과 적용은 후속 작업입니다. 카드 정의와 이미지는 `Data/Cards`와 `Art/Cards`에 있습니다.
- 뽑을 카드·버린 카드·전체 덱 버튼은 해당 카드 목록을 엽니다. 포션 슬롯은 현재 내용 또는 빈 슬롯 안내를 엽니다. 포션 사용은 아직 구현하지 않았습니다.
- 지도 버튼은 현재 층을 표시하는 팝업을 엽니다. 실제 경로 생성·이동은 후속 작업입니다.
- 설정에서 전체 화면과 전체 음량을 변경할 수 있습니다. 설정 저장은 아직 구현하지 않았습니다.
- 전투를 다시 초기화하려면 Play를 재시작하거나 `BattleSetup.InitializeBattle()`을 호출합니다. 체력·에너지·전투용 카드 더미는 초기화하고, 같은 RunState의 골드·층수·보유 덱은 유지합니다.
- 이미지: `Art/Characters/Ironclad.png`, `Art/Backgrounds/Act1Dungeon.png`, `Art/UI/Reference/*.png`. `GenerationManifest.json`에 사용한 참조와 프롬프트를 기록합니다. 내장 imagegen 도구로 생성·편집한 리소스이며, 참고 원본을 그대로 추출한 결과와는 세부 묘사가 다를 수 있습니다.
- 한글 글꼴: Noto Sans CJK KR. 공개 SIL OFL 라이선스를 `Art/Fonts/OFL.txt`에 함께 포함합니다.

`Roguelike → Apply Reference Battle HUD`는 기존 Battle 씬의 이전 프로토타입 HUD를 교체하는 에디터 도구입니다. 이후 직접 수정한 HUD를 다시 생성할 때는 변경 내용을 먼저 커밋하세요.

## 전투 데이터 수정과 적 추가

- 플레이어: `Assets/_Game/Data/Characters/Ironclad.asset`에서 이름·이미지·최대 체력·기본 에너지를 수정합니다.
- 초기 적: `Assets/_Game/Data/Enemies/TrainingSlime.asset`은 테스트용 일반 적입니다. 원작 적의 수치·행동을 재현한 에셋은 아닙니다.
- 새 적: Project 창에서 Create → Roguelike → Enemy를 선택하고 ID, 이름, 이미지, 최대 체력, Actions를 설정합니다. 적은 목록 순서대로 표시된 행동을 실행하고, 마지막 행동 다음에는 첫 행동으로 돌아갑니다.
- 후보 등록: `Act1NormalEnemyPool.asset`의 Candidates에 새 적을 추가합니다. 유효한 서로 다른 에셋을 동일 확률로 선택하며, 누락·잘못된 후보는 제외하고 중복 참조는 한 번만 셉니다. 유효한 후보가 없으면 오류를 표시하고 전투 시작을 중단합니다.
- 원본 설정과 현재 체력·방어도·에너지는 분리돼 있습니다. 전투 재초기화는 현재 체력까지 초기화합니다. 추후 연속 전투에서는 진행 상태의 체력을 별도로 전달해야 합니다.
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
