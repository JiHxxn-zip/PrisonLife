using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handcuffs → PrisonerNpc 변환 엔진
// 플레이어/NPC에게서 수갑을 받아 줄 서 있는 죄수 NPC에게 결제로 전달하고,
// 결제가 완료된 NPC는 PrisonZone으로 이동한다.
// Inspector에서 HandcuffZone / PrisonZone / queuePoints를 연결 필요.
[DisallowMultipleComponent]
public class HandcuffsMoneyExchangeZone : MonoBehaviour
{
    [Header("연결 Zone")]
    [SerializeField] private HandcuffZone handcuffZone;

    [Header("감옥")]
    [SerializeField] private PrisonZone prisonZone;

    [Header("결제 연출")]
    [Tooltip("Money를 던져 쌓을 대상 MoneyZone")]
    [SerializeField] private MoneyZone moneyZone;

    [Header("죄수 NPC")]
    [SerializeField] private GameObject prisonerPrefab;
    [Tooltip("초기 스폰 인원 (씬 시작 시 줄 세울 NPC 수)")]
    [SerializeField] private int initialQueueSize = 2;

    [Header("줄 서기 위치 (Index 0 = 결제 위치, 이후 대기 순서)")]
    [SerializeField] private Transform[] queuePoints;

    [Header("Handcuff 적층 오프셋")]
    [SerializeField] private Vector3 handcuffLocalOffset = Vector3.zero;
    [SerializeField] private float   handcuffSpacingY = 0.15f;

    [Header("Timing")]
    [Tooltip("수갑 1개를 NPC에게 전달하는 간격 (초)")]
    [SerializeField] private float processInterval = 0.5f;

    // ── 런타임 ────────────────────────────────────────────────

    private Transform handcuffAnchor;

    private readonly List<GameObject>   zoneHandcuffs = new List<GameObject>();
    private readonly List<PrisonerNpc>  npcQueue      = new List<PrisonerNpc>();

    private bool isProcessing;
    private int  totalSpawnedCount;

    // ── 공개 프로퍼티 ─────────────────────────────────────────

    public Transform HandcuffAnchorTransform => handcuffAnchor;
    public int       ZoneHandcuffsCount      => zoneHandcuffs.Count;

    // MoneyZone 하위 호환 스텁 (MoneyZone이 씬에 남아있어도 컴파일 유지)
    public int  ActiveMoneyCount => 0;
    public bool TakeTopMoney()   => false;

    // ── 초기화 ────────────────────────────────────────────────

    private void Awake()
    {
        if (handcuffZone != null)
        {
            handcuffAnchor = handcuffZone.transform;
            handcuffZone.Initialize(this);
        }
        else
        {
            Debug.LogWarning("[HandcuffsMoneyExchangeZone] HandcuffZone 미연결");
            handcuffAnchor = transform;
        }

        if (moneyZone != null)
            moneyZone.Initialize(this);
    }

    private void Start()
    {
        for (int i = 0; i < initialQueueSize; i++)
            SpawnPrisoner();
    }

    // ── 플레이어 입력 (HandcuffZone → 호출) ──────────────────

    public void ReceiveHandcuffsFromPlayer(PlayerAgent player)
    {
        if (player == null) return;

        HandcuffsHoldStack holdStack = player.GetHandcuffsHoldStack();
        if (holdStack == null || holdStack.Count <= 0) return;

        List<GameObject> released = holdStack.ReleaseAll();
        AcceptHandcuffs(released);

        Debug.Log($"[HandcuffsMoneyExchangeZone] 플레이어 Handcuffs {released.Count}개 수령");
    }

    // ── NPC 입력 (NpcDeliveryAgent → 호출) ───────────────────

    public void ReceiveHandcuffsFromNpc(List<GameObject> handcuffs)
    {
        if (handcuffs == null || handcuffs.Count == 0) return;

        AcceptHandcuffs(handcuffs);

        Debug.Log($"[HandcuffsMoneyExchangeZone] NPC로부터 Handcuffs {handcuffs.Count}개 수령");
    }

    // ── 공통 수령 로직 ────────────────────────────────────────

    private void AcceptHandcuffs(List<GameObject> handcuffs)
    {
        foreach (GameObject hc in handcuffs)
        {
            if (hc == null) continue;
            hc.transform.SetParent(handcuffAnchor, false);
            zoneHandcuffs.Add(hc);

            ItemPickup pickup = hc.GetComponent<ItemPickup>();
            if (pickup != null) pickup.SetPickupEnabled(false);
        }

        RebuildHandcuffTransforms();

        if (!isProcessing)
            StartCoroutine(ProcessHandcuffs());
    }

    // ── 처리 사이클 ───────────────────────────────────────────

    private IEnumerator ProcessHandcuffs()
    {
        isProcessing = true;

        while (zoneHandcuffs.Count > 0)
        {
            // 감옥 만원 → 빈자리가 생길 때까지 대기 (감옥 확장 시 자동 재개)
            while (prisonZone != null && prisonZone.IsFull)
                yield return null;

            // 큐에 NPC가 없으면 스폰될 때까지 대기
            while (npcQueue.Count == 0)
                yield return null;

            // 이미 떠난 NPC가 큐 앞에 남아있으면 제거
            PrisonerNpc frontNpc = npcQueue[0];
            if (frontNpc == null
                || frontNpc.State == PrisonerNpcState.GoingToJail
                || frontNpc.State == PrisonerNpcState.InJail)
            {
                npcQueue.RemoveAt(0);
                continue;
            }

            yield return new WaitForSeconds(Mathf.Max(0f, processInterval));

            if (zoneHandcuffs.Count == 0) break;

            // 수갑 1개 소모
            int lastIdx = zoneHandcuffs.Count - 1;
            GameObject hc = zoneHandcuffs[lastIdx];
            zoneHandcuffs.RemoveAt(lastIdx);
            if (hc != null) hc.SetActive(false);

            RebuildHandcuffTransforms();

            // 죄수 NPC에 수납
            frontNpc.StartPayment();
            bool paymentComplete = frontNpc.ReceiveHandcuff();

            Debug.Log($"[HandcuffsMoneyExchangeZone] 수갑 수납 → {frontNpc.name}, 완료={paymentComplete}");

            if (!paymentComplete) continue;

            // 결제 완료 연출: 보유 수갑 수만큼 Money를 MoneyZone으로 던짐
            if (moneyZone != null)
                moneyZone.LaunchMoneyBatch(frontNpc.transform.position, frontNpc.PaymentCost);

            // 감옥 슬롯 예약 — 가득 찼으면 공간이 생길 때까지 대기
            if (prisonZone != null)
            {
                while (!prisonZone.TryReserveSlot())
                    yield return null;
            }

            frontNpc.DepartToJail();
            npcQueue.RemoveAt(0);

            // 나머지 NPC 큐 포지션 앞으로 이동
            ShiftQueuePositions();

            // 새 NPC 보충
            SpawnPrisoner();
        }

        isProcessing = false;
        Debug.Log("[HandcuffsMoneyExchangeZone] 처리 완료");
    }

    // ── NPC 큐 관리 ──────────────────────────────────────────

    private void SpawnPrisoner()
    {
        if (prisonerPrefab == null)
        {
            Debug.LogWarning("[HandcuffsMoneyExchangeZone] prisonerPrefab 미설정");
            return;
        }

        int       queueIndex = npcQueue.Count;
        Transform queuePoint = GetQueuePoint(queueIndex);
        int       cost       = (Random.value < 0.5f) ? 2 : 4;

        GameObject instance = Instantiate(prisonerPrefab);
        instance.name = $"Prisoner_{totalSpawnedCount++}";

        // 스폰 위치를 큐 포인트로 설정
        if (queuePoint != null)
            instance.transform.position = queuePoint.position;

        PrisonerNpc npc = instance.GetComponent<PrisonerNpc>();
        if (npc == null)
        {
            Debug.LogError("[HandcuffsMoneyExchangeZone] prisonerPrefab에 PrisonerNpc 컴포넌트 없음");
            Destroy(instance);
            return;
        }

        npc.Initialize(cost, queuePoint, prisonZone, this);
        npcQueue.Add(npc);

        Debug.Log($"[HandcuffsMoneyExchangeZone] 죄수 NPC 스폰 (비용={cost}, 큐 인덱스={queueIndex})");
    }

    private void ShiftQueuePositions()
    {
        for (int i = 0; i < npcQueue.Count; i++)
        {
            Transform point = GetQueuePoint(i);
            if (npcQueue[i] != null)
                npcQueue[i].UpdateQueuePoint(point);
        }
    }

    private Transform GetQueuePoint(int index)
    {
        if (queuePoints != null && index < queuePoints.Length)
            return queuePoints[index];
        return null;
    }

    // ── PrisonerNpc 콜백 ─────────────────────────────────────

    // PrisonerNpc.ArriveAtJail() → 호출됨
    public void OnPrisonerArrived(PrisonerNpc npc)
    {
        Debug.Log($"[HandcuffsMoneyExchangeZone] {npc.name} 감옥 도착");
    }

    // ── 내부 유틸 ─────────────────────────────────────────────

    private void RebuildHandcuffTransforms()
    {
        for (int i = 0; i < zoneHandcuffs.Count; i++)
        {
            GameObject hc = zoneHandcuffs[i];
            if (hc == null) continue;
            Vector3 lp = handcuffLocalOffset;
            lp.x = 0f;
            lp.z = 0f;
            lp.y = handcuffLocalOffset.y + handcuffSpacingY * i;
            hc.transform.localPosition = lp;
            hc.transform.localRotation = Quaternion.identity;
        }
    }
}
