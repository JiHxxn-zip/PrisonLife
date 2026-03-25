using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SellZone : BaseZone
{
    [Header("Sell")]
    [SerializeField] private int goldPerItem = 1;
    [SerializeField] private float sellTickInterval = 0.15f;

    private readonly Dictionary<PlayerAgent, CancellationTokenSource> tokenByPlayer = new Dictionary<PlayerAgent, CancellationTokenSource>();

    protected override void OnPlayerEnterZone(PlayerAgent player)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        tokenByPlayer[player] = cts;
        _ = SellLoopAsync(player, cts.Token);
    }

    protected override void OnPlayerExitZone(PlayerAgent player)
    {
        CancelLoop(player);
    }

    private async Task SellLoopAsync(PlayerAgent player, CancellationToken token)
    {
        int delayMs = Mathf.CeilToInt(Mathf.Max(0.05f, sellTickInterval) * 1000f);
        while (!token.IsCancellationRequested && playersInZone.Contains(player))
        {
            if (player == null)
            {
                break;
            }

            int reward = player.Inventory.SellAll(goldPerItem);
            if (reward > 0)
            {
                player.Stats.AddGold(reward);
                player.NotifyInventoryUpdated();
            }

            await DelayWithToken(delayMs, token);
        }
    }

    private static async Task DelayWithToken(int milliseconds, CancellationToken token)
    {
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
