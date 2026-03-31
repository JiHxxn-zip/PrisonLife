using UnityEngine;

// 기본 근접 몬스터 — MonsterBase 구체 구현.
// 피격 시 플레이어를 향해 직선 추적.
// 공격 사거리(attackRange) 이내에 진입하면 이동을 멈추고 attackCooldown 간격으로 근접 공격.
public class NormalMonster : MonsterBase
{
    [Header("근접 공격")]
    [SerializeField] private float attackRange    = 2f;
    [SerializeField] private int   attackDamage   = 10;
    [SerializeField] private float attackCooldown = 3f;

    [Header("피격 피드백")]
    [SerializeField] private Renderer monsterRenderer;
    [SerializeField] private Color    hitColor         = Color.red;
    [SerializeField] private float    hitFlashDuration = 0.1f;

    private float       _attackTimer;      // 0 이하일 때 공격 가능
    private IAttackable _playerAttackable; // 플레이어 IAttackable 캐시

    private Color _originalColor;
    private float _hitFlashTimer;
    private bool  _isFlashing;

    // ── 초기화 ────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        if (monsterRenderer == null)
            monsterRenderer = GetComponentInChildren<Renderer>();

        if (monsterRenderer != null)
            _originalColor = monsterRenderer.material.color;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _attackTimer      = 0f;   // 재활성화 시 즉시 공격 가능 상태로 리셋
        _playerAttackable = null;
    }

    // ── 매 프레임 ─────────────────────────────────────

    protected override void Update()
    {
        base.Update();
        UpdateAttackTimer();
        UpdateHitFlash();
    }

    // 공격 쿨다운 타이머 차감 (Update에서 매 프레임 처리)
    private void UpdateAttackTimer()
    {
        if (_attackTimer > 0f)
            _attackTimer -= Time.deltaTime;
    }

    // ── 추적 / 공격 ───────────────────────────────────

    // MonsterBase.Chase()를 오버라이드 — 사거리 내면 이동 멈추고 공격, 밖이면 기본 추적
    protected override void Chase()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= attackRange)
        {
            // 사거리 내 : 이동 멈추고 플레이어를 바라보며 공격 시도
            FaceDirection(playerTransform.position - transform.position);
            TryAttack();
        }
        else
        {
            base.Chase();
        }
    }

    // 쿨다운이 끝났을 때만 플레이어에게 데미지 전달
    private void TryAttack()
    {
        if (_attackTimer > 0f) return;

        // IAttackable 캐시 — 최초 한 번만 GetComponent
        if (_playerAttackable == null)
            _playerAttackable = playerTransform.GetComponent<IAttackable>();

        _playerAttackable?.TakeDamage(attackDamage, transform.position);
        _attackTimer = attackCooldown;
    }

    // ── 피격 / 사망 훅 ───────────────────────────────

    protected override void OnHit(Vector3 hitFrom)
    {
        base.OnHit(hitFrom);
        StartHitFlash();
    }

    protected override void OnDeath()
    {
        StopHitFlash();
        base.OnDeath();
    }

    // ── 피격 색상 깜빡임 ─────────────────────────────

    private void StartHitFlash()
    {
        if (monsterRenderer == null) return;
        monsterRenderer.material.color = hitColor;
        _hitFlashTimer = hitFlashDuration;
        _isFlashing    = true;
    }

    private void UpdateHitFlash()
    {
        if (!_isFlashing) return;

        _hitFlashTimer -= Time.deltaTime;
        if (_hitFlashTimer <= 0f)
        {
            monsterRenderer.material.color = _originalColor;
            _isFlashing = false;
        }
    }

    private void StopHitFlash()
    {
        if (monsterRenderer != null)
            monsterRenderer.material.color = _originalColor;
        _isFlashing = false;
    }
}
