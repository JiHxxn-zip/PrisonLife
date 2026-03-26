using UnityEngine;

// 플레이어가 결제하면 NpcCollectorAgent를 스폰하고,
// 완료 즉시 DeliveryPurchaseZone을 활성화한 뒤 자신은 비활성화
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class CollectorPurchaseZone : MonoBehaviour
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

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        // DeliveryPurchaseZone은 처음엔 반드시 비활성
        if (deliveryPurchaseZone != null)
            deliveryPurchaseZone.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        ItemStackInventory inventory = player.GetComponentInChildren<ItemStackInventory>();
        if (inventory == null) return;

        if (inventory.MoneyTotalValue < hireCost)
        {
            Debug.Log($"[CollectorPurchaseZone] 잔액 부족 ({inventory.MoneyTotalValue}/{hireCost}원)");
            return;
        }

        if (!inventory.TryConsumeMoneyValue(hireCost)) return;

        for (int i = 0; i < spawnCount; i++)
            SpawnCollector(i);

        Debug.Log($"[CollectorPurchaseZone] NpcCollectorAgent {spawnCount}명 스폰 완료");

        // 2단계 Zone 해금
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

        Vector3 pos      = transform.position;
        Quaternion rot   = transform.rotation;

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
