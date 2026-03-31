using UnityEngine;

// 플레이어 초반 동선 안내 튜토리얼 매니저
//
// Step1 CollectionZonePool  — 3D 화살표 그리드 위 바운스. Metal 채집 성공 시 다음 단계
// Step2 MetalExchangeSellTrigger       — 2D 화살표 활성화 + 방향 추적. 트리거 시 다음 단계
// Step3 MetalExchangeHandcuffsCollect  — 2D 화살표 유지 + 3D 화살표 재활성화. 트리거 시 다음
// Step4 HandcuffZone        — 2D 화살표 비활성화, 3D 화살표 위치 이동. 트리거 시 다음 단계
// Step5 MoneyZone           — 3D 화살표 위치 이동. 트리거 시 튜토리얼 종료 + LevelUpZone 활성화

[DisallowMultipleComponent]
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public enum Step
    {
        Step1_CollectMetal,
        Step2_SellMetal,
        Step3_CollectHandcuffs,
        Step4_HandcuffZone,
        Step5_MoneyZone,
        Complete,

        // ── Chapter 2 ──────────────────────────
        Ch2_GoToGate,
        Ch2_GoToWeapon,
        Ch2_GoToMonster,
        Ch2_GoToBase,
        Ch2_Complete
    }

    [Header("Player")]
    [SerializeField] private PlayerAgent playerAgent;
    [SerializeField] private HyperCasualPlayerController playerAgent2;

    [Header("Tutorial Targets (순서대로)")]
    [SerializeField] private CollectionZonePool collectionZonePool;                             // Step 1
    [SerializeField] private MetalExchangeSellTrigger sellTrigger;                              // Step 2
    [SerializeField] private MetalExchangeHandcuffsCollectTrigger handcuffsCollectTrigger;      // Step 3
    [SerializeField] private HandcuffZone handcuffZone;                                         // Step 4
    [SerializeField] private MoneyZone moneyZone;                                               // Step 5

    [Header("완료 시 활성화")]
    [SerializeField] private LevelUpZone levelUpZone;

    [Header("챕터 2 시작 시 비활성화")]
    [Tooltip("NpcCollectorAgent는 HiringZone에서 동적 생성되므로 런타임에 FindObjectsOfType으로 수집")]
    [SerializeField] private NpcDeliveryAgent[] npcDeliveries;

    [Header("챕터 2")]
    [SerializeField] private ChapterClearPopup chapterClearPopup;
    [SerializeField] private GateTrigger gateTrigger;
    [Tooltip("챕터 2 시작 시 카메라가 이동할 Gate 위치")]
    [SerializeField] private Transform gateViewTarget;
    [Tooltip("2D 화살표가 가리킬 무기 위치")]
    [SerializeField] private WeaponPickup ch2WeaponPickup;
    [Tooltip("무기 습득 후 2D 화살표가 가리킬 몬스터 위치")]
    [SerializeField] private Transform ch2MonsterTarget;
    [SerializeField] private MonsterZone ch2MonsterZone;
    [Tooltip("몬스터 전멸 후 카메라가 이동할 기지 위치")]
    [SerializeField] private Transform ch2BaseTarget;
    [SerializeField] private Ch2BaseZone ch2BaseZone;

    [Header("카메라 연출")]
    [SerializeField] private float cinematicMoveDuration = 1.5f;
    [SerializeField] private float cinematicHoldDuration = 1.5f;

    [Header("3D 월드 화살표")]
    [Tooltip("3D 화살표 프리팹 — 처음 사용 시 자동 Instantiate")]
    [SerializeField] private GameObject arrow3DPrefab;
    [Tooltip("타겟 위 오프셋 (Y)")]
    [SerializeField] private float arrowOffsetY = 2f;
    [Tooltip("바운스 진폭")]
    [SerializeField] private float bounceHeight = 0.4f;
    [Tooltip("바운스 속도")]
    [SerializeField] private float bounceSpeed = 2f;

    [Header("2D 플레이어 화살표")]
    [Tooltip("Sprite Z축 바운스 진폭")]
    [SerializeField] private float spriteBounceAmplitude = 0.15f;
    [Tooltip("Sprite Z축 바운스 속도")]
    [SerializeField] private float spriteBounceSpeed = 3f;

    // ── 런타임 ────────────────────────────────────────────────

    // 스프라이트 기본 로컬 회전: X90 Y90 Z0
    private static readonly Quaternion SpriteBaseRotation = Quaternion.Euler(90f, 90f, 0f);

    private Step currentStep;
    private GameObject arrow3D;        // Instantiate된 인스턴스
    private Transform arrow3DTarget;
    private Transform arrow2DTarget;
    private float spriteBaseLocalZ;    // ArrowSprite 초기 로컬 Z (바운스 기준)
    private float _arrow3DOffsetY;     // 현재 활성화된 3D 화살표의 Y 오프셋

    // PlayerArrowAgent가 런타임에 등록
    private Transform arrowPivot;
    private Transform arrowSprite;


    // ── 초기화 ────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // LevelUpZone은 튜토리얼 완료 전까지 비활성
        if (levelUpZone != null)
            levelUpZone.gameObject.SetActive(false);

        // GateTrigger는 Chapter2 시작 전까지 비활성
        if (gateTrigger != null)
            gateTrigger.gameObject.SetActive(false);

        // ChapterClearPopup 이벤트 구독
        if (chapterClearPopup != null)
        {
            chapterClearPopup.OnShown  += OnChapterClearPopupShown;
            chapterClearPopup.OnHidden += BeginChapter2;
        }

        // 트리거 이벤트 구독
        if (sellTrigger != null)
            sellTrigger.OnPlayerEntered += OnSellTriggered;
        if (handcuffsCollectTrigger != null)
            handcuffsCollectTrigger.OnPlayerEntered += OnHandcuffsCollectTriggered;
        if (handcuffZone != null)
            handcuffZone.OnPlayerEntered += OnHandcuffZoneTriggered;
        if (moneyZone != null)
            moneyZone.OnPlayerEntered += OnMoneyZoneTriggered;

        BeginStep(Step.Step1_CollectMetal);
    }

    private void OnDestroy()
    {
        if (sellTrigger != null)
            sellTrigger.OnPlayerEntered -= OnSellTriggered;
        if (handcuffsCollectTrigger != null)
            handcuffsCollectTrigger.OnPlayerEntered -= OnHandcuffsCollectTriggered;
        if (handcuffZone != null)
            handcuffZone.OnPlayerEntered -= OnHandcuffZoneTriggered;
        if (moneyZone != null)
            moneyZone.OnPlayerEntered -= OnMoneyZoneTriggered;
        if (chapterClearPopup != null)
        {
            chapterClearPopup.OnShown  -= OnChapterClearPopupShown;
            chapterClearPopup.OnHidden -= BeginChapter2;
        }
        if (gateTrigger != null)
            gateTrigger.OnGatePassed -= OnGatePassed;
        if (ch2WeaponPickup != null)
            ch2WeaponPickup.OnPickedUp -= OnCh2WeaponPickedUp;
        if (ch2MonsterZone != null)
            ch2MonsterZone.OnAllMonstersDefeated -= OnCh2AllMonstersDefeated;
        if (ch2BaseZone != null)
            ch2BaseZone.OnPlayerArrived -= OnCh2BaseArrived;
    }

    // ── 화살표 등록 (PlayerArrowAgent 호출) ───────────────────

    public void RegisterArrow(Transform pivot, Transform sprite)
    {
        arrowPivot  = pivot;
        arrowSprite = sprite;
        spriteBaseLocalZ = arrowSprite != null ? arrowSprite.localPosition.z : 0f;

        // 현재 2D 화살표가 활성 중이면 새 pivot에 즉시 반영
        if (arrowPivot != null)
            arrowPivot.gameObject.SetActive(arrow2DTarget != null);
    }

    // ── 이동 잠금 헬퍼 ───────────────────────────────────────

    private void LockPlayer1(bool locked) => playerAgent?.SetMovementLocked(locked);
    private void LockPlayer2(bool locked) => playerAgent2?.SetMovementLocked(locked);

    private void OnChapterClearPopupShown() => LockPlayer1(true);

    // ── Update ────────────────────────────────────────────────

    private void Update()
    {
        if (currentStep == Step.Complete) return;

        // Step1: Metal 채집 감지 (Poll)
        if (currentStep == Step.Step1_CollectMetal)
        {
            if (playerAgent != null && playerAgent.GetItemCount(ItemType.Metal) > 0)
                BeginStep(Step.Step2_SellMetal);
        }

        // 3D 화살표 바운스 (Y 위치만 갱신, 회전은 Set3DArrow에서 지정한 값 유지)
        if (arrow3D != null && arrow3D.activeSelf && arrow3DTarget != null)
        {
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            arrow3D.transform.position = arrow3DTarget.position + Vector3.up * (_arrow3DOffsetY + bounce);
        }

        // 2D 화살표 방향 추적
        if (arrowPivot != null && arrowPivot.gameObject.activeSelf && arrow2DTarget != null && playerAgent != null)
        {
            // ArrowPivot: 현재 등록된 Pivot 위치 → 타겟 방향으로 Y축 회전
            Vector3 toTarget = arrow2DTarget.position - arrowPivot.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                arrowPivot.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

            // ArrowSprite: 기본 로컬 회전(X90 Y90 Z0) 유지 + Z축 바운스
            if (arrowSprite != null)
            {
                arrowSprite.localRotation = SpriteBaseRotation;

                Vector3 lp = arrowSprite.localPosition;
                lp.z = spriteBaseLocalZ + Mathf.Sin(Time.time * spriteBounceSpeed) * spriteBounceAmplitude;
                arrowSprite.localPosition = lp;
            }
        }
    }

    // ── 단계 전환 ─────────────────────────────────────────────

    private void BeginStep(Step step)
    {
        currentStep = step;
        Debug.Log($"[Tutorial] → {step}");

        switch (step)
        {
            // ── Step 1: Metal 채집 안내 ──────────────────────
            case Step.Step1_CollectMetal:
                Set3DArrow(true, collectionZonePool != null ? collectionZonePool.transform : null, 0);
                Set2DArrow(false, null);
                break;

            // ── Step 2: Metal 판매 안내 ──────────────────────
            case Step.Step2_SellMetal:
                Set3DArrow(false, null);
                Set2DArrow(true, sellTrigger != null ? sellTrigger.transform : null);
                break;

            // ── Step 3: Handcuffs 수거 안내 ──────────────────
            case Step.Step3_CollectHandcuffs:
                Transform hcTarget = handcuffsCollectTrigger != null ? handcuffsCollectTrigger.transform : null;
                Set3DArrow(true, hcTarget, 0);
                Set2DArrow(true, hcTarget);
                break;

            // ── Step 4: HandcuffZone 안내 ────────────────────
            case Step.Step4_HandcuffZone:
                Set2DArrow(false, null);
                Set3DArrow(true, handcuffZone != null ? handcuffZone.transform : null, 0);
                break;

            // ── Step 5: MoneyZone 안내 ───────────────────────
            case Step.Step5_MoneyZone:
                Set3DArrow(true, moneyZone != null ? moneyZone.transform : null, 0);
                break;

            // ── Complete ─────────────────────────────────────
            case Step.Complete:
                Set3DArrow(false, null, 2, 90);
                Set2DArrow(false, null);
                StartCoroutine(CompleteCinematic());
                break;

            // ── Chapter 2: Gate 안내 ──────────────────────────
            case Step.Ch2_GoToGate:
                StartCoroutine(Chapter2GateCinematic());
                break;

            // ── Chapter 2: 무기 안내 ──────────────────────────
            case Step.Ch2_GoToWeapon:
                StartCoroutine(Chapter2WeaponCinematic());
                break;

            // ── Chapter 2: 몬스터 안내 ────────────────────────
            case Step.Ch2_GoToMonster:
                StartCoroutine(Chapter2MonsterCinematic());
                break;

            // ── Chapter 2: 기지 시네마틱 ─────────────────────
            case Step.Ch2_GoToBase:
                StartCoroutine(Chapter2BaseCinematic());
                break;

            case Step.Ch2_Complete:
                Set3DArrow(false, null);
                Set2DArrow(false, null);
                Debug.Log("[Tutorial] Chapter2 완료");
                StartCoroutine(Chapter2CompleteCinematic());
                break;
        }
    }

    // ── 완료 시네마틱 ─────────────────────────────────────────

    private System.Collections.IEnumerator CompleteCinematic()
    {
        // 플레이어 조작 잠금
        if (playerAgent != null)
            playerAgent.SetMovementLocked(true);

        // 카메라를 LevelUpZone으로 이동 (cinematicMoveDuration 초)
        if (levelUpZone != null)
            yield return CameraManager.Instance?.StartCinematicLerp(levelUpZone.transform, cinematicMoveDuration);

        // 도착 시 LevelUpZone 활성화
        if (levelUpZone != null)
            levelUpZone.gameObject.SetActive(true);

        Debug.Log("[Tutorial] 완료 — LevelUpZone 활성화");

        // cinematicHoldDuration 초 대기
        yield return new WaitForSeconds(cinematicHoldDuration);

        // 카메라를 플레이어에게 복귀 (cinematicMoveDuration 초)
        if (playerAgent != null)
            yield return CameraManager.Instance?.StartCinematicLerp(playerAgent.transform, cinematicMoveDuration);

        // 플레이어 조작 해제
        if (playerAgent != null)
            playerAgent.SetMovementLocked(false);

        Debug.Log("[Tutorial] 카메라 복귀 완료 — 조작 해제");
    }

    // ── Chapter 2 ────────────────────────────────────────────

    public void BeginChapter2()
    {
        // 동적 생성된 NpcCollectorAgent 전체 비활성화
        foreach (NpcCollectorAgent npc in FindObjectsOfType<NpcCollectorAgent>())
            npc.gameObject.SetActive(false);

        // 씬에 배치된 NpcDeliveryAgent 비활성화
        foreach (NpcDeliveryAgent npc in npcDeliveries)
            if (npc != null) npc.gameObject.SetActive(false);

        BeginStep(Step.Ch2_GoToGate);
    }

    private System.Collections.IEnumerator Chapter2GateCinematic()
    {
        // ArrowPivot 비활성화
        Set2DArrow(false, null);

        // Player1 잠금 (팝업에서 이미 잠겼지만 명시적으로 보장)
        LockPlayer1(true);

        // 카메라 → Gate로 이동
        if (gateViewTarget != null)
            yield return CameraManager.Instance?.StartCinematicLerp(gateViewTarget, cinematicMoveDuration);

        // Gate 위에 3D 화살표 표시 + GateTrigger 활성화
        Set3DArrow(true, gateViewTarget);

        if (gateTrigger != null)
        {
            gateTrigger.OnGatePassed += OnGatePassed;
            gateTrigger.gameObject.SetActive(true);
        }

        Debug.Log("[Tutorial] Chapter2 Gate 안내 완료 — Gate 활성화");

        // 잠시 감상
        yield return new WaitForSeconds(cinematicHoldDuration);

        // 카메라 → 플레이어1로 복귀 (아직 포탈 통과 전)
        if (playerAgent != null)
            yield return CameraManager.Instance?.StartCinematicLerp(playerAgent.transform, cinematicMoveDuration);

        // Player1 잠금 해제 → 포탈로 이동 가능
        LockPlayer1(false);
    }

    private System.Collections.IEnumerator Chapter2WeaponCinematic()
    {
        Transform weaponTarget = ch2WeaponPickup != null ? ch2WeaponPickup.transform : null;

        // Player2 잠금
        LockPlayer2(true);

        // 카메라 → 무기로 이동
        if (weaponTarget != null)
            yield return CameraManager.Instance?.StartCinematicLerp(weaponTarget, cinematicMoveDuration);

        // 무기 위치 감상
        yield return new WaitForSeconds(cinematicHoldDuration);

        // 카메라 → 플레이어2로 복귀
        if (playerAgent2 != null)
            yield return CameraManager.Instance?.StartCinematicLerp(playerAgent2.transform, cinematicMoveDuration);

        // Player2 잠금 해제 + 챕터2 UI 활성화
        LockPlayer2(false);
        UIManager.Instance?.ShowNextChapter();

        // 시네마틱 완료 후 2D 화살표 활성화 + 픽업 이벤트 구독
        Set2DArrow(true, weaponTarget);
        if (ch2WeaponPickup != null)
            ch2WeaponPickup.OnPickedUp += OnCh2WeaponPickedUp;
    }

    private void OnGatePassed(PlayerAgent player)
    {
        if (gateTrigger != null)
            gateTrigger.OnGatePassed -= OnGatePassed;

        BeginStep(Step.Ch2_GoToWeapon);
    }

    private System.Collections.IEnumerator Chapter2MonsterCinematic()
    {
        // ArrowPivot 비활성화
        Set2DArrow(false, null);

        // Player2 잠금
        LockPlayer2(true);

        // 카메라 → 몬스터로 이동
        if (ch2MonsterTarget != null)
            yield return CameraManager.Instance?.StartCinematicLerp(ch2MonsterTarget, cinematicMoveDuration);

        // 몬스터 위에 3D 화살표 표시
        Set3DArrow(true, ch2MonsterTarget);

        // 카메라 도착 시 MonsterZone 활성화
        if (ch2MonsterZone != null)
        {
            ch2MonsterZone.OnAllMonstersDefeated += OnCh2AllMonstersDefeated;
            ch2MonsterZone.Activate();
        }

        // 감상 대기
        yield return new WaitForSeconds(cinematicHoldDuration);

        // 카메라 → 플레이어2로 복귀
        if (playerAgent2 != null)
            yield return CameraManager.Instance?.StartCinematicLerp(playerAgent2.transform, cinematicMoveDuration);

        // Player2 잠금 해제
        LockPlayer2(false);

        // 2D 화살표 몬스터 방향으로 활성화
        Set2DArrow(true, ch2MonsterTarget);
    }

    private System.Collections.IEnumerator Chapter2BaseCinematic()
    {
        // Player2 잠금
        LockPlayer2(true);

        Set3DArrow(false, null);
        Set2DArrow(false, null);

        // 카메라 → 기지로 이동
        if (ch2BaseTarget != null)
            yield return CameraManager.Instance?.StartCinematicLerp(ch2BaseTarget, cinematicMoveDuration);

        // 기지 감상
        yield return new WaitForSeconds(cinematicHoldDuration);

        // 카메라 → 플레이어2 복귀
        if (playerAgent2 != null)
            yield return CameraManager.Instance?.StartCinematicLerp(playerAgent2.transform, cinematicMoveDuration);

        // Player2 잠금 해제
        LockPlayer2(false);

        // BaseZone 도착 대기
        if (ch2BaseZone != null)
        {
            ch2BaseZone.OnPlayerArrived += OnCh2BaseArrived;
            Set2DArrow(true, ch2BaseZone.transform);
        }
        else
        {
            BeginStep(Step.Ch2_Complete);
        }
    }

    private void OnCh2AllMonstersDefeated()
    {
        if (ch2MonsterZone != null)
            ch2MonsterZone.OnAllMonstersDefeated -= OnCh2AllMonstersDefeated;

        BeginStep(Step.Ch2_GoToBase);
    }

    private void OnCh2BaseArrived()
    {
        if (ch2BaseZone != null)
            ch2BaseZone.OnPlayerArrived -= OnCh2BaseArrived;

        Set2DArrow(false, null);
        BeginStep(Step.Ch2_Complete);
    }

    private System.Collections.IEnumerator Chapter2CompleteCinematic()
    {
        yield return new WaitForSeconds(2f);
        UIManager.Instance?.ShowClear();
    }

    private void OnCh2WeaponPickedUp()
    {
        if (ch2WeaponPickup != null)
            ch2WeaponPickup.OnPickedUp -= OnCh2WeaponPickedUp;

        BeginStep(ch2MonsterTarget != null ? Step.Ch2_GoToMonster : Step.Ch2_Complete);
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────

    private void OnSellTriggered(PlayerAgent player)
    {
        if (currentStep != Step.Step2_SellMetal) return;
        BeginStep(Step.Step3_CollectHandcuffs);
    }

    private void OnHandcuffsCollectTriggered(PlayerAgent player)
    {
        if (currentStep != Step.Step3_CollectHandcuffs) return;
        BeginStep(Step.Step4_HandcuffZone);
    }

    private void OnHandcuffZoneTriggered(PlayerAgent player)
    {
        if (currentStep != Step.Step4_HandcuffZone) return;
        BeginStep(Step.Step5_MoneyZone);
    }

    private void OnMoneyZoneTriggered(PlayerAgent player)
    {
        if (currentStep != Step.Step5_MoneyZone) return;
        BeginStep(Step.Complete);
    }

    // ── 화살표 제어 ───────────────────────────────────────────

    // offsetY : 타겟 위 높이 오프셋 (기본값 = arrowOffsetY)
    // rotationY : 화살표 Y축 회전 (기본값 = 0)
    private void Set3DArrow(bool active, Transform target, float offsetY = -1f, float rotationY = 0f)
    {
        arrow3DTarget = active ? target : null;

        if (active)
        {
            // 최초 사용 시 프리팹 Instantiate
            if (arrow3D == null && arrow3DPrefab != null)
            {
                arrow3D = Instantiate(arrow3DPrefab);
                arrow3D.name = "Arrow3D_Tutorial";
            }

            if (arrow3D != null)
            {
                _arrow3DOffsetY = offsetY < 0f ? arrowOffsetY : offsetY;

                // 활성화 전에 위치·회전을 먼저 지정해 첫 프레임 깜빡임 방지
                if (target != null)
                    arrow3D.transform.position = target.position + Vector3.up * _arrow3DOffsetY;

                arrow3D.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
                arrow3D.SetActive(true);
            }
        }
        else
        {
            if (arrow3D != null)
                arrow3D.SetActive(false);
        }
    }

    private void Set2DArrow(bool active, Transform target)
    {
        arrow2DTarget = active ? target : null;
        if (arrowPivot == null) return;
        arrowPivot.gameObject.SetActive(active);
    }
}
