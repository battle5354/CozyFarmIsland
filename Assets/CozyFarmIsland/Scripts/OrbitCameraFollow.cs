using UnityEngine;

public class OrbitCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Rotation")]
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private float rotationSmoothTime = 0.05f;

    [Header("Zoom")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothTime = 0.08f;

    [Header("Follow")]
    [SerializeField] private float followSmoothTime = 0.05f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float cameraCollisionRadius = 0.2f;

    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;

    private float yawVelocity;
    private float pitchVelocity;

    private float targetDistance;
    private float currentDistance;
    private float distanceVelocity;
    private Vector3 followVelocity;

    private void OnValidate()
    {
        if (target == null)
        {
            Debug.LogWarning("OrbitCameraFollow: Target is not assigned!", this);
        }
    }
    private void Awake()
    {
        if (target == null)
        {
            var player = FindAnyObjectByType<PlayerInteractor>();

            if (player != null)
                target = player.transform;
        }

        if (target == null)
        {
            Debug.LogError("OrbitCameraFollow: Target is not assigned!", this);
            enabled = false;
        }
    }
    private void Start()
    {
        Vector3 angles = transform.eulerAngles;

        targetYaw = angles.y;
        currentYaw = angles.y;

        targetPitch = angles.x;
        currentPitch = angles.x;

        targetDistance = distance;
        currentDistance = distance;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleCursorLock();
        HandleRotation();
        HandleZoom();
        UpdateSmoothedValues();
        UpdateCameraPosition();
    }

    private void HandleCursorLock()
    {
        // Unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Lock cursor with middle mouse
        if (Input.GetMouseButtonDown(2))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleRotation()
    {
        if (!Input.GetMouseButton(1)) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        targetYaw += mouseX * mouseSensitivity;
        targetPitch -= mouseY * mouseSensitivity;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    private void UpdateSmoothedValues()
    {
        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref yawVelocity,
            rotationSmoothTime
        );

        currentPitch = Mathf.SmoothDampAngle(
            currentPitch,
            targetPitch,
            ref pitchVelocity,
            rotationSmoothTime
        );

        currentDistance = Mathf.SmoothDamp(
            currentDistance,
            targetDistance,
            ref distanceVelocity,
            zoomSmoothTime
        );
    }

    private bool HasObstacle(Vector3 focusPoint, Vector3 desiredPosition)
    {
        Vector3 direction = desiredPosition - focusPoint;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return false;

        direction /= distance;

        return Physics.SphereCast(
            focusPoint,
            cameraCollisionRadius,
            direction,
            out RaycastHit hit,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 focusPoint = target.position + targetOffset;
        Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * currentDistance;

        bool hasObstacle = HasObstacle(focusPoint, desiredPosition);

        Vector3 targetPosition;

        if (hasObstacle)
        {
            targetPosition = transform.position;

            // Prevent hidden rotation buildup while camera is blocked
            targetYaw = currentYaw;
            targetPitch = currentPitch;
        }
        else
        {
            targetPosition = desiredPosition;
        }

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime
        );

        transform.SetPositionAndRotation(smoothedPosition, rotation);
    }

    public Vector3 GetPlanarForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    public Vector3 GetPlanarRight()
    {
        Vector3 right = transform.right;
        right.y = 0f;
        return right.normalized;
    }
}