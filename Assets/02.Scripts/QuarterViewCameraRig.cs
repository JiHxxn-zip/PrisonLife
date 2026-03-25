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
        if (target == null)
        {
            return;
        }

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

    // 평행 투영·orthographicSize 고정
    private void ApplyCameraSettings()
    {
        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
    }
}
