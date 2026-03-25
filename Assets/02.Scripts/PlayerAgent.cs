using UnityEngine;

// 플레이어 입력/이동(컨트롤러) + 아이템 획득(등 뒤 스태킹) + 존 상호작용 연결
[DisallowMultipleComponent]
[RequireComponent(typeof(HyperCasualPlayerController))]
public class PlayerAgent : MonoBehaviour
{
    [Header("Progression")]
    [SerializeField] private int playerLevel = 1;

    public int Level => Mathf.Max(1, playerLevel);

    [Header("Item Stack")]
    [SerializeField] private ItemStackInventory itemStackInventory;

    [Header("Handcuffs Hold")]
    [SerializeField] private HandcuffsHoldStack handcuffsHoldStack;

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

        if (handcuffsHoldStack == null)
        {
            handcuffsHoldStack = GetComponentInChildren<HandcuffsHoldStack>();
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
        return CollectItems(itemType, count) > 0;
    }

    // 아이템 여러 개 획득 시도 후, 실제로 쌓인 개수 반환
    public int CollectItems(ItemType itemType, int count = 1)
    {
        if (itemStackInventory == null)
        {
            Debug.LogWarning("[PlayerAgent] ItemStackInventory가 설정되어 있지 않습니다.");
            return 0;
        }

        int safeCount = Mathf.Max(0, count);
        int addedCount = 0;
        for (int i = 0; i < safeCount; i++)
        {
            bool added = itemStackInventory.TryAddItem(itemType);
            if (!added)
            {
                // MAX 도달 등으로 더 이상 못 넣는 경우 반복 호출도 의미가 없어서 종료
                break;
            }

            addedCount++;
        }

        return addedCount;
    }

    // 특정 타입 보유 개수 조회
    public int GetItemCount(ItemType itemType)
    {
        return itemStackInventory != null ? itemStackInventory.GetCountByType(itemType) : 0;
    }

    // 특정 타입을 가장 최근에 쌓인 것부터 1개 제거 (LIFO)
    public bool TryRemoveLastItem(ItemType itemType)
    {
        if (itemStackInventory == null)
        {
            return false;
        }

        return itemStackInventory.TryRemoveLastOfType(itemType);
    }

    public HandcuffsHoldStack GetHandcuffsHoldStack()
    {
        return handcuffsHoldStack;
    }

    public void AddLevel(int amount)
    {
        playerLevel = Mathf.Max(1, playerLevel + Mathf.Max(0, amount));
    }
}
