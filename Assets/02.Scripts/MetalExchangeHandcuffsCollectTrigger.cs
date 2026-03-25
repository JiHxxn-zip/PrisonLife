using UnityEngine;

// ExchangeZone의 Handcuffs 수집(이동) 트리거용 핸들러
[RequireComponent(typeof(Collider))]
public class MetalExchangeHandcuffsCollectTrigger : MonoBehaviour
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

        exchangeZone.CollectAllProducedHandcuffs(player);
    }
}

