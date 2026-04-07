using UnityEngine;

// 드래그 이동·쿼터뷰 기준 회전·존에서의 이동 잠금.
// Update  : 조이스틱 입력 → 월드 이동 벡터 계산
// FixedUpdate : Rigidbody.MovePosition / MoveRotation 으로 물리 충돌 보장
[RequireComponent(typeof(Rigidbody))]
public class HyperCasualPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationLerpSpeed = 12f;

    [Header("Tutorial Arrow")]
    [SerializeField] private Transform arrowPivot;
    [SerializeField] private Transform arrowSprite;

    private Rigidbody _rb;
    private Camera    _camera;
    private Vector3   _currentMoveDir;
    private Vector3   _lastMoveDir;
    private bool      _movementLocked;
    private bool      _started;

    public bool  HasMovementInput => _currentMoveDir.sqrMagnitude > 0.0001f;
    public float MoveSpeed        => moveSpeed;

    private void Awake()
    {
        _rb     = GetComponent<Rigidbody>();
        _camera = Camera.main;

        // 회전·Y축 위치는 코드로만 제어
        _rb.freezeRotation = true;
        _rb.constraints    = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        _rb.useGravity     = false;
        _rb.interpolation  = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        _started = true;
        TutorialManager.Instance?.RegisterArrow(arrowPivot, arrowSprite);
        CameraManager.Instance?.SetTarget(this.transform);
    }

    private void OnEnable()
    {
        if (!_started) return;

        TutorialManager.Instance?.RegisterArrow(arrowPivot, arrowSprite);
        CameraManager.Instance?.SetTarget(this.transform);
    }

    private void OnDisable()
    {
        TutorialManager.Instance?.RegisterArrow(null, null);
    }

    // 입력 계산 — 렌더 프레임마다 최신 값 유지
    private void Update()
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
    }

    // 물리 이동 + 회전 — FixedUpdate에서 Rigidbody API 사용
    private void FixedUpdate()
    {
        // 이동 (Y축 고정이므로 수평 속도만 설정)
        _rb.velocity = _currentMoveDir * moveSpeed;

        // 회전
        Vector3 lookDir = _currentMoveDir.sqrMagnitude > 0.0001f ? _currentMoveDir : _lastMoveDir;
        if (lookDir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, rotationLerpSpeed * Time.fixedDeltaTime));
    }

    // 존 상호작용 등 — 이동 허용/차단
    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
        if (locked)
        {
            _currentMoveDir    = Vector3.zero;
            _rb.velocity = Vector3.zero;
        }
    }

    // 스탯·업그레이드 반영용 이동 속도 설정
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }
}
