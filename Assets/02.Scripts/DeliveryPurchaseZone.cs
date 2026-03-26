using UnityEngine;

// 누적 결제로 씬에 미리 배치된 NpcDeliveryAgent를 활성화하는 Zone
[DisallowMultipleComponent]
public class DeliveryPurchaseZone : AccumulatedPaymentZone
{
    [Header("Purchase")]
    [SerializeField] private int hireCost = 50;

    [Header("NpcDeliveryAgent (씬 내 비활성 오브젝트 연결)")]
    [Tooltip("Hierarchy에 미리 배치된 비활성 NpcDeliveryAgent 오브젝트를 연결")]
    [SerializeField] private NpcDeliveryAgent deliveryAgent;

    protected override int CurrentTarget => hireCost;

    protected override void OnAwake()
    {
        // NpcDeliveryAgent는 결제 완료 전까지 반드시 비활성
        if (deliveryAgent != null)
            deliveryAgent.gameObject.SetActive(false);
    }

    protected override void OnPaymentComplete(PlayerAgent player, ItemStackInventory inventory)
    {
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
