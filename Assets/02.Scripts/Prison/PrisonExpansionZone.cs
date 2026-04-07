using System.Collections;
using UnityEngine;

// 감옥이 가득 찼을 때 PrisonZone이 활성화하는 결제 존.
// expandCost원을 납부하면 PrisonZone의 최대 수용량을 늘리고 자신은 비활성화.
[DisallowMultipleComponent]
public class PrisonExpansionZone : AccumulatedPaymentZone
{
    [Header("확장 설정")]
    [SerializeField] private int expandCost       = 50;
    [SerializeField] private int capacityIncrease = 20;

    private PrisonZone prisonZone;

    public void Initialize(PrisonZone zone)
    {
        prisonZone = zone;
    }

    [Header("맵 전환")]
    [Tooltip("결제 완료 시 비활성화할 GameObject (기존 맵)")]
    [SerializeField] private GameObject mapObjectToDisable;
    [Tooltip("결제 완료 시 활성화할 GameObject (새 맵)")]
    [SerializeField] private GameObject mapObjectToEnable;

    [Header("카메라 연출")]
    [SerializeField] private float cameraTravelDuration = 1.5f;
    [SerializeField] private float mapShowDuration = 1f;

    protected override int CurrentTarget => expandCost;

    protected override void OnPaymentComplete(PlayerAgent player, ItemStackInventory inventory)
    {
        prisonZone?.ExpandCapacity(capacityIncrease);
        StartCoroutine(MapTransitionCinematic(player));
    }

    private IEnumerator MapTransitionCinematic(PlayerAgent player)
    {
        // 플레이어 이동 잠금
        HyperCasualPlayerController controller = player.GetComponent<HyperCasualPlayerController>();
        controller?.SetMovementLocked(true);

        // 카메라 → 새 맵으로 이동
        if (mapObjectToEnable != null)
            yield return CameraManager.Instance?.StartCinematicLerp(mapObjectToEnable.transform, cameraTravelDuration);

        // 맵 전환
        if (mapObjectToDisable != null) mapObjectToDisable.SetActive(false);
        if (mapObjectToEnable  != null) mapObjectToEnable.SetActive(true);

        // 새 맵 감상
        yield return new WaitForSeconds(mapShowDuration);

        // 카메라 → 플레이어로 복귀
        yield return CameraManager.Instance?.StartCinematicLerp(player.transform, cameraTravelDuration);

        // 챕터 클리어 팝업
        UIManager.Instance?.ShowClear();

        // 플레이어 이동 해제 & 존 비활성화
        controller?.SetMovementLocked(false);
        prisonZone?.DeactivateExpansionZone();

        Debug.Log($"[PrisonExpansionZone] 결제 완료 — 수용량 +{capacityIncrease}, 맵 전환");
    }
}
