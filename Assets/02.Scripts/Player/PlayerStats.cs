using System;
using UnityEngine;

// 이동·공격·골드 등 플레이어 수치 (업그레이드 반영)
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

    // 골드 증가
    public void AddGold(int value)
    {
        gold += Mathf.Max(0, value);
    }

    // 골드 차감 시도, 부족하면 false
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

    // 이동 속도 보너스 누적
    public void UpgradeSpeed(float bonusValue)
    {
        moveSpeedBonus += Mathf.Max(0.01f, bonusValue);
    }

    // 공격력 보너스 누적
    public void UpgradeAttack(int bonusValue)
    {
        attackPower += Mathf.Max(1, bonusValue);
    }
}
