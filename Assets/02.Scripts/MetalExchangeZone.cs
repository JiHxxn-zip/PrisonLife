using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// Metal 판매(입금) + Handcuffs 생산/적치 + Handcuffs 수집(이동)까지 한 구역에서 관리
public class MetalExchangeZone : MonoBehaviour
{
    [Header("Production")]
    [SerializeField] private GameObject handcuffsPrefab;
    [SerializeField] private Transform handcuffsAnchor;
    [SerializeField] private Vector3 handcuffsLocalOffset = Vector3.zero;
    [SerializeField] private float handcuffsSpacingY = 0.15f;
    [SerializeField] private float handcuffsSpawnIntervalSeconds = 0.3f;

    [Header("Sell")]
    [SerializeField] private float metalRemoveIntervalSeconds = 0.1f;

    private readonly Dictionary<PlayerAgent, CancellationTokenSource> sellCtsByPlayer = new Dictionary<PlayerAgent, CancellationTokenSource>();

    // 판매된 Metal 개수만큼 생성해야 해서, 대기열로 쌓아둠
    private int pendingHandcuffsToSpawn;
    private bool spawnLoopRunning;
    private CancellationTokenSource spawnLoopCts;

    // 구역 바닥에 쌓인 Handcuffs 비주얼(수집 시 플레이어로 이동)
    private readonly List<GameObject> producedHandcuffs = new List<GameObject>();

    private readonly HashSet<PlayerAgent> activeSellingPlayers = new HashSet<PlayerAgent>();

    private void Awake()
    {
        if (handcuffsAnchor == null)
        {
            handcuffsAnchor = transform;
        }
    }

    public void RequestStartSelling(PlayerAgent player)
    {
        if (player == null)
        {
            return;
        }

        if (sellCtsByPlayer.ContainsKey(player))
        {
            return;
        }

        activeSellingPlayers.Add(player);

        CancellationTokenSource cts = new CancellationTokenSource();
        sellCtsByPlayer[player] = cts;
        _ = SellLoopAsync(player, cts.Token);

        StartSpawnLoopIfNeeded();
    }

    public void RequestStopSelling(PlayerAgent player)
    {
        if (player == null)
        {
            return;
        }

        activeSellingPlayers.Remove(player);

        if (sellCtsByPlayer.TryGetValue(player, out CancellationTokenSource cts))
        {
            sellCtsByPlayer.Remove(player);
            cts.Cancel();
            cts.Dispose();
        }
    }

    // Handcuffs 수집 트리거에서 호출
    public void CollectAllProducedHandcuffs(PlayerAgent player)
    {
        if (player == null)
        {
            return;
        }

        HandcuffsHoldStack holdStack = player.GetHandcuffsHoldStack();
        if (holdStack == null)
        {
            Debug.LogWarning("[MetalExchangeZone] 플레이어에 HandcuffsHoldStack이 없습니다.");
            return;
        }

        if (producedHandcuffs.Count == 0)
        {
            return;
        }

        holdStack.AddRange(producedHandcuffs);
        producedHandcuffs.Clear();
    }

    private void StartSpawnLoopIfNeeded()
    {
        if (spawnLoopRunning)
        {
            return;
        }

        if (handcuffsPrefab == null)
        {
            Debug.LogWarning("[MetalExchangeZone] handcuffsPrefab이 비어 있습니다.");
            return;
        }

        spawnLoopRunning = true;
        spawnLoopCts = new CancellationTokenSource();
        _ = SpawnLoopAsync(spawnLoopCts.Token);
    }

    private async Task SellLoopAsync(PlayerAgent player, CancellationToken token)
    {
        int waitMs = Mathf.CeilToInt(Mathf.Max(0.01f, metalRemoveIntervalSeconds) * 1000f);

        while (!token.IsCancellationRequested && activeSellingPlayers.Contains(player))
        {
            // LIFO: Metal을 하나 제거할 때마다 Handcuffs 생산 대기열 +1
            bool removed = player.TryRemoveLastItem(ItemType.Metal);
            if (!removed)
            {
                // 더 이상 제거할 Metal이 없음(또는 MAX 상태가 해제됨). 플레이어가 존에 있어도 루프를 종료.
                break;
            }

            lock (this)
            {
                pendingHandcuffsToSpawn++;
            }

            await DelayWithToken(waitMs, token);
        }
    }

    private async Task SpawnLoopAsync(CancellationToken token)
    {
        int waitMs = Mathf.CeilToInt(Mathf.Max(0.01f, handcuffsSpawnIntervalSeconds) * 1000f);

        while (!token.IsCancellationRequested)
        {
            bool shouldStop;
            int pending;
            lock (this)
            {
                pending = pendingHandcuffsToSpawn;
                shouldStop = activeSellingPlayers.Count == 0 && pendingHandcuffsToSpawn <= 0;
            }

            if (shouldStop)
            {
                break;
            }

            if (pending > 0)
            {
                lock (this)
                {
                    pendingHandcuffsToSpawn--;
                }

                SpawnOneHandcuffVisual();
                await DelayWithToken(waitMs, token);
            }
            else
            {
                // pending이 생길 때까지 약간 대기
                await DelayWithToken(50, token);
            }
        }

        spawnLoopRunning = false;
        spawnLoopCts?.Dispose();
        spawnLoopCts = null;
    }

    private void SpawnOneHandcuffVisual()
    {
        if (handcuffsPrefab == null || handcuffsAnchor == null)
        {
            return;
        }

        GameObject instance = Instantiate(handcuffsPrefab, handcuffsAnchor);
        int index = producedHandcuffs.Count;
        instance.transform.localPosition = handcuffsLocalOffset + new Vector3(0f, handcuffsSpacingY * index, 0f);
        instance.transform.localRotation = Quaternion.identity;

        producedHandcuffs.Add(instance);
    }

    private static async Task DelayWithToken(int milliseconds, CancellationToken token)
    {
        try
        {
            await Task.Delay(milliseconds, token);
        }
        catch (TaskCanceledException)
        {
            // canceled
        }
    }

    private void OnDisable()
    {
        foreach (var kv in sellCtsByPlayer)
        {
            kv.Value.Cancel();
            kv.Value.Dispose();
        }
        sellCtsByPlayer.Clear();

        spawnLoopCts?.Cancel();
        spawnLoopCts?.Dispose();
        spawnLoopCts = null;
        spawnLoopRunning = false;
    }
}

