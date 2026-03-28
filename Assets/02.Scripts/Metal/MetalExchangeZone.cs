using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 플레이어: Metal 비주얼 전체를 sellPos로 이전 → processInterval마다 Metal 1개 비활성화 + Handcuffs 1개 생산
// NPC: LaunchMetalFrom() 호출 → Metal이 포물선으로 sellPos에 날아와 쌓임 → 동일하게 ProcessCycle 진행
public class MetalExchangeZone : MonoBehaviour
{
    [Header("Metal Sell Position")]
    [SerializeField] private Transform sellPos;
    [SerializeField] private Vector3 metalLocalOffset = Vector3.zero;
    [SerializeField] private float metalSpacingY = 0.5f;

    [Header("Handcuffs Production")]
    [SerializeField] private GameObject handcuffsPrefab;
    [SerializeField] private Transform handcuffsAnchor;
    [SerializeField] private Vector3 handcuffsLocalOffset = Vector3.zero;
    [SerializeField] private float handcuffsSpacingY = 0.5f;

    [Header("Stack Limit (레벨3 플레이어 최대 소지량 기준)")]
    [SerializeField] private int maxMetalStack = 14;
    [SerializeField] private int maxHandcuffsStack = 14;

    [Header("Max UI (World Space)")]
    [SerializeField] private TMP_Text metalMaxText;
    [SerializeField] private TMP_Text handcuffsMaxText;

    [Header("Timing")]
    [Tooltip("Metal 1개 비활성화 + Handcuffs 1개 생산 간격 (초)")]
    [SerializeField] private float processInterval = 0.2f;

    [Header("NPC Arc Flight")]
    [Tooltip("NPC가 날려 보내는 Metal 비주얼 프리팹 (CollectionZone Metal과 동일 프리팹 권장)")]
    [SerializeField] private GameObject metalPrefab;
    [Tooltip("포물선 최고점 높이")]
    [SerializeField] private float arcHeight = 2.5f;
    [Tooltip("sellPos까지 비행 시간 (초)")]
    [SerializeField] private float arcFlightDuration = 0.6f;

    [Header("Working Indicator")]
    [Tooltip("작업 중일 때 표시할 월드 UI 이미지 오브젝트")]
    [SerializeField] private GameObject workingIndicator;
    [Tooltip("두둥실 상하 진폭 (유닛)")]
    [SerializeField] private float bobAmplitude = 0.15f;
    [Tooltip("상하 왕복 속도")]
    [SerializeField] private float bobSpeed = 2f;
    [Tooltip("Z축 회전 속도 (도/초)")]
    [SerializeField] private float spinSpeed = 90f;

    // sellPos에 이동된 Metal 비주얼 (플레이어 판매 / NPC 납품 공통)
    private readonly List<GameObject> zoneMetals = new List<GameObject>();
    // Handcuffs 오브젝트 풀
    private readonly List<GameObject> handcuffsPool = new List<GameObject>();
    // 수집 대기 중인 생산된 Handcuffs
    private readonly List<GameObject> producedHandcuffs = new List<GameObject>();
    // NPC 포물선 비행용 Metal 풀 (sellPos 도달 후 zoneMetals로 이관)
    private readonly List<GameObject> flyingMetalPool = new List<GameObject>();

    private bool isProcessing;
    private Vector3 indicatorBaseLocalPos;
    private Coroutine indicatorCoroutine;

    public Transform SellPos => sellPos;

    private void Awake()
    {
        if (sellPos == null) sellPos = transform;
        if (handcuffsAnchor == null) handcuffsAnchor = transform;

        SetMaxTextActive(metalMaxText, false);
        SetMaxTextActive(handcuffsMaxText, false);

        if (workingIndicator != null)
        {
            indicatorBaseLocalPos = workingIndicator.transform.localPosition;
            workingIndicator.SetActive(false);
        }
    }

    // ── NPC 전용 ──────────────────────────────────────────────

    // NPC 수집 완료 시 호출 — 포물선으로 sellPos까지 날아와 쌓임
    public void LaunchMetalFrom(Vector3 worldPos)
    {
        if (metalPrefab == null)
        {
            Debug.LogWarning("[MetalExchangeZone] metalPrefab 미설정 — NPC 납품 연출 생략");
            return;
        }

        GameObject go = GetOrCreateFlyingMetal();
        go.transform.position = worldPos;
        go.transform.SetParent(null, true); // 비행 중 월드 공간 유지
        go.SetActive(true);

        StartCoroutine(ArcFlight(go, worldPos));
    }

    private IEnumerator ArcFlight(GameObject metalGo, Vector3 from)
    {
        float elapsed = 0f;

        while (elapsed < arcFlightDuration)
        {
            if (metalGo == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / arcFlightDuration);

            // 포물선: Lerp + sin 아치
            Vector3 pos = Vector3.Lerp(from, sellPos.position, t);
            pos.y += arcHeight * Mathf.Sin(t * Mathf.PI);
            metalGo.transform.position = pos;

            // 비행 중 회전 연출
            metalGo.transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);

            yield return null;
        }

        // sellPos 도달 → 정착 및 판매 큐에 추가
        if (metalGo != null)
            DepositNpcMetal(metalGo);
    }

    // 비행 완료한 Metal을 sellPos에 쌓고 판매 카운트 추가
    private void DepositNpcMetal(GameObject metalVisual)
    {
        if (metalVisual == null || sellPos == null) return;

        if (zoneMetals.Count >= maxMetalStack)
        {
            metalVisual.SetActive(false);
            SetMaxTextActive(metalMaxText, true);
            return;
        }

        metalVisual.transform.SetParent(sellPos, false);
        metalVisual.transform.localRotation = Quaternion.identity;

        Vector3 lp = metalLocalOffset;
        lp.x = 0f;
        lp.z = 0f;
        lp.y = metalLocalOffset.y + metalSpacingY * zoneMetals.Count;
        metalVisual.transform.localPosition = lp;

        ItemPickup pickup = metalVisual.GetComponent<ItemPickup>();
        if (pickup != null) pickup.SetPickupEnabled(false);

        zoneMetals.Add(metalVisual);

        if (!isProcessing)
            StartCoroutine(ProcessCycle());
    }

    private GameObject GetOrCreateFlyingMetal()
    {
        for (int i = 0; i < flyingMetalPool.Count; i++)
        {
            if (flyingMetalPool[i] != null && !flyingMetalPool[i].activeSelf)
                return flyingMetalPool[i];
        }

        GameObject go = Instantiate(metalPrefab);
        go.name = $"FlyingMetal_{flyingMetalPool.Count}";
        go.SetActive(false);

        ItemPickup pickup = go.GetComponent<ItemPickup>();
        if (pickup != null) pickup.SetPickupEnabled(false);

        flyingMetalPool.Add(go);
        return go;
    }

    // ── 플레이어 판매 ─────────────────────────────────────────

    // MetalExchangeSellTrigger에서 호출
    public void RequestStartSelling(PlayerAgent player)
    {
        if (player == null) return;

        ItemStackInventory inventory = player.GetComponentInChildren<ItemStackInventory>();
        if (inventory == null || inventory.MetalCount <= 0) return;

        // 플레이어 Metal 비주얼 전부 sellPos로 이전 (인벤토리에서 분리)
        List<GameObject> metals = inventory.ReleaseActiveMetal();
        if (metals.Count == 0) return;

        foreach (GameObject m in metals)
        {
            if (m == null) continue;

            if (zoneMetals.Count >= maxMetalStack)
            {
                m.SetActive(false);
                SetMaxTextActive(metalMaxText, true);
                continue;
            }

            m.transform.SetParent(sellPos, false);
            zoneMetals.Add(m);

            ItemPickup pickup = m.GetComponent<ItemPickup>();
            if (pickup != null) pickup.SetPickupEnabled(false);
        }

        RebuildMetalTransforms();

        if (!isProcessing)
            StartCoroutine(ProcessCycle());

        Debug.Log($"[MetalExchangeZone] Metal {metals.Count}개 sellPos 이전 완료, 처리 시작");
    }

    // 기존 호출 호환성 유지 (no-op)
    public void RequestStopSelling(PlayerAgent player) { }

    // ── NPC 전용 수거 API ─────────────────────────────────────

    public Transform HandcuffsAnchor => handcuffsAnchor;
    public int ProducedHandcuffsCount => producedHandcuffs.Count;

    // NpcDeliveryAgent가 호출 — producedHandcuffs를 NPC에게 넘기고 Zone 목록에서 제거
    public List<GameObject> CollectHandcuffsForNpc()
    {
        if (producedHandcuffs.Count == 0)
            return new List<GameObject>();

        List<GameObject> collected = new List<GameObject>(producedHandcuffs);
        foreach (GameObject hc in collected)
            handcuffsPool.Remove(hc);
        producedHandcuffs.Clear();
        SetMaxTextActive(handcuffsMaxText, false);
        return collected;
    }

    // ── 플레이어 Handcuffs 수집 ───────────────────────────────

    // MetalExchangeHandcuffsCollectTrigger에서 호출
    public void CollectAllProducedHandcuffs(PlayerAgent player)
    {
        if (player == null) return;

        HandcuffsHoldStack holdStack = player.GetHandcuffsHoldStack();
        if (holdStack == null)
        {
            Debug.LogWarning("[MetalExchangeZone] 플레이어에 HandcuffsHoldStack이 없습니다.");
            return;
        }

        if (producedHandcuffs.Count == 0) return;

        holdStack.AddRange(producedHandcuffs);

        foreach (GameObject hc in producedHandcuffs)
            handcuffsPool.Remove(hc);

        producedHandcuffs.Clear();
        SetMaxTextActive(handcuffsMaxText, false);
    }

    // ── 처리 사이클 ───────────────────────────────────────────

    private IEnumerator ProcessCycle()
    {
        isProcessing = true;
        SetWorkingIndicator(true);

        while (zoneMetals.Count > 0)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, processInterval));

            int last = zoneMetals.Count - 1;
            if (zoneMetals[last] != null)
                zoneMetals[last].SetActive(false);
            zoneMetals.RemoveAt(last);

            if (zoneMetals.Count < maxMetalStack)
                SetMaxTextActive(metalMaxText, false);

            SpawnOneHandcuff();

            Debug.Log($"[MetalExchangeZone] Metal → Handcuffs 변환 (남은: {zoneMetals.Count})");
        }

        isProcessing = false;
        SetWorkingIndicator(false);
    }

    private void SetWorkingIndicator(bool active)
    {
        if (workingIndicator == null) return;

        if (active)
        {
            workingIndicator.SetActive(true);
            if (indicatorCoroutine != null) StopCoroutine(indicatorCoroutine);
            indicatorCoroutine = StartCoroutine(AnimateIndicator());
        }
        else
        {
            if (indicatorCoroutine != null)
            {
                StopCoroutine(indicatorCoroutine);
                indicatorCoroutine = null;
            }
            workingIndicator.SetActive(false);
        }
    }

    private IEnumerator AnimateIndicator()
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime;

            // 상하 두둥실
            Vector3 lp = indicatorBaseLocalPos;
            lp.y += Mathf.Sin(time * bobSpeed) * bobAmplitude;
            workingIndicator.transform.localPosition = lp;

            // Z축 회전
            workingIndicator.transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.Self);

            yield return null;
        }
    }

    private void SpawnOneHandcuff()
    {
        if (handcuffsPrefab == null) return;

        if (producedHandcuffs.Count >= maxHandcuffsStack)
        {
            SetMaxTextActive(handcuffsMaxText, true);
            return;
        }

        int slot = producedHandcuffs.Count;
        GameObject instance = GetOrCreateHandcuff();
        instance.SetActive(true);

        Vector3 lp = handcuffsLocalOffset;
        lp.x = 0f;
        lp.z = 0f;
        lp.y = handcuffsLocalOffset.y + handcuffsSpacingY * slot;
        instance.transform.localPosition = lp;
        instance.transform.localRotation = Quaternion.identity;

        producedHandcuffs.Add(instance);
    }

    private GameObject GetOrCreateHandcuff()
    {
        for (int i = 0; i < handcuffsPool.Count; i++)
        {
            if (handcuffsPool[i] != null && !handcuffsPool[i].activeSelf)
            {
                handcuffsPool[i].transform.SetParent(handcuffsAnchor, false);
                return handcuffsPool[i];
            }
        }

        GameObject instance = Instantiate(handcuffsPrefab, handcuffsAnchor);
        instance.name = $"Handcuffs_Zone_{handcuffsPool.Count}";
        handcuffsPool.Add(instance);
        return instance;
    }

    private void RebuildMetalTransforms()
    {
        for (int i = 0; i < zoneMetals.Count; i++)
        {
            if (zoneMetals[i] == null) continue;
            Vector3 lp = metalLocalOffset;
            lp.x = 0f;
            lp.z = 0f;
            lp.y = metalLocalOffset.y + metalSpacingY * i;
            zoneMetals[i].transform.localPosition = lp;
            zoneMetals[i].transform.localRotation = Quaternion.identity;
        }
    }

    private void SetMaxTextActive(TMP_Text text, bool active)
    {
        if (text != null) text.gameObject.SetActive(active);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isProcessing = false;
        indicatorCoroutine = null;
        if (workingIndicator != null)
            workingIndicator.SetActive(false);
    }
}
