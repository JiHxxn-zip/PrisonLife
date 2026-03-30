using System;
using UnityEngine;

// 몬스터 베이스 클래스. IAttackable 구현.
// 피격 시 플레이어 확인 후 추적 시작.
// chaseRange 이상 멀어지면 귀환, 피격 시 공격 방향 반대로 넉백.
// HP 관리는 HpComponent에 위임 — TakeDamage 호출 시 OnHPChanged 이벤트로 OverheadHpBar 자동 갱신.
// 서브클래스에서 OnHit / OnDeath / Chase 오버라이드로 다양한 몬스터 유형 확장.
[RequireComponent(typeof(HpComponent))]
public abstract class MonsterBase : MonoBehaviour, IAttackable
{
    public event Action OnDied;

    [Header("스탯")]
    [SerializeField] protected int   maxHp     = 100;
    [SerializeField] protected float moveSpeed = 3f;

    [Header("추적 / 귀환")]
    [SerializeField] protected float chaseRange      = 8f;   // 이 거리 초과 시 추적 포기
    [SerializeField] protected float returnSpeed     = 4f;   // 귀환 속도
    [SerializeField] private   float returnStopDist  = 0.2f; // 집 도착 판정

    [Header("넉백")]
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDecay = 10f;  // 감속 계수 (클수록 빨리 멈춤)

    [Header("드롭")]
    [SerializeField] private GameObject moneyDropPrefab;

    protected HpComponent _hp;
    protected Transform   playerTransform;
    protected bool        isChasing;

    private bool    _isReturning;
    private Vector3 _homePosition;
    private Vector3 _knockbackVelocity;

    // ── 초기화 ────────────────────────────────────────

    protected virtual void Awake()
    {
        _hp           = GetComponent<HpComponent>();
        _hp.Initialize(maxHp);
        _homePosition = transform.position;
    }

    protected virtual void OnEnable()
    {
        _hp.ResetHp();
        isChasing          = false;
        _isReturning       = false;
        _knockbackVelocity = Vector3.zero;
    }

    // ── 매 프레임 ─────────────────────────────────────

    protected virtual void Update()
    {
        UpdateKnockback();

        if (_hp.IsDead) return;

        if (isChasing)
        {
            if (playerTransform == null)
            {
                isChasing = false;
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer > chaseRange)
            {
                // 플레이어가 너무 멀어지면 추적 포기, 귀환
                isChasing    = false;
                _isReturning = true;
            }
            else
            {
                Chase();
            }
        }
        else if (_isReturning)
        {
            ReturnToHome();
        }
    }

    // 기본 추적: 플레이어 방향으로 직선 이동 + Y축 회전
    protected virtual void Chase()
    {
        Vector3 dir = (playerTransform.position - transform.position).normalized;
        FaceDirection(dir);
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    // 집으로 귀환 + Y축 회전
    protected virtual void ReturnToHome()
    {
        Vector3 toHome = _homePosition - transform.position;
        if (toHome.magnitude <= returnStopDist)
        {
            transform.position = _homePosition;
            _isReturning       = false;
            return;
        }
        FaceDirection(toHome.normalized);
        transform.position += toHome.normalized * returnSpeed * Time.deltaTime;
    }

    // 이동 방향으로 Y축만 즉시 회전
    protected void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    // 넉백 속도 감속 적용
    private void UpdateKnockback()
    {
        if (_knockbackVelocity.sqrMagnitude < 0.01f) return;
        transform.position += _knockbackVelocity * Time.deltaTime;
        _knockbackVelocity  = Vector3.MoveTowards(
            _knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
    }

    // ── IAttackable ───────────────────────────────────

    public virtual void TakeDamage(int damage, Vector3 hitFrom)
    {
        if (_hp.IsDead) return;
        _hp.TakeDamage(damage); // OnHPChanged 이벤트 발행 → OverheadHpBar 자동 갱신
        if (_hp.IsDead) OnDeath();
        else            OnHit(hitFrom);
    }

    // ── 피격 / 사망 훅 ───────────────────────────────

    protected virtual void OnHit(Vector3 hitFrom)
    {
        // 플레이어 참조가 없으면 씬에서 탐색
        if (playerTransform == null)
            playerTransform = FindObjectOfType<PlayerCombat>()?.transform;

        // 추적 시작 (귀환 중단)
        isChasing    = true;
        _isReturning = false;

        // 넉백: 공격받은 방향의 반대로
        Vector3 knockDir = (transform.position - hitFrom).normalized;
        knockDir.y         = 0f;  // 수직 방향은 무시
        _knockbackVelocity = knockDir * knockbackForce;
    }

    protected virtual void OnDeath()
    {
        _knockbackVelocity = Vector3.zero;
        OnDied?.Invoke();
        if (moneyDropPrefab != null)
            Instantiate(moneyDropPrefab, transform.position, Quaternion.identity);
        gameObject.SetActive(false);
    }

    public int CurrentHp => _hp.CurrentHp;
    public int MaxHp     => _hp.MaxHp;
}
