using UnityEngine;

// World에 있는 아이템 프리팹(줍는 오브젝트) 스크립트: 플레이어와 충돌 시 PlayerAgent로 아이템 획득 요청
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemType itemType = ItemType.Metal;
    [SerializeField] private int count = 1;
    [SerializeField] private bool destroyOnPickup = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null)
        {
            return;
        }

        bool added = player.CollectItem(itemType, count);
        if (!added)
        {
            return; // MAX 등으로 못 넣으면 pickup은 유지
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 풀에서 생성된 아이템이 타입/획득량/처리방식을 런타임에 갱신할 때 사용
    public void Configure(ItemType newType, int newCount, bool shouldDestroyOnPickup)
    {
        itemType = newType;
        count = Mathf.Max(1, newCount);
        destroyOnPickup = shouldDestroyOnPickup;
    }
}

