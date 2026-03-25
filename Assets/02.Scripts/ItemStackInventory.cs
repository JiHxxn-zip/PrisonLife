using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 플레이어 등 뒤에 Metal(최우선, Max 있음) + Money(무제한)를 Y축으로 적층
// 정렬 규칙: 항상 Metal이 index 0부터 먼저, Money는 Metal 뒤에서 이어서 배치
public class ItemStackInventory : MonoBehaviour
{
    [Serializable]
    public class ItemPrefabEntry
    {
        public ItemType itemType;
        public GameObject prefab;
    }

    [Header("Back Anchor")]
    [SerializeField] private Transform backAnchor;
    [Tooltip("슬롯 위치를 직접 지정하고 싶을 때 사용 (index 순서대로 배치)")]
    [SerializeField] private List<Transform> backAnchors = new List<Transform>();
    [SerializeField] private Vector3 stackLocalOffset = Vector3.zero;
    [SerializeField] private float stackSpacingY = 0.18f;

    [Header("Limits (Metal only)")]
    [SerializeField] private int maxMetalCount = 10;

    [Header("Prefabs")]
    [SerializeField] private List<ItemPrefabEntry> itemPrefabs = new List<ItemPrefabEntry>();

    [Header("UI")]
    [Tooltip("Metal이 최대치에 도달하면 활성화 (World Space TMP Text)")]
    [SerializeField] private TMP_Text maxTextWorld;

    private readonly Dictionary<ItemType, GameObject> prefabByType = new Dictionary<ItemType, GameObject>();
    private readonly List<GameObject> metalVisuals = new List<GameObject>();
    private readonly List<GameObject> moneyVisuals = new List<GameObject>();
    private bool initialized;

    public int MetalCount => metalVisuals.Count;
    public int MoneyCount => moneyVisuals.Count;
    public int TotalCount => metalVisuals.Count + moneyVisuals.Count;
    public bool IsMetalFull => MetalCount >= maxMetalCount;

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

        ClearAll();
    }

    public bool TryAddItem(ItemType itemType)
    {
        if (itemType == ItemType.Metal)
        {
            if (IsMetalFull)
            {
                Debug.Log($"[ItemStack] Metal MAX — 더 이상 획득 불가 ({MetalCount}/{maxMetalCount})");
                UpdateMaxText();
                return false;
            }

            if (!prefabByType.TryGetValue(ItemType.Metal, out GameObject prefab) || prefab == null)
            {
                Debug.LogWarning("[ItemStack] Metal Prefab 미설정");
                return false;
            }

            GameObject instance = Instantiate(prefab, backAnchor);
            metalVisuals.Add(instance);
            RebuildAllTransforms();
            UpdateMaxText();
            Debug.Log($"[ItemStack] 획득 +1 Metal ({MetalCount}/{maxMetalCount})");
            return true;
        }

        if (itemType == ItemType.Money)
        {
            if (!prefabByType.TryGetValue(ItemType.Money, out GameObject prefab) || prefab == null)
            {
                Debug.LogWarning("[ItemStack] Money Prefab 미설정");
                return false;
            }

            GameObject instance = Instantiate(prefab, backAnchor);
            moneyVisuals.Add(instance);
            RebuildAllTransforms();
            // Money은 무제한이므로 MAX UI는 Metal 상태만 반영
            UpdateMaxText();
            Debug.Log($"[ItemStack] 획득 +1 Money ({MoneyCount} 보유)");
            return true;
        }

        // Handcuffs 등 다른 타입은 등 뒤 인벤토리 시스템에서 다루지 않음
        Debug.LogWarning($"[ItemStack] 등 뒤 스택은 {itemType}을 지원하지 않습니다.");
        return false;
    }

    // 특정 타입 현재 개수 조회
    public int GetCountByType(ItemType itemType)
    {
        if (itemType == ItemType.Metal) return MetalCount;
        if (itemType == ItemType.Money) return MoneyCount;
        return 0;
    }

    // Metal을 LIFO로 제거 (Money는 제거 규칙이 별도로 없으므로 여기서는 false)
    public bool TryRemoveLastOfType(ItemType itemType)
    {
        if (itemType != ItemType.Metal)
        {
            return false;
        }

        if (metalVisuals.Count <= 0)
        {
            return false;
        }

        int lastIndex = metalVisuals.Count - 1;
        GameObject visual = metalVisuals[lastIndex];
        metalVisuals.RemoveAt(lastIndex);

        if (visual != null)
        {
            visual.SetActive(false);
            Destroy(visual);
        }

        RebuildAllTransforms();
        UpdateMaxText();
        Debug.Log($"[ItemStack] 제거 -1 Metal ({MetalCount}/{maxMetalCount})");
        return true;
    }

    // 등 뒤 시각 오브젝트 전부 제거
    public void ClearAll()
    {
        for (int i = metalVisuals.Count - 1; i >= 0; i--)
        {
            if (metalVisuals[i] != null)
            {
                Destroy(metalVisuals[i]);
            }
        }
        metalVisuals.Clear();

        for (int i = moneyVisuals.Count - 1; i >= 0; i--)
        {
            if (moneyVisuals[i] != null)
            {
                Destroy(moneyVisuals[i]);
            }
        }
        moneyVisuals.Clear();

        UpdateMaxText();
    }

    // Metal이 항상 index 0부터, Money는 Metal 뒤에서 이어서 배치되도록 재정렬
    private void RebuildAllTransforms()
    {
        // Metal: 0..metalCount-1
        for (int i = 0; i < metalVisuals.Count; i++)
        {
            SetLocalForSlot(metalVisuals[i], i);
        }

        // Money: metalCount..metalCount+moneyCount-1
        int moneyStartIndex = metalVisuals.Count;
        for (int j = 0; j < moneyVisuals.Count; j++)
        {
            SetLocalForSlot(moneyVisuals[j], moneyStartIndex + j);
        }
    }

    private void SetLocalForSlot(GameObject visual, int slotIndex)
    {
        if (visual == null)
        {
            return;
        }

        visual.transform.localPosition = GetLocalSlotPosition(slotIndex);
        visual.transform.localRotation = Quaternion.identity;
    }

    private Vector3 GetLocalSlotPosition(int index)
    {
        if (backAnchors != null && index >= 0 && index < backAnchors.Count && backAnchors[index] != null)
        {
            // backAnchors를 "월드 좌표"로 잡아뒀을 때를 대비
            if (backAnchor != null)
            {
                return backAnchor.InverseTransformPoint(backAnchors[index].position);
            }

            return backAnchors[index].localPosition;
        }

        // 슬롯이 backAnchors를 넘어서면 spacingY 기반으로 계속 위로 적층
        return stackLocalOffset + new Vector3(0f, stackSpacingY * index, 0f);
    }

    private void UpdateMaxText()
    {
        if (maxTextWorld == null)
        {
            return;
        }

        maxTextWorld.text = "MAX";
        maxTextWorld.gameObject.SetActive(IsMetalFull);
    }
}

