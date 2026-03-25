using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(HyperCasualPlayerController))]
public class PlayerAgent : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private Inventory inventory = new Inventory();
    [SerializeField] private PlayerStats stats = new PlayerStats();

    [Header("Full State")]
    [SerializeField] private bool showFullUi;
    [SerializeField] private Transform sellZoneHintTarget;

    private HyperCasualPlayerController movementController;
    private BaseZone currentInteractionZone;
    private bool isMovementLockedByZone;

    public event Action<PlayerStateType> OnStateChanged;
    public event Action<bool> OnFullUiChanged;

    public PlayerStateType State { get; private set; } = PlayerStateType.IdleMove;
    public Inventory Inventory => inventory;
    public PlayerStats Stats => stats;
    public bool IsFull => State == PlayerStateType.Full;
    public Transform SellZoneHintTarget => sellZoneHintTarget;

    private void Awake()
    {
        movementController = GetComponent<HyperCasualPlayerController>();
        inventory.Initialize();
        movementController.SetMoveSpeed(stats.MoveSpeed);
        RefreshState();
    }

    public void BeginInteraction(BaseZone zone, bool lockMovement)
    {
        currentInteractionZone = zone;
        isMovementLockedByZone = lockMovement;
        movementController.SetMovementLocked(isMovementLockedByZone);
        RefreshState();
    }

    public void EndInteraction(BaseZone zone)
    {
        if (currentInteractionZone == zone)
        {
            currentInteractionZone = null;
            isMovementLockedByZone = false;
            movementController.SetMovementLocked(false);
            RefreshState();
        }
    }

    public void NotifyInventoryUpdated()
    {
        RefreshState();
    }

    public void ApplyMoveSpeedFromStats()
    {
        movementController.SetMoveSpeed(stats.MoveSpeed);
    }

    private void RefreshState()
    {
        PlayerStateType nextState;
        if (inventory.IsAnyResourceFull())
        {
            nextState = PlayerStateType.Full;
        }
        else if (currentInteractionZone != null)
        {
            nextState = PlayerStateType.Interacting;
        }
        else
        {
            nextState = PlayerStateType.IdleMove;
        }

        bool shouldShowFull = nextState == PlayerStateType.Full;
        if (showFullUi != shouldShowFull)
        {
            showFullUi = shouldShowFull;
            OnFullUiChanged?.Invoke(showFullUi);
        }

        if (State != nextState)
        {
            State = nextState;
            OnStateChanged?.Invoke(State);
        }
    }
}
