using System;
using UnityEngine;

// 몬스터 처치 시 드롭되는 챕터2 전용 Money 픽업.
// 플레이어가 닿으면 Ch2HUD에 이벤트를 보내고 자신을 Destroy한다.
[RequireComponent(typeof(Collider))]
public class Ch2MoneyPickup : MonoBehaviour
{
    public const int MoneyPerPickup = 10;

    public static event Action OnMoneyCollected;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<HyperCasualPlayerController>() == null) return;
        OnMoneyCollected?.Invoke();
        Destroy(gameObject);
    }
}
