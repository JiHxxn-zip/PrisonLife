using UnityEngine;

// ExchangeZone의 SellTrigger용 핸들러
[RequireComponent(typeof(Collider))]
public class MetalExchangeSellTrigger : MonoBehaviour
{
    [SerializeField] private MetalExchangeZone exchangeZone;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (exchangeZone == null)
        {
            return;
        }

        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null)
        {
            return;
        }

        exchangeZone.RequestStartSelling(player);
    }

    private void OnTriggerExit(Collider other)
    {
        if (exchangeZone == null)
        {
            return;
        }

        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null)
        {
            return;
        }

        exchangeZone.RequestStopSelling(player);
    }
}

