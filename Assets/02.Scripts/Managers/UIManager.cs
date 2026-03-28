using System;
using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Serializable]
    public class ChapterUI
    {
        public string chapterName;
        public GameObject uiRoot;
    }

    [Header("Chapter UI")]
    [SerializeField] private ChapterUI[] chapters;
    [SerializeField] private int startChapter = 0;

    [Header("Popups")]
    [SerializeField] private ChapterClearPopup chapterClearPopup;

    [Header("Fade")]
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private float fadeDuration = 0.5f;

    private int _currentChapter = -1;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ShowChapter(startChapter);
    }

    // ── 챕터 UI ──────────────────────────────────────

    public void ShowChapter(int index)
    {
        if (index < 0 || index >= chapters.Length)
        {
            Debug.LogWarning($"[UIManager] 유효하지 않은 챕터 인덱스: {index}");
            return;
        }

        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i].uiRoot != null)
                chapters[i].uiRoot.SetActive(i == index);
        }

        _currentChapter = index;
    }

    public void ShowNextChapter()
    {
        ShowChapter(_currentChapter + 1);
    }

    public int CurrentChapter => _currentChapter;

    // ── 팝업 ─────────────────────────────────────────

    public void ShowChapterClearPopup()
    {
        if (chapterClearPopup != null)
            chapterClearPopup.Show();
    }

    public void HideChapterClearPopup()
    {
        if (chapterClearPopup != null)
            chapterClearPopup.Hide();
    }

    // ── Fade ─────────────────────────────────────────

    public IEnumerator FadeOut() => Fade(1f);
    public IEnumerator FadeIn()  => Fade(0f);

    private IEnumerator Fade(float targetAlpha)
    {
        if (screenFade == null) yield break;

        float startAlpha = screenFade.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            screenFade.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        screenFade.alpha = targetAlpha;
    }
}
