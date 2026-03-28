using UnityEngine;

// 누적 결제로 NpcCollectorAgent를 스폰하는 Zone
// 결제 완료 즉시 DeliveryPurchaseZone을 활성화하고 자신은 비활성화
[DisallowMultipleComponent]
public class CollectorPurchaseZone : AccumulatedPaymentZone
{
    [Header("Purchase")]
    [SerializeField] private int hireCost = 50;

    [Header("NPC Spawn")]
    [SerializeField] private int spawnCount = 3;
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private MetalExchangeZone targetExchangeZone;
    [Tooltip("스폰 위치 목록 (미지정 시 Zone 위치 사용)")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("2단계 해금 — 결제 완료 직후 활성화")]
    [SerializeField] private DeliveryPurchaseZone deliveryPurchaseZone;

    protected override int CurrentTarget => hireCost;

    protected override void OnAwake()
    {
        if (deliveryPurchaseZone != null)
            deliveryPurchaseZone.gameObject.SetActive(false);
    }

    protected override void OnPaymentComplete(PlayerAgent player, ItemStackInventory inventory)
    {
        for (int i = 0; i < spawnCount; i++)
            SpawnCollector(i);

        Debug.Log($"[CollectorPurchaseZone] NpcCollectorAgent {spawnCount}명 스폰 완료");

        if (deliveryPurchaseZone != null)
        {
            deliveryPurchaseZone.gameObject.SetActive(true);
            Debug.Log("[CollectorPurchaseZone] DeliveryPurchaseZone 활성화");
        }

        gameObject.SetActive(false);
    }

    private void SpawnCollector(int index)
    {
        if (npcPrefab == null)
        {
            Debug.LogWarning("[CollectorPurchaseZone] npcPrefab 미설정");
            return;
        }

        Vector3 pos    = transform.position;
        Quaternion rot = transform.rotation;

        if (spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null)
        {
            pos = spawnPoints[index].position;
            rot = spawnPoints[index].rotation;
        }

        GameObject npcObj = Instantiate(npcPrefab, pos, rot);
        NpcCollectorAgent agent = npcObj.GetComponent<NpcCollectorAgent>();

        if (agent != null)
            agent.Initialize(targetExchangeZone);
        else
            Debug.LogWarning("[CollectorPurchaseZone] NPC 프리팹에 NpcCollectorAgent가 없습니다.");
    }
}
