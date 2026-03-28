using UnityEngine;

// 플레이어 레벨을 감지해 단계별 구매 Zone을 순차 해금하는 오케스트레이터
// 자체 Collider 불필요 — 트리거 역할은 각 PurchaseZone이 담당
[DisallowMultipleComponent]
public class HiringZone : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("미지정 시 Start()에서 FindObjectOfType으로 자동 탐색")]
    [SerializeField] private PlayerAgent playerAgent;

    [Header("1단계 — Collector 구매 Zone (레벨 도달 시 활성화)")]
    [SerializeField] private CollectorPurchaseZone collectorPurchaseZone;
    [SerializeField] private int collectorUnlockLevel = 2;

    private bool collectorUnlocked;

    private void Start()
    {
        if (playerAgent == null)
            playerAgent = FindObjectOfType<PlayerAgent>();

        // 두 Zone 모두 처음엔 비활성 상태여야 함
        if (collectorPurchaseZone != null)
            collectorPurchaseZone.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (collectorUnlocked) return;
        if (playerAgent == null) return;

        if (playerAgent.Level >= collectorUnlockLevel)
        {
            collectorUnlocked = true;

            if (collectorPurchaseZone != null)
                collectorPurchaseZone.gameObject.SetActive(true);

            Debug.Log($"[HiringZone] 레벨 {collectorUnlockLevel} 달성 → CollectorPurchaseZone 활성화");
        }
    }
}
