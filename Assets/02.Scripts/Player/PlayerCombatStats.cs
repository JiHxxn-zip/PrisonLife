using System;
using UnityEngine;

// 플레이어 HP + 전투용 Money를 관리하는 컴포넌트.
// IAttackable 구현 → 몬스터가 TakeDamage()로 데미지 전달.
// HP 관리는 HpComponent에 위임 — OnHPChanged 이벤트를 OnHpChanged로 전달해 기존 HUD와 호환.
// Ch2MoneyPickup.OnMoneyCollected 이벤트를 구독해 자동으로 Money 카운트.
[DisallowMultipleComponent]
[RequireComponent(typeof(HpComponent))]
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

    private HpComponent _hp;
    private float       _invincibleTimer;

    // ── Money ─────────────────────────────────────────
    // 픽업 1개당 획득량은 Ch2MoneyPickup.MoneyPerPickup 상수를 공유

    private int _money;

    // ── 프로퍼티 ──────────────────────────────────────
    public int  CurrentHp => _hp.CurrentHp;
    public int  MaxHp     => _hp.MaxHp;
    public int  Money     => _money;
    public bool IsDead    => _hp.IsDead;

    // ── 초기화 ────────────────────────────────────────

    private void Awake()
    {
        _hp = GetComponent<HpComponent>();
        _hp.Initialize(maxHp);
        _hp.OnHPChanged += ForwardHpChanged;
        _hp.OnDied      += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_hp == null) return;
        _hp.OnHPChanged -= ForwardHpChanged;
        _hp.OnDied      -= HandleDeath;
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
        if (_hp.IsDead || _invincibleTimer > 0f) return;
        _invincibleTimer = invincibleDuration;
        _hp.TakeDamage(damage); // OnHPChanged → ForwardHpChanged, HP=0 시 OnDied → HandleDeath
    }

    // ── HP 이벤트 전달 ────────────────────────────────

    private void ForwardHpChanged(int current, int max)
    {
        OnHpChanged?.Invoke(current, max);
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
        OnDied?.Invoke();
    }
}
