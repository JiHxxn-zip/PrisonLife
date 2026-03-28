using System.Collections.Generic;
using UnityEngine;

// 플레이어의 4개 무기 앵커를 관리.
// 무기 획득 시 TryEquip() 호출 → 빈 슬롯에 자동 장착.
[DisallowMultipleComponent]
public class WeaponAnchorSystem : MonoBehaviour
{
    [SerializeField] private Transform[] anchors = new Transform[4];

    private readonly WeaponBase[] _equipped = new WeaponBase[4];

    // 빈 앵커에 무기 장착. 슬롯이 모두 찬 경우 false 반환
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

        Debug.Log("[WeaponAnchorSystem] 슬롯이 가득 찼습니다.");
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
}
