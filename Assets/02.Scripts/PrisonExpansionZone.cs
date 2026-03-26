using UnityEngine;

// 감옥이 가득 찼을 때 PrisonZone이 활성화하는 결제 존.
// expandCost원을 납부하면 PrisonZone의 최대 수용량을 늘리고 자신은 비활성화.
[DisallowMultipleComponent]
public class PrisonExpansionZone : AccumulatedPaymentZone
{
    [Header("확장 설정")]
    [SerializeField] private int expandCost       = 50;
    [SerializeField] private int capacityIncrease = 20;

    [Header("연결")]
    [SerializeField] private PrisonZone prisonZone;

    [Header("맵 전환")]
    [Tooltip("결제 완료 시 비활성화할 GameObject (기존 맵)")]
    [SerializeField] private GameObject mapObjectToDisable;
    [Tooltip("결제 완료 시 활성화할 GameObject (새 맵)")]
    [SerializeField] private GameObject mapObjectToEnable;

    protected override int CurrentTarget => expandCost;

    protected override void OnPaymentComplete(PlayerAgent player, ItemStackInventory inventory)
    {
        prisonZone?.ExpandCapacity(capacityIncrease);
        prisonZone?.DeactivateExpansionZone();

        if (mapObjectToDisable != null) mapObjectToDisable.SetActive(false);
        if (mapObjectToEnable  != null) mapObjectToEnable.SetActive(true);

        Debug.Log($"[PrisonExpansionZone] 결제 완료 — 수용량 +{capacityIncrease}, 맵 전환");
    }
}
