using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 진입 시:
//  1) 보유 Metal 비주얼 전체를 sellPos로 즉시 이동 (플레이어 인벤토리 비움)
//  2) processInterval(0.2s)마다 Metal 1개 비활성화 + Handcuffs 1개 생산 (동시)
//  HandcuffsMoneyExchangeZone과 동일한 구조
public class MetalExchangeZone : MonoBehaviour
{
    [Header("Metal Sell Position")]
    [SerializeField] private Transform sellPos;
    [SerializeField] private Vector3 metalLocalOffset = Vector3.zero;
    [SerializeField] private float metalSpacingY = 0.18f;

    [Header("Handcuffs Production")]
    [SerializeField] private GameObject handcuffsPrefab;
    [SerializeField] private Transform handcuffsAnchor;
    [SerializeField] private Vector3 handcuffsLocalOffset = Vector3.zero;
    [SerializeField] private float handcuffsSpacingY = 0.15f;

    [Header("Timing")]
    [Tooltip("Metal 1개 비활성화 + Handcuffs 1개 생산 간격 (초)")]
    [SerializeField] private float processInterval = 0.2f;

    // sellPos에 이동된 Metal 비주얼
    private readonly List<GameObject> zoneMetals = new List<GameObject>();
    // Handcuffs 오브젝트 풀
    private readonly List<GameObject> handcuffsPool = new List<GameObject>();
    // 수집 대기 중인 생산된 Handcuffs
    private readonly List<GameObject> producedHandcuffs = new List<GameObject>();

    private bool isProcessing;

    private void Awake()
    {
        if (sellPos == null) sellPos = transform;
        if (handcuffsAnchor == null) handcuffsAnchor = transform;
    }

    // MetalExchangeSellTrigger에서 호출
    public void RequestStartSelling(PlayerAgent player)
    {
        if (player == null || isProcessing) return;

        ItemStackInventory inventory = player.GetComponentInChildren<ItemStackInventory>();
        if (inventory == null || inventory.MetalCount <= 0) return;

        // 1) 플레이어 Metal 비주얼 전부 sellPos로 이전 (인벤토리에서 분리)
        List<GameObject> metals = inventory.ReleaseActiveMetal();
        if (metals.Count == 0) return;

        foreach (GameObject m in metals)
        {
            if (m == null) continue;
            m.transform.SetParent(sellPos, false);
            zoneMetals.Add(m);

            // sellPos에 올라온 Metal은 플레이어가 다시 줍지 못하도록
            ItemPickup pickup = m.GetComponent<ItemPickup>();
            if (pickup != null) pickup.SetPickupEnabled(false);
        }

        RebuildMetalTransforms();

        // 2) 스텝마다 Metal 1개 비활성화 + Handcuffs 1개 생산
        StartCoroutine(ProcessCycle());
        Debug.Log($"[MetalExchangeZone] Metal {metals.Count}개 sellPos 이전 완료, 처리 시작");
    }

    // 기존 호출 호환성 유지 (no-op)
    public void RequestStopSelling(PlayerAgent player) { }

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

        // 플레이어에게 이전됐으니 풀에서도 제거
        foreach (GameObject hc in producedHandcuffs)
        {
            handcuffsPool.Remove(hc);
        }

        producedHandcuffs.Clear();
    }

    // processInterval마다 Metal 1개 비활성화 + Handcuffs 1개 생산 (HandcuffsMoneyExchangeZone 동일 구조)
    private IEnumerator ProcessCycle()
    {
        isProcessing = true;

        while (zoneMetals.Count > 0)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, processInterval));

            // Metal 뒤에서부터 1개 비활성화
            int last = zoneMetals.Count - 1;
            if (zoneMetals[last] != null)
                zoneMetals[last].SetActive(false);
            zoneMetals.RemoveAt(last);

            // Handcuffs 1개 생산
            SpawnOneHandcuff();

            Debug.Log($"[MetalExchangeZone] Metal → Handcuffs 변환 (남은 Metal: {zoneMetals.Count})");
        }

        isProcessing = false;
        Debug.Log("[MetalExchangeZone] 전체 처리 완료");
    }

    private void SpawnOneHandcuff()
    {
        if (handcuffsPrefab == null) return;

        // 현재 쌓인 수 기준으로 y=0부터 슬롯 결정
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

    // 비활성 Handcuffs 재사용, 없으면 새로 생성
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

    private void OnDisable()
    {
        StopAllCoroutines();
        isProcessing = false;
    }
}
