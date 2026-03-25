using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory
{
    [SerializeField] private List<ResourceData> resourceConfigs = new List<ResourceData>();

    private readonly Dictionary<ResourceType, int> amounts = new Dictionary<ResourceType, int>();
    private readonly Dictionary<ResourceType, ResourceData> configByType = new Dictionary<ResourceType, ResourceData>();

    public IReadOnlyDictionary<ResourceType, int> Amounts => amounts;

    public void Initialize()
    {
        amounts.Clear();
        configByType.Clear();

        for (int i = 0; i < resourceConfigs.Count; i++)
        {
            ResourceData data = resourceConfigs[i];
            if (data == null)
            {
                continue;
            }

            ResourceType type = data.ResourceType;
            configByType[type] = data;

            if (!amounts.ContainsKey(type))
            {
                amounts[type] = 0;
            }
        }
    }

    public int GetAmount(ResourceType type)
    {
        return amounts.TryGetValue(type, out int value) ? value : 0;
    }

    public int GetCapacity(ResourceType type)
    {
        return configByType.TryGetValue(type, out ResourceData data) ? data.MaxCapacity : 0;
    }

    public bool TryAddResource(ResourceType type, int amount)
    {
        if (amount <= 0 || !configByType.TryGetValue(type, out ResourceData data))
        {
            return false;
        }

        int current = GetAmount(type);
        int clamped = Mathf.Min(data.MaxCapacity, current + amount);
        int delta = clamped - current;

        if (delta <= 0)
        {
            return false;
        }

        amounts[type] = clamped;
        return true;
    }

    public int RemoveResource(ResourceType type, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int current = GetAmount(type);
        int removed = Mathf.Min(current, amount);
        amounts[type] = Mathf.Max(0, current - removed);
        return removed;
    }

    public int RemoveAll(ResourceType type)
    {
        int current = GetAmount(type);
        amounts[type] = 0;
        return current;
    }

    public bool IsResourceFull(ResourceType type)
    {
        if (!configByType.TryGetValue(type, out ResourceData data))
        {
            return false;
        }

        return GetAmount(type) >= data.MaxCapacity;
    }

    public bool IsAnyResourceFull()
    {
        foreach (KeyValuePair<ResourceType, ResourceData> pair in configByType)
        {
            if (GetAmount(pair.Key) >= pair.Value.MaxCapacity)
            {
                return true;
            }
        }

        return false;
    }

    public int GetTotalAmount()
    {
        int total = 0;
        foreach (KeyValuePair<ResourceType, int> pair in amounts)
        {
            total += pair.Value;
        }

        return total;
    }

    public int SellAll(int goldPerItem)
    {
        int soldTotal = 0;
        List<ResourceType> keys = new List<ResourceType>(amounts.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            ResourceType type = keys[i];
            soldTotal += RemoveAll(type);
        }

        return soldTotal * Mathf.Max(0, goldPerItem);
    }

    public bool HasConfig(ResourceType type)
    {
        return configByType.ContainsKey(type);
    }

    public ResourceData GetConfig(ResourceType type)
    {
        return configByType.TryGetValue(type, out ResourceData data) ? data : null;
    }

    public void UpgradeAllCapacities(int bonusCapacity)
    {
        foreach (KeyValuePair<ResourceType, ResourceData> pair in configByType)
        {
            pair.Value.UpgradeCapacity(bonusCapacity);
        }
    }
}
