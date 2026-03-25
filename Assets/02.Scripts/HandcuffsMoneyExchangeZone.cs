using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 진입 시:
//  1) 보유 Handcuffs 전체를 Zone의 handcuffAnchor로 이동
//  2) 한 턴마다 2 또는 4개(랜덤) Handcuffs 비활성화 + 동수 Money 프리팹 순차 생성
//  3) Money 는 항상 y=0 슬롯부터 쌓임 (현재 활성 Money 수를 기준으로 위치 계산)
[RequireComponent(typeof(Collider))]
public class HandcuffsMoneyExchangeZone : MonoBehaviour
{
    [Header("Handcuff Anchor (Zone 내 수갑 표시 위치)")]
    [SerializeField] private Transform handcuffAnchor;
    [SerializeField] private Vector3 handcuffLocalOffset = Vector3.zero;
    [SerializeField] private float handcuffSpacingY = 0.15f;

    [Header("Money Spawn")]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Transform moneyAnchor;
    [SerializeField] private Vector3 moneyLocalOffset = Vector3.zero;
    [SerializeField] private float moneySpacingY = 0.12f;

    [Header("Consume Options (랜덤으로 둘 중 하나 선택)")]
    [SerializeField] private int consumeOptionA = 2;
    [SerializeField] private int consumeOptionB = 4;

    [Header("Timing")]
    [Tooltip("Handcuffs 비활성화 + Money 생성 간격 (초)")]
    [SerializeField] private float spawnInterval = 1f;

    [Header("Money Value (UI 연동용)")]
    [SerializeField] private int moneyValuePerItem = 10;

    // Zone 앵커에 이동된 수갑 비주얼
    private readonly List<GameObject> zoneHandcuffs = new List<GameObject>();
    // 생성된 Money 프리팹 풀 (활성/비활성 포함)
    private readonly List<GameObject> moneyPool = new List<GameObject>();

    private bool isProcessing;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (handcuffAnchor == null) handcuffAnchor = transform;
        if (moneyAnchor == null) moneyAnchor = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isProcessing) return;

        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        HandcuffsHoldStack holdStack = player.GetHandcuffsHoldStack();
        if (holdStack == null || holdStack.Count <= 0) return;

        if (moneyPrefab == null)
        {
            Debug.LogWarning("[HandcuffsMoneyExchangeZone] moneyPrefab 미설정");
            return;
        }

        // 보유 수갑 전체를 Zone 앵커로 이동
        List<GameObject> released = holdStack.ReleaseAll();
        foreach (GameObject hc in released)
        {
            if (hc == null) continue;
            hc.transform.SetParent(handcuffAnchor, false);
            zoneHandcuffs.Add(hc);

            // handcuffAnchor에 쌓인 Handcuffs는 플레이어가 다시 줍지 못하도록
            ItemPickup pickup = hc.GetComponent<ItemPickup>();
            if (pickup != null) pickup.SetPickupEnabled(false);
        }

        RebuildHandcuffTransforms();
        StartCoroutine(ProcessTurns());

        Debug.Log($"[HandcuffsMoneyExchangeZone] Handcuffs {released.Count}개 Zone으로 이동, 처리 시작");
    }

    // 한 턴마다 2 또는 4개씩 수갑 비활성화 + Money 순차 생성
    private IEnumerator ProcessTurns()
    {
        isProcessing = true;

        while (zoneHandcuffs.Count > 0)
        {
            int turnAmount = (Random.value < 0.5f) ? consumeOptionA : consumeOptionB;
            turnAmount = Mathf.Min(turnAmount, zoneHandcuffs.Count);

            for (int i = 0; i < turnAmount; i++)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, spawnInterval));

                // 마지막 수갑 비활성화
                int lastIdx = zoneHandcuffs.Count - 1;
                GameObject hc = zoneHandcuffs[lastIdx];
                zoneHandcuffs.RemoveAt(lastIdx);
                if (hc != null) hc.SetActive(false);

                // Money 1개 생성 (항상 현재 활성 Money 수 기준 y 슬롯)
                SpawnOneMoney();

                Debug.Log($"[HandcuffsMoneyExchangeZone] Handcuffs 비활성화, Money +1 생성 (남은 수갑: {zoneHandcuffs.Count})");
            }
        }

        isProcessing = false;
        Debug.Log("[HandcuffsMoneyExchangeZone] 전체 처리 완료");
    }

    private void SpawnOneMoney()
    {
        // 현재 활성 Money 수를 기준으로 Y 슬롯 결정 → 항상 y=0부터 쌓임
        int activeSlot = CountActiveMoney();

        GameObject instance = GetOrCreateMoney();
        instance.SetActive(true);

        Vector3 lp = moneyLocalOffset;
        lp.x = 0f;
        lp.z = 0f;
        lp.y = moneyLocalOffset.y + moneySpacingY * activeSlot;
        instance.transform.localPosition = lp;
        instance.transform.localRotation = Quaternion.identity;

        ItemPickup pickup = instance.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.Configure(ItemType.Money, 1, false);
        }
    }

    // 비활성화된 Money가 있으면 재사용, 없으면 새로 생성
    private GameObject GetOrCreateMoney()
    {
        for (int i = 0; i < moneyPool.Count; i++)
        {
            if (moneyPool[i] != null && !moneyPool[i].activeSelf)
            {
                moneyPool[i].transform.SetParent(moneyAnchor, false);
                return moneyPool[i];
            }
        }

        GameObject instance = Instantiate(moneyPrefab, moneyAnchor);
        instance.name = $"Money_Zone_{moneyPool.Count}";
        moneyPool.Add(instance);
        return instance;
    }

    private int CountActiveMoney()
    {
        int count = 0;
        for (int i = 0; i < moneyPool.Count; i++)
        {
            if (moneyPool[i] != null && moneyPool[i].activeSelf)
                count++;
        }
        return count;
    }

    private void RebuildHandcuffTransforms()
    {
        for (int i = 0; i < zoneHandcuffs.Count; i++)
        {
            GameObject hc = zoneHandcuffs[i];
            if (hc == null) continue;
            Vector3 lp = handcuffLocalOffset;
            lp.x = 0f;
            lp.z = 0f;
            lp.y = handcuffLocalOffset.y + handcuffSpacingY * i;
            hc.transform.localPosition = lp;
            hc.transform.localRotation = Quaternion.identity;
        }
    }
}
