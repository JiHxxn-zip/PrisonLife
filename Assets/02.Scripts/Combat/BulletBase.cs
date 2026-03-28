using UnityEngine;

// 풀링 가능한 투사체 베이스 — 호밍 방식.
// 발사 후 매 프레임 타겟을 향해 방향을 갱신하므로 플레이어 이동과 무관하게 반드시 명중.
public abstract class BulletBase : MonoBehaviour
{
    [SerializeField] protected float speed       = 10f;
    [SerializeField] protected float maxLifetime = 3f;
    [Tooltip("이 거리 이하이면 충돌 처리 (콜라이더 미사용 시 대비)")]
    [SerializeField] private   float hitDistance = 0.3f;

    protected int        damage;
    protected BulletPool pool;
    private   Transform  _target;
    private   float      _spawnTime;

    // ── 발사 ─────────────────────────────────────────

    public void Launch(Vector3 origin, Transform target, int damage, BulletPool pool)
    {
        transform.position = origin;
        _target            = target;
        this.damage        = damage;
        this.pool          = pool;
        _spawnTime         = Time.time;
        gameObject.SetActive(true);
        OnLaunched();
    }

    protected virtual void OnLaunched() { }

    // ── 매 프레임 호밍 이동 ───────────────────────────

    protected virtual void Update()
    {
        // 타겟이 사라졌으면 풀 반환
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            ReturnToPool();
            return;
        }

        Vector3 dir  = (_target.position - transform.position);
        float   dist = dir.magnitude;

        // 도달 판정
        if (dist <= hitDistance)
        {
            IAttackable attackable = _target.GetComponentInParent<IAttackable>();
            if (attackable != null)
                OnHit(attackable);
            else
                ReturnToPool();
            return;
        }

        transform.position += dir.normalized * speed * Time.deltaTime;

        // 수명 초과
        if (Time.time - _spawnTime >= maxLifetime)
            ReturnToPool();
    }

    // ── 콜라이더 피격 (보조) ──────────────────────────

    protected virtual void OnTriggerEnter(Collider other)
    {
        IAttackable target = other.GetComponentInParent<IAttackable>();
        if (target == null) return;
        OnHit(target);
    }

    // ── 피격 처리 ─────────────────────────────────────

    protected virtual void OnHit(IAttackable target)
    {
        target.TakeDamage(damage);
        ReturnToPool();
    }

    // ── 풀 반환 ───────────────────────────────────────

    protected void ReturnToPool()
    {
        _target = null;
        pool.Return(this);
    }
}
