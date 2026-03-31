using UnityEngine;

// WeaponBase 구체 구현 — 총기형 무기.
// 쿨다운(재장전 대기) 중에는 마지막 타겟 방향으로 Y축 회전 조준.
// 쿨다운 완료(IsReady) 후에는 장착 기본 방향으로 부드럽게 복귀.
public class GunWeapon : WeaponBase
{
    [Header("발사 위치")]
    [SerializeField] private Transform muzzle; // 총구 위치. 없으면 무기 중심 사용

    [Header("조준 회전")]
    [SerializeField] private float rotationSpeed = 8f; // 조준·복귀 회전 속도

    private Transform  _currentTarget;      // 마지막으로 공격한 타겟
    private Quaternion _defaultLocalRot;    // 장착 시 기본 로컬 회전 (복귀 기준)

    // ── 초기화 ────────────────────────────────────────

    private void Awake()
    {
        _defaultLocalRot = transform.localRotation;
    }

    // ── 매 프레임 회전 처리 ───────────────────────────

    private void Update()
    {
        UpdateAimRotation();
    }

    private void UpdateAimRotation()
    {
        if (!IsReady && _currentTarget != null)
        {
            // 쿨다운 중 — 타겟 방향으로 Y축 회전 조준
            Vector3 dir = _currentTarget.position - transform.position;
            dir.y = 0f; // Y축만 회전
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion aimRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, aimRot, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // 쿨다운 완료 또는 타겟 없음 — 기본 방향으로 복귀
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, _defaultLocalRot, rotationSpeed * Time.deltaTime);
        }
    }

    // ── 발사 ─────────────────────────────────────────

    protected override void Attack(Transform target)
    {
        if (bulletPool == null)
        {
            Debug.LogWarning("[GunWeapon] BulletPool이 연결되지 않았습니다.");
            return;
        }

        _currentTarget = target; // 발사 직전 타겟 캐시 → 쿨다운 중 조준에 사용

        Vector3 origin = muzzle != null ? muzzle.position : transform.position;

        BulletBase bullet = bulletPool.Get();
        bullet.Launch(origin, target, damage, bulletPool);
    }
}
