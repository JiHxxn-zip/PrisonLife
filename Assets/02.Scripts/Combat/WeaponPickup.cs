using System;
using UnityEngine;

// 무기 프리팹에 부착.
// 플레이어와 트리거 시 WeaponAnchorSystem에 장착 시도.
// 슬롯이 가득 찼으면 습득되지 않는다.
[RequireComponent(typeof(WeaponBase))]
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    public event Action OnPickedUp;

    private WeaponBase _weapon;
    private Collider   _collider;

    private void Awake()
    {
        _weapon   = GetComponent<WeaponBase>();
        _collider = GetComponent<Collider>();

        // 바닥에 놓인 상태에서는 무기 자체 비활성
        _weapon.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // WeaponAnchorSystem이 있으면 슬롯 장착 시도
        WeaponAnchorSystem anchors = other.GetComponentInParent<WeaponAnchorSystem>();
        if (anchors != null)
        {
            bool equipped = anchors.TryEquip(_weapon);
            if (!equipped) return; // 슬롯 가득 — 습득 불가
        }
        else
        {
            // WeaponAnchorSystem이 없는 플레이어(Player2 등)는
            // PlayerArrowAgent 여부로 플레이어 판별
            if (other.GetComponentInParent<PlayerArrowAgent>() == null) return;
        }

        // 픽업 성공
        _weapon.enabled   = true;
        _collider.enabled = false;
        enabled           = false;

        OnPickedUp?.Invoke();
    }
}
