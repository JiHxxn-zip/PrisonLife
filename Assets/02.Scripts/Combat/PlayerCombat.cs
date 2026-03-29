using System.Collections.Generic;
using UnityEngine;

// 무기 앵커 관리 + 공격 범위 센서로 몬스터 감지 후 TryAttack() 호출.
// (WeaponAnchorSystem 기능을 통합)
[DisallowMultipleComponent]
public class PlayerCombat : MonoBehaviour
{
    [Header("무기 앵커")]
    [SerializeField] private Transform[] anchors = new Transform[4];

    [Header("공격 범위 센서")]
    [SerializeField] private AttackRangeSensor attackRangeSensor;

    private readonly WeaponBase[]    _equipped       = new WeaponBase[4];
    private readonly List<Transform> _targetsInRange = new List<Transform>();

    // ── 무기 장착 / 해제 ──────────────────────────────

    // 빈 앵커에 무기 장착. 슬롯이 모두 찬 경우 false 반환.
    public bool TryEquip(WeaponBase weapon)
    {
        for (int i = 0; i < anchors.Length; i++)
        {
            if (_equipped[i] != null) continue;

            _equipped[i] = weapon;
            weapon.transform.SetParent(anchors[i]);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.gameObject.SetActive(true);
            return true;
        }

        Debug.Log("[PlayerCombat] 슬롯이 가득 찼습니다.");
        return false;
    }

    // 특정 슬롯 무기 제거
    public void Unequip(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _equipped.Length) return;
        if (_equipped[slotIndex] == null) return;

        _equipped[slotIndex].gameObject.SetActive(false);
        _equipped[slotIndex].transform.SetParent(null);
        _equipped[slotIndex] = null;
    }

    public IReadOnlyList<WeaponBase> GetEquipped() => _equipped;

    // ── 공격 범위 감지 ────────────────────────────────

    private void OnEnable()
    {
        if (attackRangeSensor == null) return;
        attackRangeSensor.OnEntered += HandleEntered;
        attackRangeSensor.OnExited  += HandleExited;
    }

    private void OnDisable()
    {
        if (attackRangeSensor == null) return;
        attackRangeSensor.OnEntered -= HandleEntered;
        attackRangeSensor.OnExited  -= HandleExited;
    }

    private void Update()
    {
        _targetsInRange.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);

        if (_targetsInRange.Count == 0) return;

        Transform target = GetNearestTarget();
        if (target == null) return;

        foreach (WeaponBase weapon in _equipped)
        {
            if (weapon == null) continue;
            weapon.TryAttack(target);
        }
    }

    private void HandleEntered(Collider other)
    {
        if (other.GetComponentInParent<MonsterBase>() != null)
            _targetsInRange.Add(other.transform);
    }

    private void HandleExited(Collider other)
    {
        _targetsInRange.Remove(other.transform);
    }

    private Transform GetNearestTarget()
    {
        Transform nearest    = null;
        float     minSqrDist = float.MaxValue;

        foreach (Transform t in _targetsInRange)
        {
            float sqrDist = (t.position - transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest    = t;
            }
        }

        return nearest;
    }
}
