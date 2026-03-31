using System.Collections;
using UnityEngine;

// Orthographic 쿼터뷰 카메라 싱글톤.
// 카메라 위치 = target.position - forward * distance
// pitch 각도가 자연스럽게 높이를 만들어주므로 별도 height 오프셋 없이 타겟이 화면 중앙에 위치한다.
[RequireComponent(typeof(Camera))]
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float followSmooth = 10f;
    [SerializeField] private float distance     = 16f;

    [Header("View Angles")]
    [Range(30f, 45f)]
    [SerializeField] private float pitch = 35f;
    [SerializeField] private float yaw   = 45f;

    [Header("Orthographic")]
    [SerializeField] private float orthographicSize = 7.5f;

    private Camera _cam;
    private bool   _cinematicActive;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _cam = GetComponent<Camera>();
        ApplyCameraSettings();
    }

    private void OnValidate()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        if (_cam != null) ApplyCameraSettings();
    }

    private void LateUpdate()
    {
        if (_cinematicActive || target == null) return;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 desiredPosition = target.position - transform.forward * distance;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            1f - Mathf.Exp(-followSmooth * Time.deltaTime)
        );
    }

    // ── 타겟 변경 ─────────────────────────────────────────────

    public void SetTarget(Transform newTarget)
    {
        if (target != null)
            target.gameObject.SetActive(false);

        target = newTarget;
    }

    // ── 시네마틱 Lerp ─────────────────────────────────────────

    public Coroutine StartCinematicLerp(Transform newTarget, float duration)
    {
        return StartCoroutine(CinematicLerpRoutine(newTarget, duration));
    }

    private IEnumerator CinematicLerpRoutine(Transform newTarget, float duration)
    {
        _cinematicActive = true;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 from = transform.position;
        Vector3 to   = newTarget.position - transform.forward * distance;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
        target             = newTarget;
        _cinematicActive   = false;
    }

    private void ApplyCameraSettings()
    {
        _cam.orthographic     = true;
        _cam.orthographicSize = orthographicSize;
    }
}
