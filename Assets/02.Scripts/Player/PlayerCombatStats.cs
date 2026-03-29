using System;
using UnityEngine;

// 플레이어 HP + 전투용 Money를 관리하는 컴포넌트.
// IAttackable 구현 → 몬스터가 TakeDamage()로 데미지 전달.
// Ch2MoneyPickup.OnMoneyCollected 이벤트를 구독해 자동으로 Money 카운트.
// 이벤트로 UI(HUD) 등에 변경 사항을 통보.
[DisallowMultipleComponent]
public class PlayerCombatStats : MonoBehaviour, IAttackable
{
    // ── 외부 통보 이벤트 ──────────────────────────────
    public event Action<int, int> OnHpChanged;    // (currentHp, maxHp)
    public event Action<int>      OnMoneyChanged; // (totalMoney)
    public event Action           OnDied;

    // ── HP ────────────────────────────────────────────
    [Header("HP")]
    [SerializeField] private int   maxHp              = 100;
    [SerializeField] private float invincibleDuration = 0.5f; // 피격 후 무적 시간

    private int   _currentHp;
    private float _invincibleTimer;
    private bool  _isDead;

    // ── Money ─────────────────────────────────────────
    // 픽업 1개당 획득량은 Ch2MoneyPickup.MoneyPerPickup 상수를 공유

    private int _money;

    // ── 프로퍼티 ──────────────────────────────────────
    public int  CurrentHp => _currentHp;
    public int  MaxHp     => maxHp;
    public int  Money     => _money;
    public bool IsDead    => _isDead;

    // ── 초기화 ────────────────────────────────────────

    private void Awake()
    {
        _currentHp = maxHp;
    }

    private void OnEnable()
    {
        Ch2MoneyPickup.OnMoneyCollected += HandleMoneyCollected;
    }

    private void OnDisable()
    {
        Ch2MoneyPickup.OnMoneyCollected -= HandleMoneyCollected;
    }

    // ── Update — 무적 타이머 ──────────────────────────

    private void Update()
    {
        if (_invincibleTimer > 0f)
            _invincibleTimer -= Time.deltaTime;
    }

    // ── IAttackable ───────────────────────────────────

    public void TakeDamage(int damage, Vector3 hitFrom)
    {
        if (_isDead || _invincibleTimer > 0f) return;

        _currentHp       = Mathf.Max(0, _currentHp - damage);
        _invincibleTimer = invincibleDuration;

        OnHpChanged?.Invoke(_currentHp, maxHp);

        if (_currentHp == 0)
            HandleDeath();
    }

    // ── Money 획득 ────────────────────────────────────

    private void HandleMoneyCollected()
    {
        _money += Ch2MoneyPickup.MoneyPerPickup;
        OnMoneyChanged?.Invoke(_money);
    }

    // ── 사망 ──────────────────────────────────────────

    private void HandleDeath()
    {
        _isDead = true;
        OnDied?.Invoke();
    }
}
