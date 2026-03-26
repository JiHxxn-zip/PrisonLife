using UnityEngine;

public enum PrisonerNpcState
{
    InQueue,      // 줄 대기 위치로 이동 중 / 대기
    AtPayment,    // 결제 위치 도달, 수갑 수납 중
    GoingToJail,  // 결제 완료, 감옥으로 이동 중
    InJail        // 감옥 수감 완료
}

// 죄수 NPC: 결제(수갑 수납) → 감옥 이동 흐름을 처리
[DisallowMultipleComponent]
public class PrisonerNpc : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float arrivalThreshold = 0.3f;

    // ── 런타임 (Initialize에서 주입) ──────────────────────────
    private int paymentCost;
    private int receivedCount;
    private Transform targetQueuePoint;
    private PrisonZone prisonZone;
    private HandcuffsMoneyExchangeZone exchangeZone;

    // 감옥 입장 경로
    private Transform[] jailWaypoints;
    private int currentWaypointIndex;

    private PrisonerNpcState state = PrisonerNpcState.InQueue;

    public PrisonerNpcState State    => state;
    public int               PaymentCost     => paymentCost;
    public bool              IsPaymentComplete => receivedCount >= paymentCost;

    // ── 초기화 (HandcuffsMoneyExchangeZone에서 호출) ──────────

    public void Initialize(int cost, Transform queuePoint, PrisonZone prison, HandcuffsMoneyExchangeZone exchange)
    {
        paymentCost      = cost;
        receivedCount    = 0;
        targetQueuePoint = queuePoint;
        prisonZone       = prison;
        exchangeZone     = exchange;
        SetState(PrisonerNpcState.InQueue);
    }

    // 큐 이동 시 목표 위치 갱신
    public void UpdateQueuePoint(Transform newPoint)
    {
        targetQueuePoint = newPoint;
    }

    // 큐 맨 앞에 도달했을 때 ExchangeZone이 호출
    public void StartPayment()
    {
        if (state == PrisonerNpcState.InQueue)
            SetState(PrisonerNpcState.AtPayment);
    }

    // 수갑 1개 수납. 결제 완료 시 true 반환
    public bool ReceiveHandcuff()
    {
        if (state != PrisonerNpcState.AtPayment) return false;
        receivedCount++;
        Debug.Log($"[PrisonerNpc:{name}] 수갑 수납 {receivedCount}/{paymentCost}");
        return IsPaymentComplete;
    }

    // 결제 완료 후 ExchangeZone이 호출
    public void DepartToJail()
    {
        // PrisonZone에서 웨이포인트 목록을 가져와 첫 번째부터 순차 이동
        jailWaypoints        = prisonZone != null ? prisonZone.EntranceWaypoints : null;
        currentWaypointIndex = 0;
        SetState(PrisonerNpcState.GoingToJail);
    }

    // ── Update ────────────────────────────────────────────────

    private void Update()
    {
        switch (state)
        {
            case PrisonerNpcState.InQueue:
            case PrisonerNpcState.AtPayment:
                TickMoveToQueue();
                break;
            case PrisonerNpcState.GoingToJail:
                TickGoingToJail();
                break;
        }
    }

    private void TickMoveToQueue()
    {
        if (targetQueuePoint == null) return;
        MoveTo(targetQueuePoint.position);
    }

    private void TickGoingToJail()
    {
        // 웨이포인트가 남아있으면 순서대로 경유
        if (jailWaypoints != null && currentWaypointIndex < jailWaypoints.Length)
        {
            Transform wp = jailWaypoints[currentWaypointIndex];
            if (wp == null)
            {
                // null 웨이포인트는 건너뜀
                currentWaypointIndex++;
                return;
            }

            MoveTo(wp.position);

            if (SqrDist2D(transform.position, wp.position) <= arrivalThreshold * arrivalThreshold)
                currentWaypointIndex++;

            return;
        }

        // 모든 웨이포인트 통과(또는 미설정) → 감옥 도착 처리
        if (prisonZone == null)
        {
            ArriveAtJail();
            return;
        }

        // 웨이포인트가 없고 EntrancePoint만 있는 경우 단독 목표로 이동
        if (jailWaypoints == null || jailWaypoints.Length == 0)
        {
            Vector3 dest = prisonZone.EntrancePoint.position;
            MoveTo(dest);

            if (SqrDist2D(transform.position, dest) <= arrivalThreshold * arrivalThreshold)
                ArriveAtJail();

            return;
        }

        // 웨이포인트 전부 통과 완료
        ArriveAtJail();
    }

    private void ArriveAtJail()
    {
        SetState(PrisonerNpcState.InJail);
        prisonZone?.RegisterPrisoner(this);
        exchangeZone?.OnPrisonerArrived(this);
    }

    // ── 유틸 ──────────────────────────────────────────────────

    private void SetState(PrisonerNpcState newState)
    {
        state = newState;
        Debug.Log($"[PrisonerNpc:{name}] → {newState}");
    }

    private void MoveTo(Vector3 destination)
    {
        Vector3 pos   = transform.position;
        destination.y = pos.y;

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
}
