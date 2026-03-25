using System.Collections.Generic;
using UnityEngine;

// Metal 프리팹 하나로 7x18 그리드 풀 생성/배치하고, 충돌 시 비활성화 방식으로 관리
public class CollectionZonePool : MonoBehaviour
{
    [Header("Pool Source")]
    [SerializeField] private GameObject metalPrefab;
    [SerializeField] private ItemType pooledItemType = ItemType.Metal;
    [SerializeField] private int pickupCountPerItem = 1;

    [Header("Grid Layout")]
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 18;
    [SerializeField] private float spacingMultiplierX = 1.1f;
    [SerializeField] private float spacingMultiplierZ = 1.1f;
    [SerializeField] private Vector3 gridOriginLocalOffset = Vector3.zero;
    [SerializeField] private bool activateAllOnStart = true;

    [Header("Respawn")]
    [SerializeField] private float respawnDelaySeconds = 3f;

    private readonly List<GameObject> pooledItems = new List<GameObject>();
    private Vector3 itemFootprint = new Vector3(1f, 1f, 1f);

    private void Start()
    {
        if (metalPrefab == null)
        {
            Debug.LogWarning("[CollectionZonePool] metalPrefab이 비어 있습니다.");
            return;
        }

        BuildPool();
        if (activateAllOnStart)
        {
            ActivateGridItems();
        }
    }

    // 런타임 시작 시 grid 크기만큼 미리 생성하고 비활성화 상태로 풀에 적재
    private void BuildPool()
    {
        ClearPool();

        itemFootprint = CalculatePrefabSize(metalPrefab);
        int count = Mathf.Max(1, columns) * Mathf.Max(1, rows);

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(metalPrefab, transform);
            instance.SetActive(false);

            // 풀 아이템은 충돌 시 Destroy 대신 SetActive(false)로 회수
            ItemPickup pickup = instance.GetComponent<ItemPickup>();
            if (pickup == null)
            {
                pickup = instance.AddComponent<ItemPickup>();
            }
            // 수집 시 비활성화 후, 3초 뒤 같은 오브젝트를 재활성화한다.
            pickup.Configure(pooledItemType, pickupCountPerItem, false, this, respawnDelaySeconds);

            pooledItems.Add(instance);
        }
    }

    // 풀에 있는 오브젝트를 7x18(또는 설정값) 격자로 활성화 배치
    public void ActivateGridItems()
    {
        if (pooledItems.Count == 0)
        {
            return;
        }

        float spacingX = Mathf.Max(0.01f, itemFootprint.x * spacingMultiplierX);
        float spacingZ = Mathf.Max(0.01f, itemFootprint.z * spacingMultiplierZ);

        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (index >= pooledItems.Count)
                {
                    return;
                }

                GameObject item = pooledItems[index];
                Vector3 localPos = gridOriginLocalOffset + new Vector3(c * spacingX, 0f, r * spacingZ);

                item.transform.localPosition = localPos;
                item.transform.localRotation = Quaternion.identity;
                item.SetActive(true);
                index++;
            }
        }
    }

    // 모든 아이템을 풀로 되돌려(비활성화) 존을 비우기
    public void DeactivateAllItems()
    {
        for (int i = 0; i < pooledItems.Count; i++)
        {
            if (pooledItems[i] != null)
            {
                pooledItems[i].SetActive(false);
            }
        }
    }

    private void ClearPool()
    {
        for (int i = pooledItems.Count - 1; i >= 0; i--)
        {
            if (pooledItems[i] != null)
            {
                Destroy(pooledItems[i]);
            }
        }
        pooledItems.Clear();
    }

    // 프리팹 Renderer bounds를 합쳐 그리드 간격 기준 크기 계산
    private static Vector3 CalculatePrefabSize(GameObject prefab)
    {
        GameObject temp = Instantiate(prefab);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;
        temp.SetActive(true);

        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>(true);
        Vector3 size = Vector3.one;
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            size = bounds.size;
        }

        Destroy(temp);

        size.x = Mathf.Max(0.05f, size.x);
        size.y = Mathf.Max(0.05f, size.y);
        size.z = Mathf.Max(0.05f, size.z);
        return size;
    }
}

