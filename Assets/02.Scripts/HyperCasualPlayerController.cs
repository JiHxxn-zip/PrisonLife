using UnityEngine;

// 드래그 이동·쿼터뷰 기준 회전·존에서의 이동 잠금
public class HyperCasualPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float dragMaxPixels = 120f;
    [SerializeField] private Camera gameplayCamera;

    [Header("Rotation")]
    [SerializeField] private float rotationLerpSpeed = 12f;
    [SerializeField] private Transform idleLookTarget;
    [SerializeField] private Vector3 idleLookDirection = Vector3.forward;
    [SerializeField] private float moveDetectionThreshold = 0.02f;

    private Vector2 dragDelta;
    private Vector2 dragStartPosition;
    private bool isDragging;
    private Vector3 currentMoveDirection;
    private bool isMovementLocked;

    public bool HasMovementInput => currentMoveDirection.sqrMagnitude > (moveDetectionThreshold * moveDetectionThreshold);
    public float MoveSpeed => moveSpeed;

    // 카메라 참조 보완, 카메라 전방을 기본 idle 바라보기 방향으로
    private void Awake()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera != null)
        {
            Vector3 cameraForwardOnPlane = Vector3.ProjectOnPlane(gameplayCamera.transform.forward, Vector3.up).normalized;
            if (cameraForwardOnPlane.sqrMagnitude > 0.0001f)
            {
                idleLookDirection = cameraForwardOnPlane;
            }
        }
    }

    // 드래그 입력 → 이동 → 회전 순 처리
    private void Update()
    {
        ReadDragInput();
        UpdateMovement();
        UpdateRotation();
    }

    // 터치 1개 또는 마우스로 드래그 델타 갱신
    private void ReadDragInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    isDragging = true;
                    dragStartPosition = touch.position;
                    dragDelta = Vector2.zero;
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging)
                    {
                        dragDelta = touch.position - dragStartPosition;
                    }
                    break;
                default:
                    isDragging = false;
                    dragDelta = Vector2.zero;
                    break;
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPosition = Input.mousePosition;
            dragDelta = Vector2.zero;
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            dragDelta = (Vector2)Input.mousePosition - dragStartPosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            dragDelta = Vector2.zero;
        }
    }

    // 카메라 평면 기준 이동 (잠금 시 속도 0)
    private void UpdateMovement()
    {
        if (gameplayCamera == null)
        {
            currentMoveDirection = Vector3.zero;
            return;
        }

        if (isMovementLocked)
        {
            currentMoveDirection = Vector3.zero;
            return;
        }

        Vector2 normalizedInput = Vector2.ClampMagnitude(dragDelta / Mathf.Max(1f, dragMaxPixels), 1f);
        Vector3 camRight = Vector3.ProjectOnPlane(gameplayCamera.transform.right, Vector3.up).normalized;
        Vector3 camForward = Vector3.ProjectOnPlane(gameplayCamera.transform.forward, Vector3.up).normalized;

        currentMoveDirection = (camRight * normalizedInput.x) + (camForward * normalizedInput.y);
        if (currentMoveDirection.sqrMagnitude > 1f)
        {
            currentMoveDirection.Normalize();
        }

        transform.position += currentMoveDirection * (moveSpeed * Time.deltaTime);
    }

    // 이동 중: 이동 방향, 정지: idle 타겟 또는 기본 방향
    private void UpdateRotation()
    {
        Vector3 lookDirection;
        bool isMoving = currentMoveDirection.sqrMagnitude > (moveDetectionThreshold * moveDetectionThreshold);

        if (isMoving)
        {
            lookDirection = currentMoveDirection;
        }
        else if (idleLookTarget != null)
        {
            lookDirection = idleLookTarget.position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                lookDirection = idleLookDirection;
            }
        }
        else
        {
            lookDirection = idleLookDirection;
        }

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
    }

    // 존 상호작용 등 — 드래그 이동 허용/차단
    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;
        if (locked)
        {
            dragDelta = Vector2.zero;
            isDragging = false;
            currentMoveDirection = Vector3.zero;
        }
    }

    // 스탯·업그레이드 반영용 이동 속도 설정
    public void SetMoveSpeed(float newMoveSpeed)
    {
        moveSpeed = Mathf.Max(0f, newMoveSpeed);
    }
}
