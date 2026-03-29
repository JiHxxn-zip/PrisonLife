using UnityEngine;
using System.Collections;

// World에 있는 아이템 프리팹(줍는 오브젝트) 스크립트: 플레이어와 충돌 시 PlayerAgent로 아이템 획득 요청
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemType itemType = ItemType.Metal;
    [SerializeField] private int count = 1;
    [SerializeField] private bool destroyOnPickup = false;

    // 수집된 뒤 특정 지연 후 다시 활성화(예: Collection Zone 리젠)할 때 사용
    private MonoBehaviour respawnHost;
    private float respawnDelaySeconds = 3f;

    // false면 플레이어와의 트리거 상호작용을 무시 (Zone 이전 시 사용)
    private bool pickupEnabled = true;
    // NPC 예약 상태 (예약 중이면 다른 NPC가 접근 불가, 플레이어는 여전히 가능)
    private bool reservedByNpc;
    private NpcCollectorAgent reservingAgent;

    public ItemType ItemType => itemType;
    public bool IsPickupEnabled => pickupEnabled;
    public bool IsReservedByNpc => reservedByNpc;

    public void SetPickupEnabled(bool enabled)
    {
        pickupEnabled = enabled;
    }

    // NPC가 이 Metal을 예약. 이미 다른 NPC가 예약 중이면 false
    public bool TryReserveForNpc(NpcCollectorAgent agent)
    {
        if (reservedByNpc && reservingAgent != agent) return false;
        reservedByNpc = true;
        reservingAgent = agent;
        return true;
    }

    public void ReleaseNpcReservation()
    {
        reservedByNpc = false;
        reservingAgent = null;
    }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!pickupEnabled)
        {
            return;
        }

        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null)
        {
            return;
        }

        // 플레이어가 NPC 예약 Metal을 가져가는 경우 NPC에게 알림
        if (reservedByNpc && reservingAgent != null)
        {
            reservingAgent.OnTargetPickedUpByPlayer();
            ReleaseNpcReservation();
        }

        bool added = player.CollectItem(itemType, count);
        if (!added)
        {
            return; // MAX 등으로 못 넣으면 pickup은 유지
        }

        if (itemType == ItemType.Money)
            UIManager.Instance?.AddMoney(count * 100);

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            if (respawnHost != null)
            {
                // respawnHost에서 코루틴을 돌려서, 이 오브젝트가 비활성화되어도 재활성화를 보장한다.
                gameObject.SetActive(false);
                respawnHost.StartCoroutine(RespawnAfterDelay());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    // NPC가 직접 수집할 때 호출 — 플레이어 인벤토리에 추가하지 않고 풀링 규칙만 따름
    public void CollectByNpc()
    {
        ReleaseNpcReservation();
        SetPickupEnabled(true); // 다음 스폰 시 다시 줍기 가능하도록 복원

        if (respawnHost != null)
        {
            gameObject.SetActive(false);
            respawnHost.StartCoroutine(RespawnAfterDelay());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 풀에서 생성된 아이템이 타입/획득량/처리방식을 런타임에 갱신할 때 사용
    public void Configure(
        ItemType newType,
        int newCount,
        bool shouldDestroyOnPickup,
        MonoBehaviour newRespawnHost = null,
        float newRespawnDelaySeconds = 3f)
    {
        itemType = newType;
        count = Mathf.Max(1, newCount);
        destroyOnPickup = shouldDestroyOnPickup;
        respawnHost = newRespawnHost;
        respawnDelaySeconds = Mathf.Max(0f, newRespawnDelaySeconds);
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelaySeconds);
        if (gameObject == null) yield break;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }
}

