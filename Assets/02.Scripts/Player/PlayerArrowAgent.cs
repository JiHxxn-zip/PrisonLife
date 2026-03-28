using UnityEngine;

// 각 플레이어 캐릭터에 부착.
// 활성화 시 자신의 ArrowPivot·ArrowSprite를 TutorialManager에 등록,
// 비활성화 시 등록 해제 → 캐릭터가 교체되어도 화살표가 자동 연결된다.
//
// Start()  : 씬 시작 시 최초 등록 (모든 Awake 이후 실행 보장 → Instance 확실히 존재)
// OnEnable : Start 이후 재활성화(Player2 전환 등) 시 재등록
[DisallowMultipleComponent]
public class PlayerArrowAgent : MonoBehaviour
{
    [SerializeField] private Transform arrowPivot;
    [SerializeField] private Transform arrowSprite;

    private bool _started;

    private void Start()
    {
        _started = true;
        TutorialManager.Instance?.RegisterArrow(arrowPivot, arrowSprite);
    }

    private void OnEnable()
    {
        // Start 실행 전이면 Instance가 없을 수 있으므로 Start에서 처리
        if (!_started) return;
        TutorialManager.Instance?.RegisterArrow(arrowPivot, arrowSprite);
    }

    private void OnDisable()
    {
        TutorialManager.Instance?.RegisterArrow(null, null);
    }
}
