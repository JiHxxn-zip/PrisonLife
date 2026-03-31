using UnityEngine;

// 누적 결제로 플레이어를 업그레이드하는 Zone
// 1단계: stage1Cost 달성 → PlayerLevel+1, CapsuleRadius+1, MetalMax+bonus → 2단계로 전환
// 2단계: stage2Cost 달성 → PlayerLevel+1, CapsuleRadius+1, MetalMax×multiplier → Zone 비활성화
[DisallowMultipleComponent]
public class LevelUpZone : AccumulatedPaymentZone
{
    [Header("Stage 1")]
    [SerializeField] private int stage1Cost = 20;
    [SerializeField] private int stage1MetalMaxBonus = 5;

    [Header("Stage 2")]
    [SerializeField] private int stage2Cost = 50;
    [SerializeField] private float stage2MetalMaxMultiplier = 2f;

    [Header("Common")]
    [SerializeField] private int levelIncreasePerStage = 1;

    private int currentStage = 1;

    protected override int CurrentTarget => currentStage == 1 ? stage1Cost : stage2Cost;

    protected override void OnPaymentComplete(PlayerAgent player, ItemStackInventory inventory)
    {
        ApplyStageEffects(player, inventory);

        if (currentStage > 2)
        {
            gameObject.SetActive(false);
        }
        else
        {
            // 다음 단계 준비: 누적 금액 초기화 및 UI 갱신
            ResetPayment();
        }
    }

    private void ApplyStageEffects(PlayerAgent player, ItemStackInventory inventory)
    {
        player.AddLevel(levelIncreasePerStage);

        if (currentStage == 1)
        {
            inventory.AddMetalMaxBonus(stage1MetalMaxBonus);
            Debug.Log($"[LevelUpZone] 1단계 완료 — 비용 {stage1Cost}원, MetalMax+{stage1MetalMaxBonus} → 2단계 전환");
            currentStage = 2;
        }
        else if (currentStage == 2)
        {
            inventory.SetMetalMaxMultiplier(stage2MetalMaxMultiplier);
            Debug.Log($"[LevelUpZone] 2단계 완료 — 비용 {stage2Cost}원, MetalMax×{stage2MetalMaxMultiplier} → Zone 비활성화");
            currentStage = 3;
        }
    }
}
