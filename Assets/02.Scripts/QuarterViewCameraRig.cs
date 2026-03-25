using UnityEngine;

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

    private void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyCameraSettings();
    }

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

    private void ApplyCameraSettings()
    {
        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
    }
}
