using UnityEngine;

// 드래그 이동·쿼터뷰 기준 회전·존에서의 이동 잠금.
// JoystickController.OnMove 이벤트를 구독해 입력을 받는다.
public class HyperCasualPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationLerpSpeed = 12f;

    [Header("Tutorial Arrow")]
    [SerializeField] private Transform arrowPivot;
    [SerializeField] private Transform arrowSprite;

    private Camera _camera;
    private Vector3 _currentMoveDir;
    private Vector3 _lastMoveDir;
    private bool _movementLocked;
    private bool _started;

    public bool HasMovementInput => _currentMoveDir.sqrMagnitude > 0.0001f;
    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Start()
    {
        _started = true;
        TutorialManager.Instance?.RegisterArrow(arrowPivot, arrowSprite);
    }

    private void OnEnable()
    {
        if (_started) TutorialManager.Instance?.RegisterArrow(arrowPivot, arrowSprite);
    }

    private void OnDisable()
    {
        TutorialManager.Instance?.RegisterArrow(null, null);
    }

    private void Update()
    {
        UpdateMovement();
        UpdateRotation();
    }

    // 카메라 평면 기준 이동 (잠금 시 속도 0)
    private void UpdateMovement()
    {
        if (_camera == null || _movementLocked)
        {
            _currentMoveDir = Vector3.zero;
            return;
        }

        Vector2 input      = UIManager.Instance != null ? UIManager.Instance.JoystickInput : Vector2.zero;
        Vector3 camRight   = Vector3.ProjectOnPlane(_camera.transform.right,   Vector3.up).normalized;
        Vector3 camForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;

        _currentMoveDir = camRight * input.x + camForward * input.y;
        if (_currentMoveDir.sqrMagnitude > 1f)
            _currentMoveDir.Normalize();

        if (_currentMoveDir.sqrMagnitude > 0.0001f)
            _lastMoveDir = _currentMoveDir;

        transform.position += _currentMoveDir * (moveSpeed * Time.deltaTime);
    }

    // 이동 중: 이동 방향, 정지: 마지막 이동 방향 유지
    private void UpdateRotation()
    {
        Vector3 lookDir = _currentMoveDir.sqrMagnitude > 0.0001f ? _currentMoveDir : _lastMoveDir;
        if (lookDir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationLerpSpeed * Time.deltaTime);
    }

    // 존 상호작용 등 — 이동 허용/차단
    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
        if (locked)
            _currentMoveDir = Vector3.zero;
    }

    // 스탯·업그레이드 반영용 이동 속도 설정
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }
}
