using UnityEngine;

// 각 플레이어 캐릭터에 부착.
// 활성화 시 자신의 ArrowPivot·ArrowSprite를 TutorialManager에 등록,
// 비활성화 시 등록 해제 → 캐릭터가 교체되어도 화살표가 자동 연결된다.
[DisallowMultipleComponent]
public class PlayerArrowAgent : MonoBehaviour
{
    [SerializeField] private Transform arrowPivot;
    [SerializeField] private Transform arrowSprite;

    private void OnEnable()
    {
        TutorialManager.Instance?.RegisterArrow(arrowPivot, arrowSprite);
    }

    private void OnDisable()
    {
        TutorialManager.Instance?.RegisterArrow(null, null);
    }
}
