# Prison Life

> Unity로 개발한 모바일 하이퍼캐주얼 감옥 경영 시뮬레이션 게임  
> 자원 수집, NPC 자동화, 감옥 확장, 전투 시스템까지 구현한 프로젝트

![Prison Life 데모](./Docs/PrisonLife.gif)

🎥 [게임 플레이 영상 보기](https://drive.google.com/file/d/1Y6Gkcy7GrPQe-Xdmw-8bBURrZwgIj9KN/view?usp=drive_link)

## 프로젝트 개요

**Prison Life**는 Unity로 개발한 모바일 하이퍼캐주얼 감옥 경영 시뮬레이션 게임입니다.  
플레이어는 자원을 수집하고, NPC를 고용하며, 감옥을 확장하면서 수익을 창출합니다.

챕터 2에서는 전투 요소가 추가되어, 단순 경영 루프를 넘어 액션 기반 플레이로 확장되도록 설계했습니다.  

---
- **장르**: 하이퍼캐주얼 / 방치형 시뮬레이션
- **플랫폼**: 모바일 (720×1280)
- **엔진**: Unity

- ## 아키텍처 및 기술 특징

### 디자인 패턴
- **Singleton**: `UIManager`, `CameraManager`, `TutorialManager`
- **Object Pool**: `BulletPool`, `MoneyZone`
- **State Machine**: `NpcCollectorAgent`
- **Observer / Event**: `HpComponent`의 `OnHPChanged`
- **Template Method**: `MonsterBase`, `WeaponBase`

### 구조적 특징
- **Data-View 분리**: `HpComponent`(데이터) + `OverheadHpBar`(뷰)
- **모듈형 Zone 시스템**: `BaseZone` 상속 기반 설계
- **UI 계층 분리**: 챕터별 `IChapterUI` 인터페이스 적용
- **Arc Flight 연출**: Metal, Money, Handcuffs의 포물선 이동 구현
- **단일 씬 + 챕터 전환 구조** 적용
