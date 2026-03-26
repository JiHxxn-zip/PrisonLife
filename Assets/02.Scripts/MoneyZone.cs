using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 죄수 NPC 결제 완료 시 던져지는 Money 프리팹을 받아 스택에 쌓고,
// 플레이어가 진입하면 스택의 Money를 인벤토리로 수거한다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class MoneyZone : MonoBehaviour
{
    [Header("플레이어 수거 설정")]
    [Tooltip("Money 1개 수거 간격 (초)")]
    [SerializeField] private float collectInterval = 0.05f;

    [Header("Money 스택 배치")]
    [SerializeField] private Vector3 moneyLocalOffset = Vector3.zero;
    [SerializeField] private float   moneySpacingY    = 0.12f;

    [Header("Arc Flight (죄수 결제 연출)")]
    [SerializeField] private GameObject moneyPrefab;
    [Tooltip("포물선 최고점 높이")]
    [SerializeField] private float arcHeight         = 2.5f;
    [Tooltip("MoneyZone까지 비행 시간 (초)")]
    [SerializeField] private float arcFlightDuration = 0.6f;
    [Tooltip("연속 발사 시 각 Money 간 간격 (초)")]
    [SerializeField] private float launchStagger     = 0.12f;

    // ── 런타임 ────────────────────────────────────────────────

    // 단일 풀 — 비활성(inactive) = 재사용 가능, 활성(active) = 비행 중 또는 스택에 쌓인 상태
    private readonly List<GameObject> moneyPool = new List<GameObject>();

    private bool isCollecting;

    public event Action<PlayerAgent> OnPlayerEntered;

    // 스택에 쌓인(MoneyZone에 부모로 있는) 활성 Money 수
    public int ActiveMoneyCount => CountDepositedMoney();

    // ── 초기화 ────────────────────────────────────────────────

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    // HandcuffsMoneyExchangeZone.Awake()에서 호출 — 하위 호환 유지용 (현재 미사용)
    public void Initialize(HandcuffsMoneyExchangeZone zone) { }

    // ── 죄수 결제 연출 ────────────────────────────────────────

    // HandcuffsMoneyExchangeZone이 결제 완료 시 호출
    // count 만큼 worldPos에서 포물선으로 날아와 스택에 쌓임
    public void LaunchMoneyBatch(Vector3 worldPos, int count)
    {
        for (int i = 0; i < count; i++)
            StartCoroutine(LaunchWithDelay(worldPos, i * launchStagger));
    }

    private IEnumerator LaunchWithDelay(Vector3 worldPos, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (moneyPrefab == null)
        {
            Debug.LogWarning("[MoneyZone] moneyPrefab 미설정 — 연출 생략");
            yield break;
        }

        GameObject go = GetOrCreateMoney();
        go.transform.position = worldPos;
        go.transform.SetParent(null, true); // 비행 중 월드 공간 유지
        go.SetActive(true);

        StartCoroutine(ArcFlight(go, worldPos));
    }

    private IEnumerator ArcFlight(GameObject moneyGo, Vector3 from)
    {
        float   elapsed = 0f;
        Vector3 dest    = transform.position;

        while (elapsed < arcFlightDuration)
        {
            if (moneyGo == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / arcFlightDuration);

            // 포물선: 선형 보간 + sin 아치
            Vector3 pos = Vector3.Lerp(from, dest, t);
            pos.y += arcHeight * Mathf.Sin(t * Mathf.PI);
            moneyGo.transform.position = pos;

            // 비행 중 회전 연출
            moneyGo.transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);

            yield return null;
        }

        if (moneyGo != null)
            DepositMoney(moneyGo);
    }

    // 비행 완료한 Money를 MoneyZone에 쌓음
    private void DepositMoney(GameObject moneyGo)
    {
        // SetParent 전에 카운트 → 비행 중(parent == null)인 자신은 포함되지 않음
        int slot = CountDepositedMoney();

        moneyGo.transform.SetParent(transform, false);
        moneyGo.transform.localRotation = Quaternion.identity;

        Vector3 lp = moneyLocalOffset;
        lp.x = 0f;
        lp.z = 0f;
        lp.y = moneyLocalOffset.y + moneySpacingY * slot;
        moneyGo.transform.localPosition = lp;

        Debug.Log($"[MoneyZone] Money 쌓임 (스택={slot + 1})");
    }

    // ── 플레이어 수거 ─────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null) return;

        OnPlayerEntered?.Invoke(player);

        if (isCollecting || ActiveMoneyCount <= 0) return;

        StartCoroutine(CollectRoutine(player));
    }

    private IEnumerator CollectRoutine(PlayerAgent player)
    {
        isCollecting = true;

        while (ActiveMoneyCount > 0)
        {
            TakeTopMoney();
            player.CollectItem(ItemType.Money);

            yield return new WaitForSeconds(Mathf.Max(0f, collectInterval));
        }

        Debug.Log("[MoneyZone] Money 전체 수거 완료");
        isCollecting = false;
    }

    // LIFO: 스택 위에서부터 활성 Money 1개 비활성화
    public bool TakeTopMoney()
    {
        for (int i = moneyPool.Count - 1; i >= 0; i--)
        {
            GameObject m = moneyPool[i];
            if (m != null && m.activeSelf && m.transform.parent == transform)
            {
                m.SetActive(false);
                return true;
            }
        }
        return false;
    }

    // ── 내부 유틸 ─────────────────────────────────────────────

    // MoneyZone에 부모로 붙어 있는 활성 Money만 카운트 (비행 중인 것 제외)
    private int CountDepositedMoney()
    {
        int count = 0;
        for (int i = 0; i < moneyPool.Count; i++)
        {
            GameObject m = moneyPool[i];
            if (m != null && m.activeSelf && m.transform.parent == transform)
                count++;
        }
        return count;
    }

    private GameObject GetOrCreateMoney()
    {
        // 비활성 상태(재사용 가능) 탐색
        for (int i = 0; i < moneyPool.Count; i++)
        {
            if (moneyPool[i] != null && !moneyPool[i].activeSelf)
                return moneyPool[i];
        }

        // 없으면 새로 생성
        GameObject instance = Instantiate(moneyPrefab);
        instance.name = $"Money_Zone_{moneyPool.Count}";
        instance.SetActive(false);

        ItemPickup pickup = instance.GetComponent<ItemPickup>();
        if (pickup != null) pickup.SetPickupEnabled(false);

        moneyPool.Add(instance);
        return instance;
    }
}
