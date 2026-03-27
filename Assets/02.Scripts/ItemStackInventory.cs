using System;
using System.Collections;
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
    [Tooltip("슬롯/스택 기준점(0번 인덱스가 기준). 아이템은 활성화 시 이 앵커의 Child로 들어간다.")]
    [SerializeField] private List<Transform> backAnchors = new List<Transform>();
    [SerializeField] private Vector3 stackLocalOffset = Vector3.zero;
    [SerializeField] private float stackSpacingY = 0.18f;

    [Header("Max Metal (by Level)")]
    [SerializeField] private int maxMetalCountAtLevel1 = 10;
    [SerializeField] private int maxMetalCountPerLevel = 2;

    [Header("Prefabs")]
    [SerializeField] private List<ItemPrefabEntry> itemPrefabs = new List<ItemPrefabEntry>();

    [Header("UI")]
    [Tooltip("Metal이 최대치에 도달하면 MAX 표시를 띄운다.")]
    [SerializeField] private TMP_Text maxTextWorld;

    [SerializeField] private float maxTextFadeDuration = 0.5f;
    [SerializeField] private float maxTextMoveDistance = 0.3f;
    [SerializeField] private float maxTextCooldownSeconds = 2f;
    [SerializeField] private float maxTextCooldownWhenColliding = 1f;

    [Header("Values (UI 연동용)")]
    [SerializeField] private int moneyValuePerItem = 10;
    [SerializeField] private TMP_Text moneyValueUI;

    private readonly Dictionary<ItemType, GameObject> prefabByType = new Dictionary<ItemType, GameObject>();
    private readonly List<GameObject> metalVisuals = new List<GameObject>();
    private readonly List<GameObject> moneyVisuals = new List<GameObject>();
    private bool initialized;

    private Transform anchor0;
    private PlayerAgent playerAgent;

    private float nextMaxTriggerTime;
    private Coroutine maxTextCoroutine;
    private Coroutine moneyUICoroutine;
    private int moneyUIDisplayedValue;

    // LevelUpZone 등 외부에서 Metal Max를 조정할 때 사용
    private int metalMaxBonus;
    private float metalMaxMultiplier = 1f;

    public int MetalCount => CountActive(metalVisuals);
    public int MoneyCount => CountActive(moneyVisuals);
    public int TotalCount => MetalCount + MoneyCount;
    public int MoneyValuePerItem => moneyValuePerItem;
    public int MoneyTotalValue => MoneyCount * moneyValuePerItem;

    // Metal Max에 고정 보너스를 더함 (1단계 업그레이드)
    public void AddMetalMaxBonus(int bonus)
    {
        metalMaxBonus += bonus;
    }

    // Metal Max에 배율을 적용 (2단계 업그레이드)
    public void SetMetalMaxMultiplier(float multiplier)
    {
        metalMaxMultiplier = Mathf.Max(1f, multiplier);
    }

    // 지정 금액만큼 Money 아이템을 소모. 금액이 부족하면 false 반환
    public bool TryConsumeMoneyValue(int value)
    {
        int itemsNeeded = Mathf.CeilToInt((float)value / Mathf.Max(1, moneyValuePerItem));
        if (MoneyCount < itemsNeeded) return false;

        int consumed = 0;
        for (int i = moneyVisuals.Count - 1; i >= 0 && consumed < itemsNeeded; i--)
        {
            if (moneyVisuals[i] != null && moneyVisuals[i].activeSelf)
            {
                moneyVisuals[i].SetActive(false);
                consumed++;
            }
        }

        RebuildAllTransforms();
        RefreshMoneyUI();
        return consumed >= itemsNeeded;
    }

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

        playerAgent = GetComponentInParent<PlayerAgent>();
        anchor0 = (backAnchors != null && backAnchors.Count > 0 && backAnchors[0] != null) ? backAnchors[0] : transform;

        if (maxTextWorld != null)
        {
            maxTextWorld.gameObject.SetActive(false);
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

        BuildInitialPoolsFromBackAnchors();
        ClearAll();
    }

    private void BuildInitialPoolsFromBackAnchors()
    {
        int initialPoolSize = backAnchors != null ? backAnchors.Count : 0;
        if (initialPoolSize <= 0)
        {
            return;
        }

        prefabByType.TryGetValue(ItemType.Metal, out GameObject metalPrefab);
        prefabByType.TryGetValue(ItemType.Money, out GameObject moneyPrefab);

        // Metal 풀
        if (metalPrefab != null)
        {
            while (metalVisuals.Count < initialPoolSize)
            {
                GameObject instance = Instantiate(metalPrefab, anchor0);
                instance.SetActive(false);
                instance.transform.localRotation = Quaternion.identity;
                metalVisuals.Add(instance);
            }
        }

        // Money 풀
        if (moneyPrefab != null)
        {
            while (moneyVisuals.Count < initialPoolSize)
            {
                GameObject instance = Instantiate(moneyPrefab, anchor0);
                instance.SetActive(false);
                instance.transform.localRotation = Quaternion.identity;
                moneyVisuals.Add(instance);
            }
        }
    }

    public bool TryAddItem(ItemType itemType)
    {
        if (itemType == ItemType.Metal)
        {
            int maxMetalCount = GetCurrentMaxMetalCount();
            if (MetalCount >= maxMetalCount)
            {
                Debug.Log($"[ItemStack] Metal MAX — 더 이상 획득 불가 ({MetalCount}/{maxMetalCount})");
                TriggerMaxTextIfAllowed(maxTextCooldownWhenColliding);
                return false;
            }

            if (!prefabByType.TryGetValue(ItemType.Metal, out GameObject prefab) || prefab == null)
            {
                Debug.LogWarning("[ItemStack] Metal Prefab 미설정");
                return false;
            }

            GameObject instance = GetOrCreateInactiveVisual(ItemType.Metal, prefab, metalVisuals);
            instance.SetActive(true);

            RebuildAllTransforms();

            // 방금 획득한 Metal로 최대치가 됐을 때 MAX 표시를 띄운다.
            if (MetalCount >= maxMetalCount)
            {
                TriggerMaxTextIfAllowed(maxTextCooldownSeconds);
            }

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

            GameObject instance = GetOrCreateInactiveVisual(ItemType.Money, prefab, moneyVisuals);
            instance.SetActive(true);

            RebuildAllTransforms();
            RefreshMoneyUI();
            Debug.Log($"[ItemStack] 획득 +1 Money ({MoneyCount} 보유)");
            return true;
        }

        // Handcuffs 등 다른 타입은 등 뒤 인벤토리 시스템에서 다루지 않음
        Debug.LogWarning($"[ItemStack] 등 뒤 스택은 {itemType}을 지원하지 않습니다.");
        return false;
    }

    // 활성화된 Metal 비주얼을 풀에서 분리해 반환한다 (Zone으로 이전 용도)
    public List<GameObject> ReleaseActiveMetal()
    {
        List<GameObject> released = new List<GameObject>();
        for (int i = 0; i < metalVisuals.Count; i++)
        {
            if (metalVisuals[i] != null && metalVisuals[i].activeSelf)
            {
                released.Add(metalVisuals[i]);
            }
        }

        foreach (GameObject v in released)
        {
            metalVisuals.Remove(v);
        }

        // Metal이 사라졌으니 Money 위치 재정렬
        RebuildAllTransforms();
        return released;
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

        if (MetalCount <= 0)
        {
            return false;
        }

        // 비활성화 객체는 리스트에 남기고, 가장 마지막에 활성화된 Metal만 비활성화한다.
        for (int i = metalVisuals.Count - 1; i >= 0; i--)
        {
            GameObject visual = metalVisuals[i];
            if (visual != null && visual.activeSelf)
            {
                visual.SetActive(false);
                RebuildAllTransforms();
                Debug.Log($"[ItemStack] 제거 -1 Metal ({MetalCount}/{GetCurrentMaxMetalCount()})");
                return true;
            }
        }

        return false;
    }

    private void RefreshMoneyUI()
    {
        if (moneyValueUI == null) return;

        int targetValue = MoneyTotalValue;

        if (moneyUICoroutine != null)
        {
            StopCoroutine(moneyUICoroutine);
        }

        moneyUICoroutine = StartCoroutine(AnimateMoneyUI(moneyUIDisplayedValue, targetValue));
    }

    private IEnumerator AnimateMoneyUI(int from, int to)
    {
        float duration = Mathf.Clamp(Mathf.Abs(to - from) * 0.015f, 0.15f, 0.6f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            int displayed = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            moneyUIDisplayedValue = displayed;
            moneyValueUI.text = displayed.ToString();
            yield return null;
        }

        moneyUIDisplayedValue = to;
        moneyValueUI.text = to.ToString();
        moneyUICoroutine = null;
    }

    // 등 뒤 시각 오브젝트 전부 제거
    public void ClearAll()
    {
        // 풀 오브젝트는 유지하고 비활성화만 수행한다.
        for (int i = 0; i < metalVisuals.Count; i++)
        {
            if (metalVisuals[i] != null)
            {
                metalVisuals[i].SetActive(false);
            }
        }

        for (int i = 0; i < moneyVisuals.Count; i++)
        {
            if (moneyVisuals[i] != null)
            {
                moneyVisuals[i].SetActive(false);
            }
        }

        if (maxTextCoroutine != null)
        {
            StopCoroutine(maxTextCoroutine);
            maxTextCoroutine = null;
        }

        if (maxTextWorld != null) maxTextWorld.gameObject.SetActive(false);
        nextMaxTriggerTime = 0f;

        if (moneyUICoroutine != null)
        {
            StopCoroutine(moneyUICoroutine);
            moneyUICoroutine = null;
        }
        moneyUIDisplayedValue = 0;
        if (moneyValueUI != null) moneyValueUI.text = "0";
    }

    // Metal은 항상 backAnchors[0] 기준으로 slot 0부터 쌓임
    // Money는 Metal이 있으면 backAnchors[1] 기준, 없으면 backAnchors[0] 기준으로 slot 0부터 쌓임
    private void RebuildAllTransforms()
    {
        Transform metalAnchor = GetAnchor(0);

        int metalSlot = 0;
        for (int i = 0; i < metalVisuals.Count; i++)
        {
            GameObject v = metalVisuals[i];
            if (v != null && v.activeSelf)
            {
                SetLocalForSlot(v, metalAnchor, metalSlot++);
            }
        }

        // Metal 유무에 따라 Money 앵커 실시간 결정
        Transform moneyAnchor = (MetalCount > 0) ? GetAnchor(1) : GetAnchor(0);

        int moneySlot = 0;
        for (int j = 0; j < moneyVisuals.Count; j++)
        {
            GameObject v = moneyVisuals[j];
            if (v != null && v.activeSelf)
            {
                SetLocalForSlot(v, moneyAnchor, moneySlot++);
            }
        }
    }

    private void SetLocalForSlot(GameObject visual, Transform anchor, int slotIndex)
    {
        if (visual == null)
        {
            return;
        }

        if (anchor != null)
        {
            visual.transform.SetParent(anchor, false);
        }

        Vector3 localPos = stackLocalOffset;
        // 로컬 X/Z는 항상 0, Y만 slotIndex에 따라 누적
        localPos.x = 0f;
        localPos.z = 0f;
        localPos.y = stackLocalOffset.y + stackSpacingY * slotIndex;

        visual.transform.localPosition = localPos;
        visual.transform.localRotation = Quaternion.identity;
    }

    private Transform GetAnchor(int index)
    {
        if (backAnchors != null && backAnchors.Count > index && backAnchors[index] != null)
        {
            return backAnchors[index];
        }

        return anchor0;
    }

    private GameObject GetOrCreateInactiveVisual(ItemType itemType, GameObject prefab, List<GameObject> pool)
    {
        // 풀 내 비활성화 객체 재활용
        for (int i = 0; i < pool.Count; i++)
        {
            GameObject v = pool[i];
            if (v != null && !v.activeSelf)
            {
                if (anchor0 != null)
                {
                    v.transform.SetParent(anchor0, false);
                    v.transform.localRotation = Quaternion.identity;
                }
                return v;
            }
        }

        // 모든 객체 사용 중이면 새로 생성
        if (anchor0 == null)
        {
            anchor0 = transform;
        }

        GameObject instance = Instantiate(prefab, anchor0);
        instance.name = $"{itemType}_Visual_{pool.Count}";
        pool.Add(instance);
        return instance;
    }

    private int CountActive(List<GameObject> pool)
    {
        int count = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null && pool[i].activeSelf)
            {
                count++;
            }
        }
        return count;
    }

    private int GetCurrentMaxMetalCount()
    {
        int level = playerAgent != null ? playerAgent.Level : 1;
        int safeLevel = Mathf.Max(1, level);
        int baseMax = Mathf.Max(0, maxMetalCountAtLevel1 + (safeLevel - 1) * Mathf.Max(0, maxMetalCountPerLevel));
        return Mathf.Max(0, Mathf.RoundToInt((baseMax + metalMaxBonus) * metalMaxMultiplier));
    }

    private void TriggerMaxTextIfAllowed(float cooldown)
    {
        if (maxTextWorld == null)
        {
            return;
        }

        // 쿨타임 중이면 무시
        if (Time.time < nextMaxTriggerTime)
        {
            return;
        }

        // 표시 중이면 중복 코루틴 방지
        if (maxTextCoroutine != null)
        {
            return;
        }

        maxTextWorld.text = "MAX";
        maxTextWorld.alpha = 0f;
        maxTextWorld.gameObject.SetActive(true);
        maxTextCoroutine = StartCoroutine(MaxTextAnimation(cooldown));
    }

    private IEnumerator MaxTextAnimation(float cooldown)
    {
        float fadeDuration = Mathf.Max(0.01f, maxTextFadeDuration);
        Vector3 originLocalPos = maxTextWorld.transform.localPosition;

        // Phase 1: 페이드 인 (0.5초)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            maxTextWorld.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        maxTextWorld.alpha = 1f;

        // Phase 2: 위로 올라가며 페이드 아웃 (0.5초)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            maxTextWorld.alpha = 1f - t;
            maxTextWorld.transform.localPosition = originLocalPos + Vector3.up * (maxTextMoveDistance * t);
            yield return null;
        }

        maxTextWorld.alpha = 0f;
        maxTextWorld.transform.localPosition = originLocalPos;
        maxTextWorld.gameObject.SetActive(false);

        nextMaxTriggerTime = Time.time + Mathf.Max(0f, cooldown);
        maxTextCoroutine = null;
    }
}

