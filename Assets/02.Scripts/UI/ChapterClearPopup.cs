using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChapterClearPopup : MonoBehaviour
{
    public event Action OnHidden;
    [Header("Logo")]
    [SerializeField] private RectTransform logoRect;

    [Header("Bounce Settings")]
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Background Button")]
    [SerializeField] private Button backgroundButton;

    private Vector3 _logoOriginScale;
    private Coroutine _bounceCoroutine;

    private void Awake()
    {
        if (logoRect != null)
            _logoOriginScale = logoRect.localScale;

        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        if (_bounceCoroutine != null)
            StopCoroutine(_bounceCoroutine);

        _bounceCoroutine = StartCoroutine(PlayBounce());
    }

    private IEnumerator PlayBounce()
    {
        if (logoRect == null) yield break;

        logoRect.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bounceDuration);
            float scale = bounceCurve.Evaluate(t);
            logoRect.localScale = _logoOriginScale * scale;
            yield return null;
        }

        logoRect.localScale = _logoOriginScale;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        OnHidden?.Invoke();
    }
}
