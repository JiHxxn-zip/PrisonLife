using System.Collections.Generic;
using UnityEngine;

// NPC 배달 상태
public enum NpcDeliveryState
{
    Idle,
    GoingToCollect,       // MetalExchangeZone으로 이동 & 수갑 대기/수거
    GoingToDeliver,       // HandcuffsMoneyExchangeZone으로 이동
    GoingToWaitPoint,     // 대기 장소로 이동
    WaitingForProcessing  // Zone 처리 완료 대기
}

// 두 구역(MetalExchangeZone ↔ HandcuffsMoneyExchangeZone)을 오가며 Handcuffs를 운반하는 NPC
[DisallowMultipleComponent]
public class NpcDeliveryAgent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MetalExchangeZone metalExchangeZone;
    [SerializeField] private HandcuffsMoneyExchangeZone handcuffsMoneyExchangeZone;
    [Tooltip("전달 후 Zone 처리 완료를 기다리는 대기 장소")]
    [SerializeField] private Transform waitPoint;

    [Header("NPC Anchor (수갑 적층 기준점)")]
    [SerializeField] private Transform npcAnchor;
    [SerializeField] private Vector3 stackLocalOffset = Vector3.zero;
    [SerializeField] private float stackSpacingY = 0.15f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float arrivalThreshold = 0.5f;

    [Header("Collect Retry")]
    [Tooltip("MetalExchangeZone에 수갑이 없을 때 재시도 간격 (초)")]
    [SerializeField] private float collectRetryInterval = 0.5f;

    // ── 런타임 ────────────────────────────────────────────────

    private readonly List<GameObject> heldHandcuffs = new List<GameObject>();
    private NpcDeliveryState currentState;
    private float collectRetryTimer;

    public NpcDeliveryState CurrentState => currentState;
    public int HeldCount => heldHandcuffs.Count;

    // ── 초기화 ────────────────────────────────────────────────

    private void Awake()
    {
        if (npcAnchor == null) npcAnchor = transform;
    }

    // Inspector에서 레퍼런스를 미리 설정한 경우 자동 시작
    private void Start()
    {
        if (currentState == NpcDeliveryState.Idle &&
            metalExchangeZone != null &&
            handcuffsMoneyExchangeZone != null)
        {
            SetState(NpcDeliveryState.GoingToCollect);
        }
    }

    // 코드에서 동적으로 초기화할 때 사용 (HiringZone 등)
    public void Initialize(MetalExchangeZone metalZone,
                           HandcuffsMoneyExchangeZone exchangeZone,
                           Transform waitPos)
    {
        metalExchangeZone          = metalZone;
        handcuffsMoneyExchangeZone = exchangeZone;
        waitPoint                  = waitPos;
        SetState(NpcDeliveryState.GoingToCollect);
    }

    // ── Update ────────────────────────────────────────────────

    private void Update()
    {
        switch (currentState)
        {
            case NpcDeliveryState.GoingToCollect:       TickGoingToCollect();       break;
            case NpcDeliveryState.GoingToDeliver:       TickGoingToDeliver();       break;
            case NpcDeliveryState.GoingToWaitPoint:     TickGoingToWaitPoint();     break;
            case NpcDeliveryState.WaitingForProcessing: TickWaitingForProcessing(); break;
        }
    }

    // ── GoingToCollect ────────────────────────────────────────
    // MetalExchangeZone.HandcuffsAnchor 근처로 이동 → 수갑 생산 대기 → 수거

    private void TickGoingToCollect()
    {
        if (metalExchangeZone == null) return;

        Vector3 dest = metalExchangeZone.HandcuffsAnchor.position;

        if (SqrDist2D(transform.position, dest) > arrivalThreshold * arrivalThreshold)
        {
            MoveTo(dest);
            return;
        }

        // 도착 — 수갑 수거 시도 (없으면 재시도 대기)
        collectRetryTimer -= Time.deltaTime;
        if (collectRetryTimer > 0f) return;

        if (metalExchangeZone.ProducedHandcuffsCount <= 0)
        {
            collectRetryTimer = collectRetryInterval;
            return;
        }

        List<GameObject> collected = metalExchangeZone.CollectHandcuffsForNpc();
        foreach (GameObject hc in collected)
            PushToStack(hc);

        if (heldHandcuffs.Count > 0)
        {
            Debug.Log($"[NpcDelivery:{name}] Handcuffs {heldHandcuffs.Count}개 수거 → 배달 이동");
            SetState(NpcDeliveryState.GoingToDeliver);
        }
        else
        {
            collectRetryTimer = collectRetryInterval;
        }
    }

    // ── GoingToDeliver ────────────────────────────────────────
    // HandcuffsMoneyExchangeZone.HandcuffAnchor 근처로 이동 → 수갑 전달

    private void TickGoingToDeliver()
    {
        if (handcuffsMoneyExchangeZone == null) return;

        Vector3 dest = handcuffsMoneyExchangeZone.HandcuffAnchorTransform.position;

        if (SqrDist2D(transform.position, dest) > arrivalThreshold * arrivalThreshold)
        {
            MoveTo(dest);
            return;
        }

        // 도착 — 보유 수갑 전체를 Zone에 전달
        List<GameObject> toDeliver = new List<GameObject>(heldHandcuffs);
        heldHandcuffs.Clear();

        handcuffsMoneyExchangeZone.ReceiveHandcuffsFromNpc(toDeliver);

        Debug.Log($"[NpcDelivery:{name}] Handcuffs {toDeliver.Count}개 전달 완료 → 대기 장소로 이동");
        SetState(NpcDeliveryState.GoingToWaitPoint);
    }

    // ── GoingToWaitPoint ──────────────────────────────────────
    // waitPoint로 이동 후 WaitingForProcessing으로 전환

    private void TickGoingToWaitPoint()
    {
        if (waitPoint == null)
        {
            // waitPoint 미설정 시 제자리에서 대기
            SetState(NpcDeliveryState.WaitingForProcessing);
            return;
        }

        Vector3 dest = waitPoint.position;

        if (SqrDist2D(transform.position, dest) > arrivalThreshold * arrivalThreshold)
        {
            MoveTo(dest);
            return;
        }

        SetState(NpcDeliveryState.WaitingForProcessing);
    }

    // ── WaitingForProcessing ──────────────────────────────────
    // Zone의 수갑이 모두 소모되면 다시 수거 루프 시작

    private void TickWaitingForProcessing()
    {
        if (handcuffsMoneyExchangeZone == null)
        {
            SetState(NpcDeliveryState.GoingToCollect);
            return;
        }

        if (handcuffsMoneyExchangeZone.ZoneHandcuffsCount <= 0)
        {
            Debug.Log($"[NpcDelivery:{name}] Zone 처리 완료 → 다시 수거");
            SetState(NpcDeliveryState.GoingToCollect);
        }
    }

    // ── 스택 비주얼 ───────────────────────────────────────────

    // 수갑 1개를 NPC 앵커에 쌓는다 (ItemStackInventory와 동일한 규칙)
    private void PushToStack(GameObject hc)
    {
        if (hc == null) return;

        hc.transform.SetParent(npcAnchor, false);
        hc.transform.localPosition = stackLocalOffset + Vector3.up * stackSpacingY * heldHandcuffs.Count;
        hc.transform.localRotation = Quaternion.identity;
        hc.SetActive(true);

        heldHandcuffs.Add(hc);
    }

    // ── 유틸 ──────────────────────────────────────────────────

    private void SetState(NpcDeliveryState newState)
    {
        currentState = newState;
        if (newState == NpcDeliveryState.GoingToCollect)
            collectRetryTimer = 0f;
        Debug.Log($"[NpcDelivery:{name}] → {newState}");
    }

    private void MoveTo(Vector3 destination)
    {
        Vector3 pos   = transform.position;
        destination.y = pos.y; // 지면 기준 Y 고정

        Vector3 dir = destination - pos;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.position = Vector3.MoveTowards(pos, destination, moveSpeed * Time.deltaTime);

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation   = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

    private static float SqrDist2D(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    private void OnDisable()
    {
        // NPC 비활성화 시 쌓인 수갑 숨기기
        foreach (GameObject hc in heldHandcuffs)
        {
            if (hc != null) hc.SetActive(false);
        }
    }
}
