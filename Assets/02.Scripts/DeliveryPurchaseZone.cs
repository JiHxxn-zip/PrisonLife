using UnityEngine;

// 플레이어가 결제하면 씬에 미리 배치된 NpcDeliveryAgent를 활성화하고 자신은 비활성화
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DeliveryPurchaseZone : MonoBehaviour
{
    [Header("Purchase")]
    [SerializeField] private int hireCost = 50;

    [Header("NpcDeliveryAgent (씬 내 비활성 오브젝트 연결)")]
    [Tooltip("Hierarchy에 미리 배치된 비활성 NpcDeliveryAgent 오브젝트를 연결")]
    [SerializeField] private NpcDeliveryAgent deliveryAgent;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        deliveryAgent.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        ItemStackInventory inventory = player.GetComponentInChildren<ItemStackInventory>();
        if (inventory == null) return;

        if (inventory.MoneyTotalValue < hireCost)
        {
            Debug.Log($"[DeliveryPurchaseZone] 잔액 부족 ({inventory.MoneyTotalValue}/{hireCost}원)");
            return;
        }

        if (!inventory.TryConsumeMoneyValue(hireCost)) return;

        if (deliveryAgent != null)
        {
            deliveryAgent.gameObject.SetActive(true);
            Debug.Log("[DeliveryPurchaseZone] NpcDeliveryAgent 활성화 완료");
        }
        else
        {
            Debug.LogWarning("[DeliveryPurchaseZone] deliveryAgent 미연결 — Inspector에서 씬 오브젝트를 연결하세요.");
        }

        gameObject.SetActive(false);
    }
}
