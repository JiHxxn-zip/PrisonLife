using UnityEngine;

// 플레이어 자식으로 배치. 레벨별 크기가 다른 메탈 수집 콜라이더 오브젝트를 관리.
// MetalCollectionZone 진입 시 현재 레벨에 맞는 오브젝트만 활성화, 퇴장 시 전부 비활성화.
[DisallowMultipleComponent]
public class PlayerMetalCollector : MonoBehaviour
{
    [Header("레벨별 메탈 수집 콜라이더 (인덱스 0=레벨1, 1=레벨2, 2=레벨3)")]
    [SerializeField] private GameObject[] levelColliders = new GameObject[3];

    private PlayerAgent playerAgent;
    private bool isActive;

    private void Awake()
    {
        playerAgent = GetComponentInParent<PlayerAgent>();
        DeactivateAll();
    }

    public void ActivateForCurrentLevel()
    {
        isActive = true;
        DeactivateAll();

        int level = playerAgent != null ? playerAgent.Level : 1;
        int index = Mathf.Clamp(level - 1, 0, levelColliders.Length - 1);

        if (levelColliders[index] != null)
            levelColliders[index].SetActive(true);
    }

    // 레벨업 시 호출 — 존 안에 있는 경우에만 콜라이더 교체
    public void OnLevelUp()
    {
        if (isActive)
            ActivateForCurrentLevel();
    }

    public void Deactivate()
    {
        isActive = false;
        DeactivateAll();
    }

    private void DeactivateAll()
    {
        for (int i = 0; i < levelColliders.Length; i++)
        {
            if (levelColliders[i] != null)
                levelColliders[i].SetActive(false);
        }
    }
}
