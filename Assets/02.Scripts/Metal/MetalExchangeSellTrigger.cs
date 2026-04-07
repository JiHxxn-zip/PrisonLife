using System;
using UnityEngine;

// ExchangeZone의 SellTrigger용 핸들러
[RequireComponent(typeof(Collider))]
public class MetalExchangeSellTrigger : MonoBehaviour
{
    private MetalExchangeZone exchangeZone;

    public event Action<PlayerAgent> OnPlayerEntered;

    public void Initialize(MetalExchangeZone zone)
    {
        exchangeZone = zone;
    }

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

        OnPlayerEntered?.Invoke(player);
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

