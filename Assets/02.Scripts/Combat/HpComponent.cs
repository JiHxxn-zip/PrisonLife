using System;
using UnityEngine;

// HP 데이터 컴포넌트. MonsterBase·PlayerCombatStats가 보유하며
// TakeDamage(int) 호출 시 OnHPChanged 이벤트를 발행해 뷰(OverheadHpBar)가 스스로 갱신되게 함.
// 데이터 계층 — 렌더링·UI 의존성 없음.
[DisallowMultipleComponent]
public class HpComponent : MonoBehaviour
{
    // ── 이벤트 ────────────────────────────────────────
    public event Action<int, int> OnHPChanged; // (current, max)
    public event Action           OnDied;

    // ── 내부 상태 ─────────────────────────────────────
    private int  _maxHp;
    private int  _currentHp;
    private bool _isDead;

    // ── 프로퍼티 ──────────────────────────────────────
    public int  CurrentHp => _currentHp;
    public int  MaxHp     => _maxHp;
    public bool IsDead    => _isDead;

    // ── 초기화 ────────────────────────────────────────

    /// <summary>maxHp를 받아 HP를 초기화합니다. MonsterBase·PlayerCombatStats의 Awake에서 호출.</summary>
    public void Initialize(int maxHp)
    {
        _maxHp     = Mathf.Max(1, maxHp);
        _currentHp = _maxHp;
        _isDead    = false;
        OnHPChanged?.Invoke(_currentHp, _maxHp);
    }

    /// <summary>오브젝트 풀 재활성화 시 HP를 최대로 복원합니다.</summary>
    public void ResetHp()
    {
        _currentHp = _maxHp;
        _isDead    = false;
        OnHPChanged?.Invoke(_currentHp, _maxHp);
    }

    // ── HP 변경 ───────────────────────────────────────

    /// <summary>피격 데미지를 적용합니다. HP가 0이 되면 OnDied를 발행합니다.</summary>
    public void TakeDamage(int damage)
    {
        if (_isDead) return;
        _currentHp = Mathf.Max(0, _currentHp - damage);
        OnHPChanged?.Invoke(_currentHp, _maxHp);
        if (_currentHp == 0)
        {
            _isDead = true;
            OnDied?.Invoke();
        }
    }

    /// <summary>HP를 회복합니다.</summary>
    public void Heal(int amount)
    {
        if (_isDead) return;
        _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
        OnHPChanged?.Invoke(_currentHp, _maxHp);
    }
}
