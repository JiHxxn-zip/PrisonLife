using System;
using UnityEngine;

[Serializable]
public class PlayerStats
{
    [SerializeField] private float baseMoveSpeed = 4.5f;
    [SerializeField] private float moveSpeedBonus = 0f;
    [SerializeField] private int attackPower = 1;
    [SerializeField] private int gold = 0;

    public float MoveSpeed => Mathf.Max(0.1f, baseMoveSpeed + moveSpeedBonus);
    public int AttackPower => Mathf.Max(1, attackPower);
    public int Gold => gold;

    public void AddGold(int value)
    {
        gold += Mathf.Max(0, value);
    }

    public bool TrySpendGold(int value)
    {
        int clamped = Mathf.Max(0, value);
        if (gold < clamped)
        {
            return false;
        }

        gold -= clamped;
        return true;
    }

    public void UpgradeSpeed(float bonusValue)
    {
        moveSpeedBonus += Mathf.Max(0.01f, bonusValue);
    }

    public void UpgradeAttack(int bonusValue)
    {
        attackPower += Mathf.Max(1, bonusValue);
    }
}
