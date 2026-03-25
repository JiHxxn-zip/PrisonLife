using System.Collections;
using UnityEngine;

// 특정 금액을 소모해 플레이어 능력치를 업그레이드하는 존
// 1단계: 20원 → PlayerLevel+1, CapsuleCollider Radius+1, MetalMax+5 → 2단계로 전환
// 2단계: 50원 → PlayerLevel+1, CapsuleCollider Radius+1, MetalMax*2 → Zone 비활성화
// 충돌 직후 3초간 재충돌 방지
[RequireComponent(typeof(Collider))]
public class LevelUpZone : MonoBehaviour
{
    [Header("Stage 1")]
    [SerializeField] private int stage1Cost = 20;
    [SerializeField] private int stage1MetalMaxBonus = 5;

    [Header("Stage 2")]
    [SerializeField] private int stage2Cost = 50;
    [SerializeField] private float stage2MetalMaxMultiplier = 2f;

    [Header("Common")]
    [SerializeField] private int levelIncreasePerStage = 1;
    [SerializeField] private float capsuleRadiusIncreasePerStage = 1f;
    [SerializeField] private float triggerCooldown = 3f;

    private int currentStage = 1;
    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        ItemStackInventory inventory = player.GetComponentInChildren<ItemStackInventory>();
        if (inventory == null) return;

        int cost = currentStage == 1 ? stage1Cost : stage2Cost;

        if (inventory.MoneyTotalValue < cost)
        {
            Debug.Log($"[LevelUpZone] 잔액 부족 ({inventory.MoneyTotalValue}/{cost})");
            return;
        }

        if (!inventory.TryConsumeMoneyValue(cost)) return;

        ApplyStageEffects(player, inventory);

        bool isFinalStage = currentStage > 2;
        StartCoroutine(CooldownCoroutine(isFinalStage));
    }

    private void ApplyStageEffects(PlayerAgent player, ItemStackInventory inventory)
    {
        // 공통: 레벨 +1
        player.AddLevel(levelIncreasePerStage);

        // 공통: CapsuleCollider Radius +1
        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.radius += capsuleRadiusIncreasePerStage;
        }

        if (currentStage == 1)
        {
            inventory.AddMetalMaxBonus(stage1MetalMaxBonus);
            Debug.Log($"[LevelUpZone] 1단계 완료 — 비용 {stage1Cost}원, MetalMax+{stage1MetalMaxBonus} → 2단계 전환");
            currentStage = 2;
        }
        else if (currentStage == 2)
        {
            inventory.SetMetalMaxMultiplier(stage2MetalMaxMultiplier);
            Debug.Log($"[LevelUpZone] 2단계 완료 — 비용 {stage2Cost}원, MetalMax×{stage2MetalMaxMultiplier} → Zone 비활성화 예정");
            currentStage = 3; // 더 이상 업그레이드 없음
        }
    }

    // cooldown 동안 콜라이더 비활성화. 마지막 단계 완료 시 cooldown 후 Zone 비활성화
    private IEnumerator CooldownCoroutine(bool deactivateAfter)
    {
        zoneCollider.enabled = false;
        yield return new WaitForSeconds(triggerCooldown);

        if (deactivateAfter)
        {
            gameObject.SetActive(false);
        }
        else
        {
            zoneCollider.enabled = true;
        }
    }
}
