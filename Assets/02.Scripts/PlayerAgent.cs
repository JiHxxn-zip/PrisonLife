using UnityEngine;

// 플레이어 입력/이동(컨트롤러) + 아이템 획득(등 뒤 스태킹) + 존 상호작용 연결
[DisallowMultipleComponent]
[RequireComponent(typeof(HyperCasualPlayerController))]
public class PlayerAgent : MonoBehaviour
{
    [Header("Item Stack")]
    [SerializeField] private ItemStackInventory itemStackInventory;

    private HyperCasualPlayerController movementController;
    private BaseZone currentInteractionZone;
    private bool isMovementLockedByZone;

    private void Awake()
    {
        movementController = GetComponent<HyperCasualPlayerController>();

        if (itemStackInventory == null)
        {
            itemStackInventory = GetComponentInChildren<ItemStackInventory>();
        }

        if (itemStackInventory != null)
        {
            itemStackInventory.Initialize();
        }
    }

    public void BeginInteraction(BaseZone zone, bool lockMovement)
    {
        currentInteractionZone = zone;
        isMovementLockedByZone = lockMovement;
        movementController.SetMovementLocked(isMovementLockedByZone);
    }

    public void EndInteraction(BaseZone zone)
    {
        if (currentInteractionZone == zone)
        {
            currentInteractionZone = null;
            isMovementLockedByZone = false;
            movementController.SetMovementLocked(false);
        }
    }

    // 외부(존/상호작용/아이템 오브젝트)가 호출하는 "획득" 진입점
    public bool CollectItem(ItemType itemType, int count = 1)
    {
        if (itemStackInventory == null)
        {
            Debug.LogWarning("[PlayerAgent] ItemStackInventory가 설정되어 있지 않습니다.");
            return false;
        }

        int safeCount = Mathf.Max(0, count);
        bool anyAdded = false;
        for (int i = 0; i < safeCount; i++)
        {
            bool added = itemStackInventory.TryAddItem(itemType);
            if (!added)
            {
                // MAX 도달 등으로 더 이상 못 넣는 경우 반복 호출도 의미가 없어서 종료
                break;
            }

            anyAdded = true;
        }

        return anyAdded;
    }
}
