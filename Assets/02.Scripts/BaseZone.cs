using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class BaseZone : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private bool lockPlayerMovementWhileInteracting = false;

    protected readonly HashSet<PlayerAgent> playersInZone = new HashSet<PlayerAgent>();

    protected virtual void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null || playersInZone.Contains(player))
        {
            return;
        }

        playersInZone.Add(player);
        player.BeginInteraction(this, lockPlayerMovementWhileInteracting);
        OnPlayerEnterZone(player);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerAgent player = other.GetComponentInParent<PlayerAgent>();
        if (player == null || !playersInZone.Remove(player))
        {
            return;
        }

        OnPlayerExitZone(player);
        player.EndInteraction(this);
    }

    protected abstract void OnPlayerEnterZone(PlayerAgent player);
    protected abstract void OnPlayerExitZone(PlayerAgent player);
}
