using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 감옥 구역: 수감 NPC 수용 인원 관리 및 내부 배치 처리
[DisallowMultipleComponent]
public class PrisonZone : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField] private int maxCapacity = 20;

    [Header("확장 결제 Zone (만원 시 자동 활성화)")]
    [SerializeField] private PrisonExpansionZone expansionZone;

    [Header("카메라 연출 (확장 Zone 활성화 전 시네마틱)")]
    [SerializeField] private PlayerAgent          player;
    [SerializeField] private float                cameraTravelDuration = 1.5f;
    [SerializeField] private float                cameraHoldDuration   = 1.5f;

    [Header("UI")]
    [SerializeField] private TMP_Text capacityText;

    [Header("입장 경로 웨이포인트 (순서대로 경유 후 수감)")]
    [Tooltip("감옥 입구부터 내부까지 경유할 위치 목록. 비워두면 EntrancePoint 단독 사용.")]
    [SerializeField] private Transform[] entranceWaypoints;

    [Header("감옥 입구 (웨이포인트 미설정 시 단독 목표)")]
    [SerializeField] private Transform entrancePoint;

    [Header("수감 NPC 내부 배치")]
    [Tooltip("감옥 내부 배치 기준 앵커")]
    [SerializeField] private Transform prisonerAnchor;
    [SerializeField] private Vector3   firstSlotLocalOffset = Vector3.zero;
    [SerializeField] private float     spacingX = 1f;
    [SerializeField] private float     spacingZ = 1f;
    [Tooltip("X축 이동 전 Z축으로 쌓을 최대 인원")]
    [SerializeField] private int       stackDepthZ = 3;

    private readonly List<PrisonerNpc> prisoners = new List<PrisonerNpc>();
    private int reservedSlots; // 이동 중(미도착) 죄수 예약 수
    private bool expansionAvailable = true;

    // ── 공개 프로퍼티 ─────────────────────────────────────────

    // 웨이포인트가 있으면 첫 번째 포인트, 없으면 entrancePoint 반환
    public Transform EntrancePoint       => (entranceWaypoints != null && entranceWaypoints.Length > 0)
                                             ? entranceWaypoints[0]
                                             : (entrancePoint != null ? entrancePoint : transform);
    public Transform[] EntranceWaypoints => entranceWaypoints;

    // 현재 수감 + 이동 중 합산으로 만원 여부 판단
    public bool IsFull         => (prisoners.Count + reservedSlots) >= maxCapacity;
    public int  CurrentCount   => prisoners.Count;
    public int  MaxCapacity    => maxCapacity;

    // ── 초기화 ────────────────────────────────────────────────

    private void Awake()
    {
        // 확장 존은 씬 시작 시 비활성 상태로 대기
        if (expansionZone != null)
        {
            expansionZone.Initialize(this);
            expansionZone.gameObject.SetActive(false);
        }

        RefreshUI();
    }

    // ── 슬롯 예약 / 수감 등록 ─────────────────────────────────

    // NPC 출발 전 슬롯 예약 시도. 가득 찼으면 false 반환 (reservedSlots 미변경)
    public bool TryReserveSlot()
    {
        if (IsFull) return false;
        reservedSlots++;
        RefreshUI();
        return true;
    }

    // 죄수 도착 시 PrisonerNpc가 호출 → 내부 그리드에 배치
    public void RegisterPrisoner(PrisonerNpc npc)
    {
        if (reservedSlots > 0)
            reservedSlots--;

        int slotIndex = prisoners.Count;
        prisoners.Add(npc);

        Transform anchor = prisonerAnchor != null ? prisonerAnchor : transform;
        int stackZ = slotIndex % stackDepthZ; // Z축 인덱스 (0~stackDepthZ-1)
        int col    = slotIndex / stackDepthZ; // X축 인덱스
        Vector3 localPos = firstSlotLocalOffset
                           + Vector3.right   * col    * spacingX
                           + Vector3.forward * stackZ * spacingZ;

        npc.transform.SetParent(anchor, true);
        npc.transform.localPosition = localPos;
        npc.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

        RefreshUI();
        Debug.Log($"[PrisonZone] 수감 {prisoners.Count}/{maxCapacity}");

        // 실제 도착 인원이 최대치에 도달했을 때 확장 존 활성화
        if (prisoners.Count >= maxCapacity)
            ActivateExpansionZone();
    }

    // ── 감옥 확장 (PrisonExpansionZone 결제 완료 시 호출) ────

    public void ExpandCapacity(int additional)
    {
        maxCapacity += Mathf.Max(0, additional);
        RefreshUI();
        Debug.Log($"[PrisonZone] 감옥 확장 → 최대 {maxCapacity}명");
    }

    private void ActivateExpansionZone()
    {
        if (expansionZone == null || !expansionAvailable) return;

        if (player != null)
            StartCoroutine(ExpansionCinematicRoutine());
        else
            expansionZone.gameObject.SetActive(true);
    }

    private IEnumerator ExpansionCinematicRoutine()
    {
        // 플레이어 이동 제한
        player.SetMovementLocked(true);

        // 카메라를 ExpansionZone으로 이동
        Transform camTarget = expansionZone.transform;

        yield return CameraManager.Instance?.StartCinematicLerp(camTarget, cameraTravelDuration);

        // 카메라 도착 후 ExpansionZone 활성화
        expansionZone.gameObject.SetActive(true);
        Debug.Log("[PrisonZone] 만원 — 확장 결제 Zone 활성화");

        // 해당 위치에서 대기
        yield return new WaitForSeconds(cameraHoldDuration);

        // 카메라를 플레이어에게 복귀
        yield return CameraManager.Instance?.StartCinematicLerp(player.transform, cameraTravelDuration);

        // 플레이어 이동 해제
        player.SetMovementLocked(false);
    }

    public void DeactivateExpansionZone()
    {
        if (expansionZone == null) return;
        expansionZone.gameObject.SetActive(false);
        expansionAvailable = false;
        Debug.Log("[PrisonZone] 확장 결제 Zone 비활성화 — 이후 만원 시 입장 차단만 적용");
    }

    // ── UI ────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (capacityText == null) return;

        bool full = prisoners.Count >= maxCapacity;
        capacityText.text  = $"{prisoners.Count}/{maxCapacity}";
        capacityText.color = full ? Color.red : Color.white;
    }
}
