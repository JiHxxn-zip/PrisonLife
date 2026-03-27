# 대화 로그 — PrisonLife Unity 작업

날짜: 2026-03-27

---

## 1. NpcCollectorAgent — 플레이어가 들고있는 Metal 추적 방지

**요청:** `NpcCollectorAgent.cs`가 Player에 붙어 있는 Metal을 따라가는 문제 수정.
`MetalCollectionZone` 안의 메탈만 추적하고, 플레이어가 들고있는 메탈은 추적하지 않도록 예외처리.

**원인 분석:**
`ItemStackInventory`가 수집된 메탈 비주얼 프리팹을 플레이어 백 앵커(`SetParent`)로 붙임.
해당 비주얼에 `ItemPickup` 컴포넌트가 있으면 `FindObjectsOfType<ItemPickup>()`에 잡혀 NPC가 추적.

**수정 파일:** `Assets/02.Scripts/NpcCollectorAgent.cs`

**수정 내용:** `FindNearestAvailableMetal()` 내 필터 조건 추가 (line 152)

```csharp
if (pickup.GetComponentInParent<PlayerAgent>() != null) continue; // 플레이어가 들고있는 Metal 제외
```

---

## 2. NpcDeliveryAgent — WaitPoint 도착 시 Y축 90도 회전 고정

**요청:** `NpcDeliveryAgent.cs`에서 WaitPoint에 기다릴 때 Y축 90도 회전 상태로 대기.

**수정 파일:** `Assets/02.Scripts/NpcDeliveryAgent.cs`

**수정 내용:**

Inspector 필드 추가:
```csharp
[Header("Wait Point")]
[Tooltip("대기 장소 도착 시 바라볼 Y축 각도")]
[SerializeField] private float waitFacingAngleY = 90f;
```

`TickGoingToWaitPoint()` 도착 직후 회전 적용:
```csharp
transform.rotation = Quaternion.Euler(0f, waitFacingAngleY, 0f);
SetState(NpcDeliveryState.WaitingForProcessing);
```

---

## 3. PrisonZone — 죄수 도착 시 Y축 -90도 회전 고정

**요청:** 죄수가 감옥에 도착했을 때 Y축 -90도 회전으로 고정.

**수정 파일:** `Assets/02.Scripts/PrisonZone.cs`

**수정 내용:** `RegisterPrisoner()` 내 회전값 변경 (line 100)

```csharp
// 변경 전
npc.transform.localRotation = Quaternion.identity;

// 변경 후
npc.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
```

---

## 4. MetalExchangeZone — 작업 중 월드 UI 인디케이터 애니메이션

**요청:** 공장이 작업 중일 때 월드 UI 이미지 한 장을 두둥실 뜨는 애니메이션 + Z축 회전 연출 추가.

**수정 파일:** `Assets/02.Scripts/MetalExchangeZone.cs`

**수정 내용:**

Inspector 필드 추가:
```csharp
[Header("Working Indicator")]
[Tooltip("작업 중일 때 표시할 월드 UI 이미지 오브젝트")]
[SerializeField] private GameObject workingIndicator;
[Tooltip("두둥실 상하 진폭 (유닛)")]
[SerializeField] private float bobAmplitude = 0.15f;
[Tooltip("상하 왕복 속도")]
[SerializeField] private float bobSpeed = 2f;
[Tooltip("Z축 회전 속도 (도/초)")]
[SerializeField] private float spinSpeed = 90f;
```

런타임 변수 추가:
```csharp
private Vector3 indicatorBaseLocalPos;
private Coroutine indicatorCoroutine;
```

`Awake()`에서 초기 로컬 위치 저장 및 비활성화:
```csharp
if (workingIndicator != null)
{
    indicatorBaseLocalPos = workingIndicator.transform.localPosition;
    workingIndicator.SetActive(false);
}
```

`ProcessCycle()` 시작/종료 시 인디케이터 토글:
```csharp
isProcessing = true;
SetWorkingIndicator(true);
// ... 처리 루프 ...
isProcessing = false;
SetWorkingIndicator(false);
```

애니메이션 코루틴:
```csharp
private IEnumerator AnimateIndicator()
{
    float time = 0f;
    while (true)
    {
        time += Time.deltaTime;

        // 상하 두둥실
        Vector3 lp = indicatorBaseLocalPos;
        lp.y += Mathf.Sin(time * bobSpeed) * bobAmplitude;
        workingIndicator.transform.localPosition = lp;

        // Z축 회전
        workingIndicator.transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.Self);

        yield return null;
    }
}
```

`OnDisable()`에서 인디케이터 정리 추가.

---
