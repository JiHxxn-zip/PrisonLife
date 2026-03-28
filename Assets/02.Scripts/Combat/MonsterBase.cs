using UnityEngine;

// 몬스터 베이스 클래스. IAttackable 구현.
// 피격 시 플레이어 추적 시작, HP 0 시 비활성화.
// 서브클래스에서 OnHit / OnDeath / Chase 오버라이드로 다양한 몬스터 유형 확장.
public abstract class MonsterBase : MonoBehaviour, IAttackable
{
    [Header("스탯")]
    [SerializeField] protected int   maxHp     = 100;
    [SerializeField] protected float moveSpeed = 3f;

    protected int       currentHp;
    protected Transform playerTransform;
    protected bool      isChasing;

    // ── 초기화 ────────────────────────────────────────

    protected virtual void Awake()
    {
        currentHp = maxHp;
    }

    protected virtual void OnEnable()
    {
        currentHp = maxHp;
        isChasing = false;
    }

    // ── 매 프레임 추적 ────────────────────────────────

    protected virtual void Update()
    {
        if (!isChasing || playerTransform == null) return;
        Chase();
    }

    // 기본 추적: 플레이어 방향으로 직선 이동
    protected virtual void Chase()
    {
        Vector3 dir = (playerTransform.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    // ── IAttackable ───────────────────────────────────

    public virtual void TakeDamage(int damage)
    {
        currentHp -= damage;

        if (currentHp <= 0)
            OnDeath();
        else
            OnHit();
    }

    // ── 피격 / 사망 훅 ───────────────────────────────

    protected virtual void OnHit()
    {
        // 플레이어 참조가 없으면 씬에서 탐색
        if (playerTransform == null)
            playerTransform = FindObjectOfType<PlayerAgent>()?.transform;

        isChasing = true;
    }

    protected virtual void OnDeath()
    {
        gameObject.SetActive(false);
    }

    public int CurrentHp  => currentHp;
    public int MaxHp      => maxHp;
}
