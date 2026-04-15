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
    [SerializeField] private float groundGraceTime = 0.12f;
    [SerializeField] private float fallConfirmTime = 0.12f;
    [SerializeField] private float fallVelocityThreshold = -1.5f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;

    private CharacterController controller;

    // Values updated during gameplay
    private Vector3 velocity;
    private Vector3 moveDirection;
    private Vector3 horizontalMove;
    private float moveSpeed;
    private float jumpStartY;
    private float lastGroundedTime;
    private bool isOnGround;
    private bool wasOnGround;
    private bool isMoving;
    private bool isRunning;
    private bool isFalling;
    private float animatorVerticalVelocity;
    private float fallTime;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
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
        UpdateFallingState();
        UpdateAnimator();
    }

    private void HandleMovement()
    {
        // Get raw keyboard input without smoothing
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        isMoving = inputDirection.magnitude >= 0.1f;
        isRunning = isMoving && Input.GetKey(KeyCode.LeftShift);

        if (isMoving)
        {
            // MOVE DIRECTION
            // Take the camera forward (-forward) and right (-right) directions
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            // Reset Y to 0, so the character does not move into the ground or into the air
            camForward.y = 0f;
            camRight.y = 0f;

            // Normalize again because changing Y breaks the original vector length
            camForward.Normalize();
            camRight.Normalize();

            // Convert input into movement relative to the camera direction
            moveDirection = camForward * inputDirection.z + camRight * inputDirection.x;
            bool backwardDominant = vertical < -0.1f && Mathf.Abs(vertical) >= Mathf.Abs(horizontal);

            // FACING DIRECTION
            // Decide which direction the character should face
            Vector3 facingDirection;

            // Face opposite to movement so backward animation really looks backward
            facingDirection = backwardDominant ? -moveDirection : moveDirection;

            // SPEED
            // Choose walk or run speed and build horizontal movement vector
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            horizontalMove = moveDirection * currentSpeed;

            // ANIMATOR
            // Convert current movement into a normalized value for the Blend Tree
            // Final locomotion value for Animator: from -1 to 1
            float speedPercent = currentSpeed / runSpeed;
            float locomotionSign = backwardDominant ? -1f : 1f;
            moveSpeed = speedPercent * locomotionSign;

            // ROTATION
            if (facingDirection.sqrMagnitude > 0.001f)
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
            // Reset movement values when there is no input
            moveSpeed = 0f;
            horizontalMove = Vector3.zero;
            moveDirection = Vector3.zero;
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
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (animator != null)
                {
                    animator.ResetTrigger("JumpTrigger");
                    animator.SetTrigger("JumpTrigger");
                }
            }

            else if (canAirJump)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (animator != null)
                {
                    animator.ResetTrigger("JumpTrigger");
                    animator.SetTrigger("JumpTrigger");
                }
            }
        }

        // Apply gravity every frame so the player goes down after the jump
        velocity.y += gravity * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        // Combine horizontal and vertical movement
        Vector3 totalMove = horizontalMove + new Vector3(0f, velocity.y, 0f);
        // Move the character and update collisions/grounding
        controller.Move(totalMove * Time.deltaTime);
        // Grounded state becomes correct only after controller.Move()
        isOnGround = controller.isGrounded;

        if (isOnGround)
        {
            lastGroundedTime = Time.time;
        }
    }

    private void UpdateFallingState()
    {
        bool recentlyGrounded = Time.time - lastGroundedTime <= groundGraceTime;
        bool fallingCandidate = !recentlyGrounded && velocity.y < fallVelocityThreshold;

        if (fallingCandidate)
            fallTime += Time.deltaTime;
        else
            fallTime = 0f;

        isFalling = fallTime >= fallConfirmTime;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float animatorVerticalVelocity = isOnGround ? 0f : velocity.y;

        // Send the current player state to Animator parameters.
        // The Animator Controller uses these parameters to switch between animation states.
        animator.SetBool("IsOnGround", isOnGround);
        animator.SetBool("IsFalling", isFalling);
        animator.SetFloat("VerticalVelocity", animatorVerticalVelocity);
        animator.SetFloat("MoveSpeed", moveSpeed);
    }
}