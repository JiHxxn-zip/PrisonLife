using UnityEngine;
using UnityEngine.UI;

// 뷰 컴포넌트. World Space Canvas에 부착.
// 부모 계층의 HpComponent를 찾아 OnHPChanged를 구독하여 Slider를 갱신.
// BillboardToCamera 컴포넌트와 함께 카메라를 향해 자동 회전(빌보드 효과).
// 데이터 계층(HpComponent)과의 통신은 오직 이벤트로만 수행 — 직접 폴링 없음.
[RequireComponent(typeof(BillboardToCamera))]
[RequireComponent(typeof(CanvasGroup))]
public class OverheadHpBar : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [Tooltip("최대 HP일 때 HP 바를 투명하게 숨깁니다.")]
    [SerializeField] private bool hideWhenFull = true;

    private HpComponent _hp;
    private CanvasGroup _canvasGroup;

    // ── 초기화 ────────────────────────────────────────

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        // 부모 계층에서 HpComponent를 자동 탐색 (플레이어·몬스터 루트에 부착된 컴포넌트)
        _hp = GetComponentInParent<HpComponent>();
        if (_hp == null)
        {
            Debug.LogWarning($"[OverheadHpBar] 부모 계층에 HpComponent가 없습니다: {name}", this);
            return;
        }

        _hp.OnHPChanged += Refresh;
        Refresh(_hp.CurrentHp, _hp.MaxHp); // 초기값 즉시 반영
    }

    private void OnDestroy()
    {
        if (_hp != null)
            _hp.OnHPChanged -= Refresh;
    }

    // ── 갱신 (이벤트 수신) ────────────────────────────

    private void Refresh(int current, int max)
    {
        if (hpSlider != null)
            hpSlider.value = max > 0 ? (float)current / max : 0f;

        // CanvasGroup.alpha로 가시성 조절 — SetActive 없이 처리해 풀링 재활성화와 호환
        if (hideWhenFull)
            _canvasGroup.alpha = current < max ? 1f : 0f;
    }
}
