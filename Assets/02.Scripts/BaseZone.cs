using System.Collections.Generic;
using UnityEngine;

// Trigger 진입/이탈 시 플레이어 등록·해제 및 상호작용 시작/종료
[RequireComponent(typeof(Collider))]
public abstract class BaseZone : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private bool lockPlayerMovementWhileInteracting = false;

    protected readonly HashSet<PlayerAgent> playersInZone = new HashSet<PlayerAgent>();

    // Collider를 Trigger로 고정
    protected virtual void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    // 플레이어 감지 시 존에 등록하고 상호작용 시작
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

    // 플레이어가 나가면 등록 해제 및 상호작용 종료
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

    // 파생 클래스: 진입 시 루프 시작 등
    protected abstract void OnPlayerEnterZone(PlayerAgent player);
    // 파생 클래스: 이탈 시 비동기 루프 취소 등
    protected abstract void OnPlayerExitZone(PlayerAgent player);
}
