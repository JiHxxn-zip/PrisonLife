using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 플레이어의 등(뒤)에 아이템 프리팹을 위로 차곡차곡 쌓는 인벤토리/스태커
public class ItemStackInventory : MonoBehaviour
{
    [Serializable]
    public class ItemPrefabEntry
    {
        public ItemType itemType;
        public GameObject prefab;
    }

    [Header("Stack Layout")]
    [SerializeField] private Transform backAnchor;
    [SerializeField] private Vector3 stackLocalOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private float stackSpacingY = 0.18f;
    [SerializeField] private int maxStackCount = 10;

    [Header("Prefabs")]
    [SerializeField] private List<ItemPrefabEntry> itemPrefabs = new List<ItemPrefabEntry>();

    [Header("UI")]
    [Tooltip("최대치 도달 시 활성화할 World Space UI(TMP Text)")]
    [SerializeField] private TMP_Text maxTextWorld;

    private readonly Dictionary<ItemType, GameObject> prefabByType = new Dictionary<ItemType, GameObject>();
    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    private bool initialized;

    public int CurrentCount => spawnedItems.Count;
    public bool IsFull => CurrentCount >= maxStackCount;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (backAnchor == null)
        {
            backAnchor = transform;
        }

        prefabByType.Clear();
        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            ItemPrefabEntry entry = itemPrefabs[i];
            if (entry == null || entry.prefab == null)
            {
                continue;
            }

            prefabByType[entry.itemType] = entry.prefab;
        }

        // 씬에 이미 남아있는 오브젝트가 있다면 제거(초기화를 깔끔히)
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
            {
                Destroy(spawnedItems[i]);
            }
        }
        spawnedItems.Clear();

        UpdateMaxText();
    }

    public bool TryAddItem(ItemType itemType)
    {
        if (IsFull)
        {
            Debug.Log($"[ItemStack] MAX — 더 이상 획득 불가 ({CurrentCount}/{maxStackCount})");
            UpdateMaxText();
            return false;
        }

        if (!prefabByType.TryGetValue(itemType, out GameObject prefab) || prefab == null)
        {
            Debug.LogWarning($"[ItemStack] Prefab 미설정: {itemType}");
            return false;
        }

        int index = CurrentCount; // 새로 추가될 자리(0부터)

        GameObject instance = Instantiate(prefab, backAnchor);
        instance.transform.localPosition = stackLocalOffset + new Vector3(0f, stackSpacingY * index, 0f);
        instance.transform.localRotation = Quaternion.identity;

        spawnedItems.Add(instance);

        Debug.Log($"[ItemStack] 획득 +1 {itemType} ({CurrentCount}/{maxStackCount})");

        UpdateMaxText();
        return true;
    }

    // 추후 "판매/버리기" 같은 기능을 붙이기 위해 비우는 메서드도 제공
    public void ClearAll()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
            {
                Destroy(spawnedItems[i]);
            }
        }
        spawnedItems.Clear();
        UpdateMaxText();
    }

    private void UpdateMaxText()
    {
        if (maxTextWorld == null)
        {
            return;
        }

        maxTextWorld.text = "MAX";
        maxTextWorld.gameObject.SetActive(IsFull);
    }
}

