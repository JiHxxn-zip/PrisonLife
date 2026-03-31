using UnityEngine;

// 무기 베이스 클래스.
// 서브클래스에서 Attack()을 구현해 투사체 발사·근접 공격 등 다양한 유형으로 확장.
public abstract class WeaponBase : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected int   damage         = 10;

    [Header("투사체 풀")]
    [SerializeField] protected BulletPool bulletPool;

    private float _lastAttackTime = float.MinValue;

    public bool IsReady => Time.time >= _lastAttackTime + attackCooldown;

    // PlayerCombat에서 호출 — 쿨다운 통과 시 Attack 실행
    public void TryAttack(Transform target)
    {
        if (!IsReady) return;
        _lastAttackTime = Time.time;
        Attack(target);
    }

    // 서브클래스에서 구체적인 공격 로직 구현
    protected abstract void Attack(Transform target);
}
