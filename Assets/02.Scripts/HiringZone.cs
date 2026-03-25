using UnityEngine;

// 플레이어가 50원을 1회 결제하면 NPC 3명을 즉시 일괄 스폰하고 Zone 비활성화
[RequireComponent(typeof(Collider))]
public class HiringZone : MonoBehaviour
{
    [Header("Hire Settings")]
    [SerializeField] private int hireCost = 50;
    [SerializeField] private int spawnCount = 3;

    [Header("NPC")]
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private MetalExchangeZone targetExchangeZone;

    [Header("Spawn Points (선택 — 미지정 시 Zone 위치 사용)")]
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        ItemStackInventory inventory = player.GetComponentInChildren<ItemStackInventory>();
        if (inventory == null) return;

        if (inventory.MoneyTotalValue < hireCost)
        {
            Debug.Log($"[HiringZone] 잔액 부족 ({inventory.MoneyTotalValue}/{hireCost}원)");
            return;
        }

        if (!inventory.TryConsumeMoneyValue(hireCost)) return;

        for (int i = 0; i < spawnCount; i++)
            SpawnNpc(i);

        Debug.Log($"[HiringZone] NPC {spawnCount}명 일괄 스폰 완료 → Zone 비활성화");
        gameObject.SetActive(false);
    }

    private void SpawnNpc(int index)
    {
        if (npcPrefab == null)
        {
            Debug.LogWarning("[HiringZone] npcPrefab 미설정");
            return;
        }

        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        if (spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null)
        {
            spawnPos = spawnPoints[index].position;
            spawnRot = spawnPoints[index].rotation;
        }

        GameObject npcObj = Instantiate(npcPrefab, spawnPos, spawnRot);
        NpcCollectorAgent agent = npcObj.GetComponent<NpcCollectorAgent>();

        if (agent != null)
            agent.Initialize(targetExchangeZone);
        else
            Debug.LogWarning("[HiringZone] NPC 프리팹에 NpcCollectorAgent 컴포넌트가 없습니다.");
    }
}
