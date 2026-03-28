using UnityEngine;
using TMPro;

// World Space UI가 항상 메인 카메라를 정면으로 바라보게 하는 빌보드
// alwaysOnTop = true 시 자식 TMP_Text의 ZTest를 Always로 설정 → 3D 오브젝트에 가려지지 않음
public class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [Tooltip("true면 3D 오브젝트에 가려지지 않고 항상 위에 렌더링됩니다.")]
    [SerializeField] private bool alwaysOnTop = true;

    private void Awake()
    {
        if (!alwaysOnTop) return;

        // fontMaterial은 인스턴스 복사본을 반환하므로 프리팹 원본에 영향 없음
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            text.fontMaterial.SetFloat(
                ShaderUtilities.ShaderTag_ZTestMode,
                (float)UnityEngine.Rendering.CompareFunction.Always);
        }
    }

    private void LateUpdate()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 toCamera = cam.transform.position - transform.position;
        if (toCamera.sqrMagnitude < 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }
}

