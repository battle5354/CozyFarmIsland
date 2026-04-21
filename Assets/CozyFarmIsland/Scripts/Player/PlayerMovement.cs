using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float groundedVerticalSpeed = -2.5f;
    [SerializeField] private float maxAirJumpHeightMultiplier = 1f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 moveDirection;
    private Vector3 horizontalMove;
    private float moveSpeed;
    private float jumpStartY;
    private bool isOnGround;
    private bool wasOnGround;
    private bool isMoving;
    private bool isRunning;
    public bool LockRotation { get; set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
            Debug.LogError("PlayerMovement: cameraTransform is not assigned and Camera.main not found!", this);
    }

    private void Start()
    {
        // Force CharacterController to update ground detection on first frame
        controller.Move(Vector3.down * 0.01f);
        isOnGround = controller.isGrounded;
    }

    private void Update()
    {
        // Store grounded state from previous frame for stable logic
        wasOnGround = isOnGround;

        HandleMovement();
        HandleGravityAndJump();
        ApplyMovement();
        UpdateAnimator();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        isMoving = inputDirection.magnitude >= 0.1f;
        isRunning = isMoving && Input.GetKey(KeyCode.LeftShift);

        if (isMoving)
        {
            // MOVE DIRECTION
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            moveDirection = camForward * inputDirection.z + camRight * inputDirection.x;
            bool backwardDominant = vertical < -0.1f && Mathf.Abs(vertical) >= Mathf.Abs(horizontal);

            // FACING DIRECTION
            // Face opposite to movement so backward animation really looks backward
            Vector3 facingDirection;
            facingDirection = backwardDominant ? -moveDirection : moveDirection;

            // SPEED
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            horizontalMove = moveDirection * currentSpeed;

            // ANIMATOR
            // Signed locomotion value for blend tree: backward = negative, forward = positive.
            float speedPercent = currentSpeed / runSpeed;
            float locomotionSign = backwardDominant ? -1f : 1f;
            moveSpeed = speedPercent * locomotionSign;

            // ROTATION
            if (!LockRotation && facingDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(facingDirection);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            moveSpeed = 0f;
            horizontalMove = Vector3.zero;
            moveDirection = Vector3.zero;
        }
    }

    private void PerformJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (animator != null)
        {
            animator.ResetTrigger("JumpTrigger");
            animator.SetTrigger("JumpTrigger");
        }
    }

    private void HandleGravityAndJump()
    {
        // Keep player "stuck" to ground instead of floating
        if (wasOnGround && velocity.y < 0f)
        {
            velocity.y = groundedVerticalSpeed;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool canGroundJump = wasOnGround;

            float maxAllowedHeight = jumpStartY + jumpHeight * maxAirJumpHeightMultiplier;
            bool belowHeightLimit = transform.position.y <= maxAllowedHeight;

            bool canAirJump = !wasOnGround && belowHeightLimit;
            if (canGroundJump)
            {
                jumpStartY = transform.position.y;
                PerformJump();
            }

            else if (canAirJump)
            {
                PerformJump();
            }
        }

        velocity.y += gravity * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        Vector3 totalMove = horizontalMove + new Vector3(0f, velocity.y, 0f);
        controller.Move(totalMove * Time.deltaTime);
        // Grounded state becomes correct only after controller.Move()
        isOnGround = controller.isGrounded;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float animatorVerticalVelocity = isOnGround ? 0f : velocity.y;

        animator.SetBool("IsOnGround", isOnGround);
        animator.SetFloat("VerticalVelocity", animatorVerticalVelocity);
        animator.SetFloat("MoveSpeed", moveSpeed);
    }
}