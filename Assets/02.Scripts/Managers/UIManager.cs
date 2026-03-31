using System.Collections;
using UnityEngine;

// 챕터 UI 통합 중계자.
// 현재 활성 챕터의 IChapterUI를 단일 변수로 참조하며,
// 외부(플레이어·게임 매니저)의 요청을 해당 구현체에 위임한다.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Joystick")]
    [SerializeField] private JoystickController joystick;

    public Vector2 JoystickInput => joystick != null ? joystick.InputVector : Vector2.zero;

    [Header("Chapter UI Roots")]
    [Tooltip("챕터 순서대로 배치. 각 루트에는 IChapterUI 구현 컴포넌트가 있어야 합니다.")]
    [SerializeField] private GameObject[] chapterRoots;
    [SerializeField] private int startChapter = 0;

    [Header("Fade")]
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private float       fadeDuration = 0.5f;

    private IChapterUI[] _chapterUIs;
    private IChapterUI   _currentUI;
    private int          _currentChapterIndex = -1;
    private int          _chapterMoney;

    // ── 초기화 ────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        CacheChapterUIs();
        ShowChapter(startChapter);
    }

    private void OnEnable()
    {
        Ch2MoneyPickup.OnMoneyCollected += HandleMoneyCollected;
    }

    private void OnDisable()
    {
        Ch2MoneyPickup.OnMoneyCollected -= HandleMoneyCollected;
    }

    private void CacheChapterUIs()
    {
        _chapterUIs = new IChapterUI[chapterRoots.Length];
        for (int i = 0; i < chapterRoots.Length; i++)
        {
            if (chapterRoots[i] != null)
                _chapterUIs[i] = chapterRoots[i].GetComponent<IChapterUI>();
        }
    }

    // ── 챕터 전환 ─────────────────────────────────────

    public void ShowChapter(int index)
    {
        // 유효성 검사를 먼저 — 실패 시 현재 챕터를 끄지 않음
        if (index < 0 || index >= chapterRoots.Length)
        {
            Debug.LogWarning($"[UIManager] 유효하지 않은 챕터 인덱스: {index} (등록된 챕터 수: {chapterRoots.Length})");
            return;
        }

        // 이전 챕터 비활성화
        if (_currentChapterIndex >= 0 && _currentChapterIndex < chapterRoots.Length)
        {
            _currentUI?.OnHide();
            if (chapterRoots[_currentChapterIndex] != null)
                chapterRoots[_currentChapterIndex].SetActive(false);
        }

        // 새 챕터 활성화
        _currentChapterIndex = index;
        _chapterMoney        = 0;

        if (chapterRoots[index] != null)
            chapterRoots[index].SetActive(true);

        _currentUI = _chapterUIs[index];
        _currentUI?.OnShow();
    }

    public void ShowNextChapter() => ShowChapter(_currentChapterIndex + 1);

    // 현재 챕터 UI만 숨김 — 다음 챕터 활성화는 별도 타이밍에 ShowNextChapter()로 처리
    public void HideCurrentChapter()
    {
        if (_currentChapterIndex < 0 || _currentChapterIndex >= chapterRoots.Length) return;
        _currentUI?.OnHide();
        if (chapterRoots[_currentChapterIndex] != null)
            chapterRoots[_currentChapterIndex].SetActive(false);
    }

    public int CurrentChapter => _currentChapterIndex;

    // ── Money ─────────────────────────────────────────

    // 외부(ItemPickup 등)에서 직접 호출
    public void AddMoney(int amount)
    {
        _chapterMoney += amount;
        _currentUI?.UpdateMoney(_chapterMoney);
    }

    // Ch2MoneyPickup 정적 이벤트 경로
    private void HandleMoneyCollected()
    {
        AddMoney(Ch2MoneyPickup.MoneyPerPickup);
    }

    // ── Chapter1 ──────────────────────────────────────

    public void SetChapter1MoneyText(string text)
    {
        if (_chapterUIs.Length > 0 && _chapterUIs[0] is Chapter1UI ch1)
            ch1.SetMoneyText(text);
    }

    // ── 클리어 팝업 ───────────────────────────────────

    // 현재 활성 챕터가 무엇이든 해당 챕터의 클리어 팝업을 표시
    public void ShowClear()             => _currentUI?.ShowClearPopup();
    public void HideClear()             => _currentUI?.HideClearPopup();

    // ── Fade ─────────────────────────────────────────

    public IEnumerator FadeOut() => Fade(1f);
    public IEnumerator FadeIn()  => Fade(0f);

    private IEnumerator Fade(float targetAlpha)
    {
        if (screenFade == null) yield break;

        float startAlpha = screenFade.alpha;
        float elapsed    = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed          += Time.deltaTime;
            screenFade.alpha  = Mathf.Lerp(startAlpha, targetAlpha,
                                    Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        screenFade.alpha = targetAlpha;
    }
}
