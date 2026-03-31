using System;
using UnityEngine;

// 무기 프리팹에 부착.
// 습득 전까지 상하 부유 + Y축 회전 모션을 재생한다.
// 플레이어와 트리거 시 PlayerCombat에 장착 시도.
// 슬롯이 가득 찼으면 습득되지 않는다.
[RequireComponent(typeof(WeaponBase))]
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    [Header("부유 모션")]
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float bobSpeed     = 2f;
    [SerializeField] private float rotateSpeed  = 90f;

    public event Action OnPickedUp;

    private WeaponBase _weapon;
    private Collider   _collider;
    private Vector3    _originLocalPos;
    private bool       _equipped;

    private void Awake()
    {
        _weapon         = GetComponent<WeaponBase>();
        _collider       = GetComponent<Collider>();
        _originLocalPos = transform.localPosition;

        _weapon.enabled = false;
    }

    private void Update()
    {
        float offsetY = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = _originLocalPos + Vector3.up * offsetY;

        if (!_equipped)
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCombat combat = other.GetComponentInParent<PlayerCombat>();
        if (combat != null)
        {
            bool equipped = combat.TryEquip(_weapon);
            if (!equipped) return;
        }
        else
        {
            if (other.GetComponentInParent<HyperCasualPlayerController>() == null) return;
        }

        // 픽업 성공 — 앵커 기준 원점으로 리셋, 회전 중단 후 상하 모션만 유지
        _originLocalPos   = Vector3.zero;
        _equipped         = true;
        _weapon.enabled   = true;
        _collider.enabled = false;

        OnPickedUp?.Invoke();
    }
}
