using System;
using System.Collections;
using UnityEngine;

// 플레이어가 진입하면 HandcuffsMoneyExchangeZone에 쌓인 Money를
// 위에서부터(LIFO) 빠르게 플레이어 인벤토리로 전달
// exchangeZone은 HandcuffsMoneyExchangeZone.Awake()에서 자동 주입됨
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class MoneyZone : MonoBehaviour
{
    [Tooltip("Money 1개 수거 간격 (초) — 작을수록 빠름")]
    [SerializeField] private float collectInterval = 0.05f;

    private HandcuffsMoneyExchangeZone exchangeZone;
    private bool isCollecting;

    public event Action<PlayerAgent> OnPlayerEntered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    // HandcuffsMoneyExchangeZone.Awake()에서 호출
    public void Initialize(HandcuffsMoneyExchangeZone zone)
    {
        exchangeZone = zone;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        OnPlayerEntered?.Invoke(player);

        if (isCollecting) return;
        if (exchangeZone == null) return;
        if (exchangeZone.ActiveMoneyCount <= 0) return;

        StartCoroutine(CollectRoutine(player));
    }

    private IEnumerator CollectRoutine(PlayerAgent player)
    {
        isCollecting = true;

        while (exchangeZone.ActiveMoneyCount > 0)
        {
            exchangeZone.TakeTopMoney();
            player.CollectItem(ItemType.Money);

            yield return new WaitForSeconds(Mathf.Max(0f, collectInterval));
        }

        Debug.Log("[MoneyZone] Money 전체 수거 완료");
        isCollecting = false;
    }
}
