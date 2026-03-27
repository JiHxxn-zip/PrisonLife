using UnityEngine;

// 메탈이 스폰되는 구역. 플레이어 진입 시 레벨에 맞는 수집 콜라이더를 활성화하고,
// 퇴장 시 비활성화한다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class MetalCollectionZone : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMetalCollector collector = other.GetComponentInParent<PlayerMetalCollector>();
        collector?.ActivateForCurrentLevel();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMetalCollector collector = other.GetComponentInParent<PlayerMetalCollector>();
        collector?.Deactivate();
    }
}
