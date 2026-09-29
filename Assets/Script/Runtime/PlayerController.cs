using UnityEngine;

public class MissionPlayerController : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float jumpPower = 6f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.22f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Camera viewCamera;
    #endregion

    #region 내부 변수
    private Rigidbody _body;
    private Animator _animator;
    private Vector3 _moveDirection;
    private bool _isGrounded;
    private bool _jumpInput;
    private float _moveAmount;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    #endregion

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();

        if (_body == null)
        {
            CPrint.Error("Rigidbody NULL => 캐릭터 객체 확인");
            return;
        }

        if (_animator == null)
        {
            CPrint.Error("Animator NULL => 캐릭터 객체 확인");
            return;
        }

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }
    }

    private void Update()
    {
        CheckGround();
        MoveInput();
        JumpInput();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Move();
        Jump();
    }

    private void CheckGround()
    {
        if (groundCheck == null)
        {
            return;
        }

        _isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void MoveInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;

        if (input.sqrMagnitude <= 0f)
        {
            _moveDirection = Vector3.zero;
            _moveAmount = 0f;
            return;
        }

        Vector3 cameraForward = viewCamera.transform.forward;
        Vector3 cameraRight = viewCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        _moveDirection = cameraForward * input.z + cameraRight * input.x;
        _moveDirection.Normalize();

        _moveAmount = Input.GetKey(KeyCode.LeftShift) ? 1f : 0.5f;
    }

    private void JumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            _jumpInput = true;
            _isGrounded = false;

            _animator.SetBool(GroundedHash, false);
            _animator.SetTrigger(JumpHash);
        }
    }

    private void Move()
    {
        if (_body == null)
        {
            return;
        }

        float speed = Input.GetKey(KeyCode.LeftShift)
            ? runSpeed
            : walkSpeed;

        Vector3 velocity = _body.velocity;

        velocity.x = _moveDirection.x * speed;
        velocity.z = _moveDirection.z * speed;

        _body.velocity = velocity;

        if (_moveDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime
        );
    }

    private void Jump()
    {
        if (!_jumpInput)
        {
            return;
        }

        _jumpInput = false;

        Vector3 velocity = _body.velocity;
        velocity.y = jumpPower;
        _body.velocity = velocity;
    }

    private void UpdateAnimator()
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetFloat(MoveSpeedHash, _moveAmount, 0.1f, Time.deltaTime);
        _animator.SetBool(GroundedHash, _isGrounded);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundRadius
        );
    }
}