using UnityEngine;

// WeaponBase 구체 구현 — 총기형 무기.
// 타겟 방향으로 투사체를 발사한다.
public class GunWeapon : WeaponBase
{
    [Header("발사 위치")]
    [SerializeField] private Transform muzzle; // 총구 위치. 없으면 무기 중심 사용

    protected override void Attack(Transform target)
    {
        if (bulletPool == null)
        {
            Debug.LogWarning("[GunWeapon] BulletPool이 연결되지 않았습니다.");
            return;
        }

        Vector3 origin    = muzzle != null ? muzzle.position : transform.position;
        Vector3 direction = (target.position - origin).normalized;

        BulletBase bullet = bulletPool.Get();
        bullet.Launch(origin, direction, damage, bulletPool);
    }
}
