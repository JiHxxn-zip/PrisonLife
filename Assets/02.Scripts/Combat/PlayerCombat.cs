using System.Collections.Generic;
using UnityEngine;

// 전투 트리거로 범위 내 몬스터를 감지하고
// 장착된 모든 무기의 TryAttack()을 호출.
[DisallowMultipleComponent]
[RequireComponent(typeof(WeaponAnchorSystem))]
public class PlayerCombat : MonoBehaviour
{
    private WeaponAnchorSystem _anchorSystem;
    private readonly List<Transform> _targetsInRange = new List<Transform>();

    private void Awake()
    {
        _anchorSystem = GetComponent<WeaponAnchorSystem>();
    }

    private void Update()
    {
        // 비활성화된 타겟 정리
        _targetsInRange.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);

        if (_targetsInRange.Count == 0) return;

        Transform target = GetNearestTarget();
        if (target == null) return;

        foreach (WeaponBase weapon in _anchorSystem.GetEquipped())
        {
            if (weapon == null) continue;
            weapon.TryAttack(target);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<MonsterBase>() != null)
            _targetsInRange.Add(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        _targetsInRange.Remove(other.transform);
    }

    private Transform GetNearestTarget()
    {
        Transform nearest = null;
        float minSqrDist  = float.MaxValue;

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
