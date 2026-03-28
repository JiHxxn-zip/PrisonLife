using System.Collections;
using UnityEngine;

// Orthographic 쿼터뷰 + 타깃 추적
[RequireComponent(typeof(Camera))]
public class QuarterViewCameraRig : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float followSmooth = 10f;
    [SerializeField] private float distance = 16f;
    [SerializeField] private float height = 10f;

    [Header("View Angles")]
    [Range(30f, 45f)]
    [SerializeField] private float pitch = 35f;
    [SerializeField] private float yaw = 45f;

    [Header("Orthographic")]
    [SerializeField] private float orthographicSize = 7.5f;

    private Camera cam;
    private bool cinematicActive; // true인 동안 LateUpdate 위치 제어 중단

    // Camera 캐시 후 Orthographic 설정 적용
    private void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyCameraSettings();
    }

    // 인스펙터 값 변경 시 에디터에서도 투영 설정 동기화
    private void OnValidate()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        if (cam != null)
        {
            ApplyCameraSettings();
        }
    }

    // 타깃 뒤·위 오프셋으로 쿼터뷰 위치·회전 유지
    private void LateUpdate()
    {
        if (cinematicActive) return; // 시네마틱 코루틴이 위치를 직접 제어하는 동안 대기

        if (target == null) return;

        Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = viewRotation;

        Vector3 backOffset = -(transform.forward * distance);
        Vector3 desiredPosition = target.position + backOffset + (Vector3.up * height);

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            1f - Mathf.Exp(-followSmooth * Time.deltaTime)
        );
    }

    // 타겟을 변경하고 duration 초에 걸쳐 선형 lerp 이동. 완료 후 일반 추적으로 복귀
    public Coroutine StartCinematicLerp(Transform newTarget, float duration)
    {
        return StartCoroutine(CinematicLerpRoutine(newTarget, duration));
    }

    private IEnumerator CinematicLerpRoutine(Transform newTarget, float duration)
    {
        cinematicActive = true;

        Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = viewRotation;

        Vector3 backOffset = -(transform.forward * distance);
        Vector3 from = transform.position;
        Vector3 to   = newTarget.position + backOffset + Vector3.up * height;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;
        target = newTarget;   // 이후 LateUpdate 일반 추적 대상 변경
        cinematicActive = false;
    }

    public void SetTarget(Transform newTarget)
    {
        if (target != null) target.gameObject.SetActive(false);
        target = newTarget;
        if (target != null) target.gameObject.SetActive(true);
    }

    // 평행 투영·orthographicSize 고정
    private void ApplyCameraSettings()
    {
        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
    }
}
