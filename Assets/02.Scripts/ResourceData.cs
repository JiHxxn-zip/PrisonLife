using System;
using UnityEngine;

[Serializable]
public class ResourceData
{
    [SerializeField] private ResourceType resourceType = ResourceType.Wood;
    [SerializeField] private int level = 1;
    [SerializeField] private int maxCapacity = 20;
    [SerializeField] private int gatherAmountPerTick = 1;
    [SerializeField] private float gatherIntervalSeconds = 0.35f;

    public ResourceType ResourceType => resourceType;
    public int Level => level;
    public int MaxCapacity => Mathf.Max(1, maxCapacity);
    public int GatherAmountPerTick => Mathf.Max(1, gatherAmountPerTick);
    public float GatherIntervalSeconds => Mathf.Max(0.05f, gatherIntervalSeconds);

    public void UpgradeCapacity(int bonusCapacity)
    {
        level++;
        maxCapacity = Mathf.Max(1, maxCapacity + Mathf.Max(1, bonusCapacity));
    }

    public void UpgradeGatherSpeed(float bonusPerSecond)
    {
        level++;
        float newInterval = gatherIntervalSeconds - Mathf.Max(0.01f, bonusPerSecond);
        gatherIntervalSeconds = Mathf.Max(0.05f, newInterval);
    }
}
