using UnityEngine;

// 무기 프리팹에 부착.
// 플레이어와 트리거 시 WeaponAnchorSystem에 장착 시도.
// 슬롯이 가득 찼으면 습득되지 않는다.
[RequireComponent(typeof(WeaponBase))]
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
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
        WeaponAnchorSystem anchors = other.GetComponentInParent<WeaponAnchorSystem>();
        if (anchors == null) return;

        bool equipped = anchors.TryEquip(_weapon);
        if (!equipped) return; // 슬롯 가득 — 아무것도 하지 않음

        // 장착 성공 : 픽업 트리거 제거, 무기 활성화
        _weapon.enabled = true;
        _collider.enabled = false;
        enabled = false;
    }
}
