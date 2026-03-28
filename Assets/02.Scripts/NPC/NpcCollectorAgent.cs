using UnityEngine;

// NPC 행동 상태 — 확장 시 여기에 추가
public enum NpcState
{
    Idle,
    SearchingMetal,  // 가장 가까운 활성 Metal 탐색
    MovingToMetal,   // Metal 위치로 이동
    Collecting       // 1초 수집 행동 후 Metal 날려보내기
}

[DisallowMultipleComponent]
public class NpcCollectorAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float arrivalThreshold = 0.5f;

    [Header("Collect")]
    [SerializeField] private float collectDuration = 1f;
    [SerializeField] private float searchRetryInterval = 1f;
    [SerializeField] private AudioClip collectSfx;

    // ── 런타임 ────────────────────────────────────────────────
    private MetalExchangeZone exchangeZone;
    private NpcState currentState;
    private ItemPickup targetMetal;
    private float collectTimer;
    private float searchRetryTimer;

    public NpcState CurrentState => currentState;

    // HiringZone 스폰 직후 호출
    public void Initialize(MetalExchangeZone zone)
    {
        exchangeZone = zone;
        SetState(NpcState.SearchingMetal);
    }

    // ── Update ────────────────────────────────────────────────
    private void Update()
    {
        switch (currentState)
        {
            case NpcState.SearchingMetal: TickSearching();     break;
            case NpcState.MovingToMetal:  TickMovingToMetal(); break;
            case NpcState.Collecting:     TickCollecting();    break;
        }
    }

    private void SetState(NpcState newState)
    {
        currentState = newState;
        if (newState == NpcState.Collecting)     collectTimer     = 0f;
        if (newState == NpcState.SearchingMetal) searchRetryTimer = 0f;
        Debug.Log($"[NPC:{name}] → {newState}");
    }

    // ── SearchingMetal ────────────────────────────────────────
    private void TickSearching()
    {
        searchRetryTimer -= Time.deltaTime;
        if (searchRetryTimer > 0f) return;

        ItemPickup nearest = FindNearestAvailableMetal();
        if (nearest != null && nearest.TryReserveForNpc(this))
        {
            targetMetal = nearest;
            SetState(NpcState.MovingToMetal);
        }
        else
        {
            searchRetryTimer = searchRetryInterval;
        }
    }

    // ── MovingToMetal ─────────────────────────────────────────
    private void TickMovingToMetal()
    {
        if (targetMetal == null || !targetMetal.gameObject.activeSelf)
        {
            targetMetal = null;
            SetState(NpcState.SearchingMetal);
            return;
        }

        Vector3 dest = targetMetal.transform.position;
        MoveTo(dest);

        if (SqrDist2D(transform.position, dest) <= arrivalThreshold * arrivalThreshold)
            SetState(NpcState.Collecting);
    }

    // ── Collecting ────────────────────────────────────────────
    private void TickCollecting()
    {
        if (targetMetal == null || !targetMetal.gameObject.activeSelf)
        {
            // 플레이어가 수집 도중 가져간 경우
            targetMetal = null;
            SetState(NpcState.SearchingMetal);
            return;
        }

        if (collectTimer == 0f)
            SoundManager.Instance?.PlaySound(collectSfx, 0.3f);

        collectTimer += Time.deltaTime;
        if (collectTimer >= collectDuration)
            CompleteCollection();
    }

    private void CompleteCollection()
    {
        if (targetMetal != null)
        {
            Vector3 metalWorldPos = targetMetal.transform.position;

            // Metal 비활성화 (기존 풀링 규칙 유지 — Destroy 하지 않음)
            targetMetal.CollectByNpc();
            targetMetal = null;

            // 비활성화된 위치에서 sellPos 방향으로 Metal 포물선 발사
            if (exchangeZone != null)
                exchangeZone.LaunchMetalFrom(metalWorldPos);
        }

        SetState(NpcState.SearchingMetal);
    }

    // ── 플레이어가 예약 Metal을 가로챈 경우 ItemPickup이 호출 ─
    public void OnTargetPickedUpByPlayer()
    {
        targetMetal = null;
        if (currentState == NpcState.MovingToMetal || currentState == NpcState.Collecting)
            SetState(NpcState.SearchingMetal);
    }

    // ── 유틸 ─────────────────────────────────────────────────
    private ItemPickup FindNearestAvailableMetal()
    {
        ItemPickup[] all = FindObjectsOfType<ItemPickup>();
        ItemPickup nearest = null;
        float nearestSqr = float.MaxValue;

        foreach (ItemPickup pickup in all)
        {
            if (!pickup.gameObject.activeSelf) continue;
            if (pickup.ItemType != ItemType.Metal) continue;
            if (!pickup.IsPickupEnabled) continue;   // sellPos·비행 중인 Metal 제외
            if (pickup.IsReservedByNpc) continue;    // 다른 NPC 예약 Metal 제외
            if (pickup.GetComponentInParent<PlayerAgent>() != null) continue; // 플레이어가 들고있는 Metal 제외

            float sqr = SqrDist2D(transform.position, pickup.transform.position);
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = pickup;
            }
        }

        return nearest;
    }

    private void MoveTo(Vector3 destination)
    {
        Vector3 pos = transform.position;
        destination.y = pos.y; // 지면 기준 Y 고정

        Vector3 dir = destination - pos;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.position = Vector3.MoveTowards(pos, destination, moveSpeed * Time.deltaTime);

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

    private static float SqrDist2D(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    private void OnDisable()
    {
        if (targetMetal != null)
        {
            targetMetal.ReleaseNpcReservation();
            targetMetal = null;
        }
    }
}
