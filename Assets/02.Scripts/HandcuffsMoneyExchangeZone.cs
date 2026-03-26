using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handcuffs → Money 변환 엔진
// Inspector에서 HandcuffZone / MoneyZone을 연결하면
// Awake에서 자동으로 각 Zone에 자신을 주입하고 앵커를 Zone 위치로 설정
[DisallowMultipleComponent]
public class HandcuffsMoneyExchangeZone : MonoBehaviour
{
    [Header("연결 Zone (앵커는 각 Zone의 위치를 자동 사용)")]
    [SerializeField] private HandcuffZone handcuffZone;
    [SerializeField] private MoneyZone moneyZone;

    [Header("Handcuff 적층 오프셋")]
    [SerializeField] private Vector3 handcuffLocalOffset = Vector3.zero;
    [SerializeField] private float handcuffSpacingY = 0.15f;

    [Header("Money Spawn")]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Vector3 moneyLocalOffset = Vector3.zero;
    [SerializeField] private float moneySpacingY = 0.12f;

    [Header("Consume Options (랜덤으로 둘 중 하나 선택)")]
    [SerializeField] private int consumeOptionA = 2;
    [SerializeField] private int consumeOptionB = 4;

    [Header("Timing")]
    [Tooltip("Handcuffs 비활성화 + Money 생성 간격 (초)")]
    [SerializeField] private float spawnInterval = 1f;

    // 앵커는 각 Zone의 transform으로 자동 설정
    private Transform handcuffAnchor;
    private Transform moneyAnchor;

    private readonly List<GameObject> zoneHandcuffs = new List<GameObject>();
    private readonly List<GameObject> moneyPool = new List<GameObject>();

    private bool isProcessing;

    // ── 공개 프로퍼티 ─────────────────────────────────────────

    public Transform HandcuffAnchorTransform => handcuffAnchor;
    public int ZoneHandcuffsCount => zoneHandcuffs.Count;
    public int ActiveMoneyCount => CountActiveMoney();

    // ── 초기화 ────────────────────────────────────────────────

    private void Awake()
    {
        // 앵커를 각 Zone의 위치로 자동 설정
        if (handcuffZone != null)
        {
            handcuffAnchor = handcuffZone.transform;
            handcuffZone.Initialize(this);
        }
        else
        {
            Debug.LogWarning("[HandcuffsMoneyExchangeZone] HandcuffZone 미연결");
            handcuffAnchor = transform;
        }

        if (moneyZone != null)
        {
            moneyAnchor = moneyZone.transform;
            moneyZone.Initialize(this);
        }
        else
        {
            Debug.LogWarning("[HandcuffsMoneyExchangeZone] MoneyZone 미연결");
            moneyAnchor = transform;
        }
    }

    // ── 플레이어 입력 (HandcuffZone → 호출) ──────────────────

    public void ReceiveHandcuffsFromPlayer(PlayerAgent player)
    {
        if (player == null) return;

        HandcuffsHoldStack holdStack = player.GetHandcuffsHoldStack();
        if (holdStack == null || holdStack.Count <= 0) return;

        if (moneyPrefab == null)
        {
            Debug.LogWarning("[HandcuffsMoneyExchangeZone] moneyPrefab 미설정");
            return;
        }

        List<GameObject> released = holdStack.ReleaseAll();
        foreach (GameObject hc in released)
        {
            if (hc == null) continue;
            hc.transform.SetParent(handcuffAnchor, false);
            zoneHandcuffs.Add(hc);

            ItemPickup pickup = hc.GetComponent<ItemPickup>();
            if (pickup != null) pickup.SetPickupEnabled(false);
        }

        RebuildHandcuffTransforms();

        if (!isProcessing)
            StartCoroutine(ProcessTurns());

        Debug.Log($"[HandcuffsMoneyExchangeZone] 플레이어 Handcuffs {released.Count}개 수령, 처리 시작");
    }

    // ── NPC 입력 (NpcDeliveryAgent → 호출) ───────────────────

    public void ReceiveHandcuffsFromNpc(List<GameObject> handcuffs)
    {
        if (handcuffs == null || handcuffs.Count == 0) return;

        if (moneyPrefab == null)
        {
            Debug.LogWarning("[HandcuffsMoneyExchangeZone] moneyPrefab 미설정 — NPC 전달 수락 불가");
            return;
        }

        foreach (GameObject hc in handcuffs)
        {
            if (hc == null) continue;
            hc.transform.SetParent(handcuffAnchor, false);
            zoneHandcuffs.Add(hc);

            ItemPickup pickup = hc.GetComponent<ItemPickup>();
            if (pickup != null) pickup.SetPickupEnabled(false);
        }

        RebuildHandcuffTransforms();

        if (!isProcessing)
            StartCoroutine(ProcessTurns());

        Debug.Log($"[HandcuffsMoneyExchangeZone] NPC로부터 Handcuffs {handcuffs.Count}개 수령, 처리 시작");
    }

    // ── Money 수거 (MoneyZone → 호출) ────────────────────────

    // 위에서부터(LIFO) 활성 Money 1개 비활성화. 성공하면 true 반환
    public bool TakeTopMoney()
    {
        for (int i = moneyPool.Count - 1; i >= 0; i--)
        {
            if (moneyPool[i] != null && moneyPool[i].activeSelf)
            {
                moneyPool[i].SetActive(false);
                return true;
            }
        }
        return false;
    }

    // ── 처리 사이클 ───────────────────────────────────────────

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

                int lastIdx = zoneHandcuffs.Count - 1;
                GameObject hc = zoneHandcuffs[lastIdx];
                zoneHandcuffs.RemoveAt(lastIdx);
                if (hc != null) hc.SetActive(false);

                SpawnOneMoney();

                Debug.Log($"[HandcuffsMoneyExchangeZone] Handcuffs → Money 변환 (남은 수갑: {zoneHandcuffs.Count})");
            }
        }

        isProcessing = false;
        Debug.Log("[HandcuffsMoneyExchangeZone] 전체 처리 완료");
    }

    // ── 내부 유틸 ─────────────────────────────────────────────

    private void SpawnOneMoney()
    {
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
            pickup.Configure(ItemType.Money, 1, false);
    }

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
