using UnityEngine;

// 풀링 가능한 투사체 베이스.
// 서브클래스에서 OnLaunched() 오버라이드로 특수 이동·이펙트 추가 가능.
public abstract class BulletBase : MonoBehaviour
{
    [SerializeField] protected float speed      = 10f;
    [SerializeField] protected float maxLifetime = 3f;

    protected int        damage;
    protected BulletPool pool;
    private   Vector3    _direction;
    private   float      _spawnTime;

    // ── 발사 ─────────────────────────────────────────

    public void Launch(Vector3 origin, Vector3 direction, int damage, BulletPool pool)
    {
        transform.position = origin;
        _direction         = direction.normalized;
        this.damage        = damage;
        this.pool          = pool;
        _spawnTime         = Time.time;
        gameObject.SetActive(true);
        OnLaunched();
    }

    // 서브클래스 추가 초기화 훅
    protected virtual void OnLaunched() { }

    // ── 매 프레임 이동 + 수명 체크 ───────────────────

    protected virtual void Update()
    {
        transform.position += _direction * speed * Time.deltaTime;

        if (Time.time - _spawnTime >= maxLifetime)
            ReturnToPool();
    }

    // ── 피격 처리 ─────────────────────────────────────

    protected virtual void OnTriggerEnter(Collider other)
    {
        IAttackable target = other.GetComponentInParent<IAttackable>();
        if (target == null) return;

        OnHit(target);
    }

    // 서브클래스에서 오버라이드해 이펙트·관통 등 추가
    protected virtual void OnHit(IAttackable target)
    {
        target.TakeDamage(damage);
        ReturnToPool();
    }

    // ── 풀 반환 ───────────────────────────────────────

    protected void ReturnToPool()
    {
        pool.Return(this);
    }
}
