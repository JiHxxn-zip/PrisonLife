using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class UpgradeZone : BaseZone
{
    private enum UpgradeType
    {
        MoveSpeed = 0,
        Capacity = 1,
        Attack = 2
    }

    [Header("Upgrade")]
    [SerializeField] private UpgradeType upgradeType = UpgradeType.MoveSpeed;
    [SerializeField] private int goldCost = 20;
    [SerializeField] private float upgradeTickInterval = 0.25f;
    [SerializeField] private float speedBonus = 0.2f;
    [SerializeField] private int capacityBonus = 5;
    [SerializeField] private int attackBonus = 1;

    private readonly Dictionary<PlayerAgent, CancellationTokenSource> tokenByPlayer = new Dictionary<PlayerAgent, CancellationTokenSource>();

    protected override void OnPlayerEnterZone(PlayerAgent player)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        tokenByPlayer[player] = cts;
        _ = UpgradeLoopAsync(player, cts.Token);
    }

    protected override void OnPlayerExitZone(PlayerAgent player)
    {
        CancelLoop(player);
    }

    private async Task UpgradeLoopAsync(PlayerAgent player, CancellationToken token)
    {
        int delayMs = Mathf.CeilToInt(Mathf.Max(0.05f, upgradeTickInterval) * 1000f);
        while (!token.IsCancellationRequested && playersInZone.Contains(player))
        {
            if (player == null)
            {
                break;
            }

            bool spent = player.Stats.TrySpendGold(goldCost);
            if (spent)
            {
                ApplyUpgrade(player);
                player.NotifyInventoryUpdated();
            }

            await DelayWithToken(delayMs, token);
        }
    }

    private void ApplyUpgrade(PlayerAgent player)
    {
        switch (upgradeType)
        {
            case UpgradeType.MoveSpeed:
                player.Stats.UpgradeSpeed(speedBonus);
                player.ApplyMoveSpeedFromStats();
                break;
            case UpgradeType.Capacity:
                player.Inventory.UpgradeAllCapacities(capacityBonus);
                break;
            case UpgradeType.Attack:
                player.Stats.UpgradeAttack(attackBonus);
                break;
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
