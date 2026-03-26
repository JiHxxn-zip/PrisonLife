using UnityEngine;

// 플레이어가 진입하면 보유 중인 Handcuffs 전체를 HandcuffsMoneyExchangeZone에 전달
// exchangeZone은 HandcuffsMoneyExchangeZone.Awake()에서 자동 주입됨
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class HandcuffZone : MonoBehaviour
{
    private HandcuffsMoneyExchangeZone exchangeZone;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    // HandcuffsMoneyExchangeZone.Awake()에서 호출
    public void Initialize(HandcuffsMoneyExchangeZone zone)
    {
        exchangeZone = zone;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (exchangeZone == null) return;

        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        exchangeZone.ReceiveHandcuffsFromPlayer(player);
    }
}
