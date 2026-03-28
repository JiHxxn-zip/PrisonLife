using UnityEngine;

// 기본 몬스터 — MonsterBase 구체 구현.
// 피격 시 플레이어를 향해 직선 추적, HP 0 시 비활성화.
public class NormalMonster : MonsterBase
{
    [Header("피격 피드백")]
    [SerializeField] private Renderer monsterRenderer;
    [SerializeField] private Color    hitColor  = Color.red;
    [SerializeField] private float    hitFlashDuration = 0.1f;

    private Color  _originalColor;
    private float  _hitFlashTimer;
    private bool   _isFlashing;

    protected override void Awake()
    {
        base.Awake();

        if (monsterRenderer == null)
            monsterRenderer = GetComponentInChildren<Renderer>();

        if (monsterRenderer != null)
            _originalColor = monsterRenderer.material.color;
    }

    protected override void Update()
    {
        base.Update();
        UpdateHitFlash();
    }

    protected override void OnHit()
    {
        base.OnHit();
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
