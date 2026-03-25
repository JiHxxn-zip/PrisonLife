using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class HarvestZone : BaseZone
{
    [Header("Harvest")]
    [SerializeField] private ResourceType resourceType = ResourceType.Wood;
    [SerializeField] private int fallbackGiveAmount = 1;
    [SerializeField] private float fallbackIntervalSeconds = 0.35f;

    private readonly Dictionary<PlayerAgent, CancellationTokenSource> tokenByPlayer = new Dictionary<PlayerAgent, CancellationTokenSource>();

    protected override void OnPlayerEnterZone(PlayerAgent player)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        tokenByPlayer[player] = cts;
        _ = HarvestLoopAsync(player, cts.Token);
    }

    protected override void OnPlayerExitZone(PlayerAgent player)
    {
        CancelLoop(player);
    }

    private async Task HarvestLoopAsync(PlayerAgent player, CancellationToken token)
    {
        while (!token.IsCancellationRequested && playersInZone.Contains(player))
        {
            if (player == null)
            {
                break;
            }

            if (player.Inventory.IsResourceFull(resourceType))
            {
                player.NotifyInventoryUpdated();
                await DelayWithToken(120, token);
                continue;
            }

            GiveItem(player);

            float waitSeconds = fallbackIntervalSeconds;
            ResourceData data = player.Inventory.GetConfig(resourceType);
            if (data != null)
            {
                waitSeconds = data.GatherIntervalSeconds;
            }

            int waitMs = Mathf.CeilToInt(waitSeconds * 1000f);
            await DelayWithToken(waitMs, token);
        }
    }

    private void GiveItem(PlayerAgent player)
    {
        ResourceData data = player.Inventory.GetConfig(resourceType);
        int giveAmount = data != null ? data.GatherAmountPerTick : fallbackGiveAmount;
        bool added = player.Inventory.TryAddResource(resourceType, giveAmount);

        if (added)
        {
            player.NotifyInventoryUpdated();
            // TODO: resource object regen FX/spawn can be triggered here.
        }
    }

    private static async Task DelayWithToken(int milliseconds, CancellationToken token)
    {
        if (milliseconds <= 0)
        {
            return;
        }

        try
        {
            await Task.Delay(milliseconds, token);
        }
        catch (TaskCanceledException)
        {
            // zone exit cancellation
        }
    }

    private void CancelLoop(PlayerAgent player)
    {
        if (!tokenByPlayer.TryGetValue(player, out CancellationTokenSource cts))
        {
            return;
        }

        tokenByPlayer.Remove(player);
        cts.Cancel();
        cts.Dispose();
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<PlayerAgent, CancellationTokenSource> pair in tokenByPlayer)
        {
            pair.Value.Cancel();
            pair.Value.Dispose();
        }

        tokenByPlayer.Clear();
    }
}
