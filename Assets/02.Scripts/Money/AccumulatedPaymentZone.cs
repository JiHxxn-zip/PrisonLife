using TMPro;
using UnityEngine;

// 누적 결제 Zone 베이스 클래스
// 플레이어가 트리거될 때마다 보유 Money 전액을 즉시 납부하며,
// currentPaid 가 targetCost 에 도달하면 OnPaymentComplete() 호출
[RequireComponent(typeof(Collider))]
public abstract class AccumulatedPaymentZone : MonoBehaviour
{
    [Header("누적 결제 UI (World Space)")]
    [SerializeField] private TMP_Text progressText;

    protected int currentPaid;

    // 서브클래스가 현재 단계의 목표 금액을 반환
    protected abstract int CurrentTarget { get; }

    // 목표 금액 도달 시 서브클래스에서 실제 기능 수행
    protected abstract void OnPaymentComplete(PlayerAgent player, ItemStackInventory inventory);

    // 서브클래스 추가 초기화 훅
    protected virtual void OnAwake() { }

    // ── 초기화 ────────────────────────────────────────────────

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        currentPaid = 0;
        OnAwake();
        RefreshUI();
    }

    // ── 트리거 ────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<MetalCollectorTrigger>() != null) return;

        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        ItemStackInventory inventory = player.GetComponentInChildren<ItemStackInventory>();
        if (inventory == null) return;

        int remaining = CurrentTarget - currentPaid;
        if (remaining <= 0) return;

        int moneyValuePerItem = inventory.MoneyValuePerItem;
        if (moneyValuePerItem <= 0) return;

        // 남은 목표를 채우는 데 필요한 아이템 수 (올림)
        int itemsForRemaining = Mathf.CeilToInt((float)remaining / moneyValuePerItem);
        // 실제 납부할 아이템 수: 필요량과 보유량 중 작은 쪽
        int itemsToPay = Mathf.Min(itemsForRemaining, inventory.MoneyCount);
        if (itemsToPay <= 0) return;

        int actualValuePaid = itemsToPay * moneyValuePerItem;
        if (!inventory.TryConsumeMoneyValue(actualValuePaid)) return;

        currentPaid = Mathf.Min(currentPaid + actualValuePaid, CurrentTarget);

        Debug.Log($"[{GetType().Name}] 납부 {actualValuePaid}원 → {currentPaid}/{CurrentTarget}");
        RefreshUI();

        if (currentPaid >= CurrentTarget)
            OnPaymentComplete(player, inventory);
    }

    // ── 보조 메서드 (서브클래스 사용 가능) ───────────────────

    // 단계 전환 시 누적 금액 초기화 (UI 갱신 포함)
    protected void ResetPayment()
    {
        currentPaid = 0;
        RefreshUI();
    }

    // currentPaid == 0 이면 목표 금액만, 아니면 "현재/목표" 형태로 표시
    protected void RefreshUI()
    {
        if (progressText == null) return;
        progressText.text = currentPaid > 0
            ? $"{currentPaid}/{CurrentTarget}"
            : $"{CurrentTarget}";
    }
}
