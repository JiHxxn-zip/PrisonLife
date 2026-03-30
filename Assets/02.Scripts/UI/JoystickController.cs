using UnityEngine;

// UIManager 하위에 위치하는 조이스틱 UI 컴포넌트.
// 드래그 입력을 처리하고 정규화된 입력 벡터를 InputVector 프로퍼티로 제공한다.
// 플레이어는 UIManager를 통해 매 프레임 이 값을 읽어 이동을 처리한다.
public class JoystickController : MonoBehaviour
{
    [SerializeField] private float dragMaxPixels = 120f;
    [SerializeField] private RectTransform joystickBg;
    [SerializeField] private RectTransform joystickStick;
    [SerializeField] private Canvas joystickCanvas;

    // 정규화된 입력 벡터 (-1 ~ 1). 손을 떼면 Vector2.zero
    public Vector2 InputVector { get; private set; }

    private Vector2 _dragDelta;
    private Vector2 _dragStartPos;
    private bool _isDragging;

    private void Awake()
    {
        SetVisible(false);
    }

    private void Update()
    {
        ReadInput();
        UpdateStickUI();
    }

    private void ReadInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    BeginDrag(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (_isDragging)
                        _dragDelta = touch.position - _dragStartPos;
                    break;
                default:
                    EndDrag();
                    return;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
                BeginDrag(Input.mousePosition);
            else if (Input.GetMouseButton(0) && _isDragging)
                _dragDelta = (Vector2)Input.mousePosition - _dragStartPos;
            else if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
                return;
            }
        }

        InputVector = Vector2.ClampMagnitude(_dragDelta / Mathf.Max(1f, dragMaxPixels), 1f);
    }

    private void BeginDrag(Vector2 screenPos)
    {
        _isDragging   = true;
        _dragStartPos = screenPos;
        _dragDelta    = Vector2.zero;
        PlaceBg(screenPos);
        SetVisible(true);
    }

    private void EndDrag()
    {
        _isDragging = false;
        _dragDelta  = Vector2.zero;
        InputVector = Vector2.zero;
        SetVisible(false);
    }

    private void PlaceBg(Vector2 screenPos)
    {
        if (joystickBg == null || joystickCanvas == null) return;

        Camera uiCam = joystickCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : joystickCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickCanvas.GetComponent<RectTransform>(), screenPos, uiCam, out Vector2 local);

        joystickBg.anchoredPosition = local;
    }

    private void UpdateStickUI()
    {
        if (joystickStick == null || joystickBg == null || !_isDragging) return;

        float radius = joystickBg.sizeDelta.x * 0.5f;
        joystickStick.anchoredPosition =
            Vector2.ClampMagnitude(_dragDelta / Mathf.Max(1f, dragMaxPixels), 1f) * radius;
    }

    private void SetVisible(bool visible)
    {
        if (joystickBg != null) joystickBg.gameObject.SetActive(visible);
        if (!visible && joystickStick != null) joystickStick.anchoredPosition = Vector2.zero;
    }
}
