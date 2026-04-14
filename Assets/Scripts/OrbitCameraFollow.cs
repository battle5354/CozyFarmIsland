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
    [SerializeField] private float rotationSmoothTime = 0.08f;

    [Header("Zoom")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothTime = 0.08f;

    [Header("Follow")]
    [SerializeField] private float followSmoothTime = 0.08f;

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
            Debug.LogError("Escape");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Lock cursor with middle mouse
        if (Input.GetMouseButtonDown(2))
        {
            Debug.LogError("Locked");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleRotation()
    {
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

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 focusPoint = target.position + targetOffset;
        Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * currentDistance;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            followSmoothTime
        );

        transform.rotation = rotation;
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