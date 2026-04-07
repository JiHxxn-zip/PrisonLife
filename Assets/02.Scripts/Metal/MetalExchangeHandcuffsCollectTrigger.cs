using System;
using UnityEngine;

// ExchangeZone의 Handcuffs 수집(이동) 트리거용 핸들러
[RequireComponent(typeof(Collider))]
public class MetalExchangeHandcuffsCollectTrigger : MonoBehaviour
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
        exchangeZone.CollectAllProducedHandcuffs(player);
    }
}

