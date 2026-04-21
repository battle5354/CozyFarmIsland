using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerMovement movement;

    [Header("Detection")]
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private SphereCollider detectionTrigger;
    [SerializeField] private float minLookDot = 0.35f;

    [Header("Score Weights")]
    [SerializeField][Range(0f, 1f)] private float distanceWeight = 0.4f;
    [SerializeField][Range(0f, 1f)] private float lookWeight = 0.6f;

    [Header("Selection Stability")]
    [SerializeField] private float switchThreshold = 0.1f;

    [Header("Carry")]
    [SerializeField] private Transform carryAnchor;

    [SerializeField] private float idleThreshold = 20f;

    private float idleTimer = 0f;

    private Animator animator;

    private static readonly int WaveHash = Animator.StringToHash("Wave");
    private static readonly int PickUpHash = Animator.StringToHash("PickUp");

    private GameObject carriedItem;

    public bool HasItem => carriedItem != null;

    private readonly List<IInteractable> nearbyInteractables = new();
    private IInteractable currentInteractable;
    private float currentScore = float.MinValue;
    private float interactRadius;
    private bool isWaving;

    // =========================
    // INITIALIZATION & UPDATE
    // =========================
    private void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (detectionTrigger == null)
            detectionTrigger = GetComponent<SphereCollider>();

        if (detectionTrigger != null)
            interactRadius = detectionTrigger.radius;

        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator not found on Player!", this);
        }
    }
    private void Update()
    {
        UpdateSelection();
        HandleInteractInput();
        HandleIdleWave();
        HandleWaveRotation();
    }

    // =========================
    // TRIGGER DETECTION
    // =========================
    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, interactableLayer))
            return;

        if (!TryResolveInteractable(other, out var interactable))
            return;

        if (!nearbyInteractables.Contains(interactable))
            nearbyInteractables.Add(interactable);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, interactableLayer))
            return;

        if (!TryResolveInteractable(other, out var interactable))
            return;

        if (ReferenceEquals(currentInteractable, interactable))
            ClearCurrentInteractable();

        nearbyInteractables.Remove(interactable);
    }

    // =========================
    // INTERACTION INPUT
    // =========================
    private void HandleInteractInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.LogError(currentInteractable != null
                ? $"Interacting with: {currentInteractable.GetType().Name}"
                : "No current interactable");
        }

        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact();
        }
    }

    public void PickUp(GameObject item)
    {
        Debug.LogError("Entered PickUP(item) method. Item = " + item + ", HasItem = " + HasItem + ", carryAnchor = " + carryAnchor + ". Animator = " + animator);
        if (item == null || HasItem || carryAnchor == null)
            return;

        ClearSelectionIfMatches(item);

        carriedItem = item;

        item.transform.SetParent(carryAnchor);
        item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (item.TryGetComponent<PickupItem>(out var pickupItem))
        {
            pickupItem.OnPickedUp();
        }

        if (animator != null)
            animator.SetTrigger(PickUpHash);
    }

    public void DropCarriedItem()
    {
        if (!HasItem)
            return;

        ClearSelectionIfMatches(carriedItem);
        UnregisterInteractable(carriedItem.GetComponent<IInteractable>());

        Destroy(carriedItem);
        carriedItem = null;
    }

    private void HandleIdleWave()
    {
        if (HasItem)
        {
            idleTimer = 0f;
            return;
        }

        bool isMoving =
            Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ||
            Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;

        bool isLooking =
            Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f ||
            Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f;

        bool isClicking =
            Input.GetMouseButton(0) ||
            Input.GetMouseButton(1) ||
            Input.GetKey(KeyCode.E) ||
            Input.GetKey(KeyCode.Space);

        if (isMoving || isLooking || isClicking)
        {
            idleTimer = 0f;
            StopWaving();
        }
        else
        {
            idleTimer += Time.deltaTime;

            if (idleTimer > idleThreshold)
            {
                idleTimer = 0f;
                StartWaving();
            }
        }
    }

    private void HandleWaveRotation()
    {
        if (!isWaving || cameraTransform == null)
            return;

        Vector3 direction = cameraTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            5f * Time.deltaTime
        );
    }

    private void StartWaving()
    {
        isWaving = true;

        if (movement != null)
            movement.LockRotation = true;

        if (animator != null)
            animator.SetTrigger(WaveHash);
    }

    private void StopWaving()
    {
        if (!isWaving)
            return;

        isWaving = false;

        if (movement != null)
            movement.LockRotation = false;
    }


    // =========================
    // SELECTION LOGIC
    // =========================
    private void UpdateSelection()
    {
        CleanupNearbyList();

        IInteractable bestInteractable = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < nearbyInteractables.Count; i++)
        {
            IInteractable interactable = nearbyInteractables[i];
            if (interactable == null)
                continue;

            if (!interactable.IsInteractionAvailable())
                continue;

            Transform interactionPoint = interactable.GetInteractionPoint();
            if (interactionPoint == null)
                continue;

            Vector3 toTargetFromCamera = interactionPoint.position - cameraTransform.position;
            if (toTargetFromCamera.sqrMagnitude <= 0.0001f)
                continue;

            Vector3 directionFromCamera = toTargetFromCamera.normalized;
            float lookDot = Vector3.Dot(cameraTransform.forward, directionFromCamera);

            if (lookDot < minLookDot)
                continue;

            float distance = Vector3.Distance(playerTransform.position, interactionPoint.position);
            if (distance > interactRadius)
                continue;

            float distanceScore = 1f - Mathf.Clamp01(distance / interactRadius);
            float finalScore = distanceScore * distanceWeight + lookDot * lookWeight;

            if (finalScore > bestScore)
            {
                bestScore = finalScore;
                bestInteractable = interactable;
            }
        }

        ApplySelection(bestInteractable, bestScore);
    }

    private void CleanupNearbyList()
    {
        for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            if (nearbyInteractables[i] == null)
                nearbyInteractables.RemoveAt(i);
        }
    }

    // =========================
    // SELECTION APPLICATION
    // =========================
    private void ApplySelection(IInteractable newInteractable, float newScore)
    {
        if (currentInteractable == null)
        {
            SetCurrentInteractable(newInteractable, newScore);
            return;
        }

        if (newInteractable == null)
        {
            ClearCurrentInteractable();
            return;
        }

        if (ReferenceEquals(currentInteractable, newInteractable))
        {
            currentScore = newScore;
            return;
        }

        if (newScore > currentScore + switchThreshold)
        {
            SetCurrentInteractable(newInteractable, newScore);
        }
    }

    // =========================
    // SELECTION STATE MANAGEMENT
    // =========================
    private void SetCurrentInteractable(IInteractable newInteractable, float newScore)
    {
        if (ReferenceEquals(currentInteractable, newInteractable))
        {
            currentScore = newScore;
            return;
        }

        if (currentInteractable != null)
            currentInteractable.SetSelected(false);

        currentInteractable = newInteractable;
        currentScore = newScore;

        if (currentInteractable != null)
            currentInteractable.SetSelected(true);
    }

    private void ClearCurrentInteractable()
    {
        if (currentInteractable != null)
            currentInteractable.SetSelected(false);

        currentInteractable = null;
        currentScore = float.MinValue;
    }

    // =========================
    // HELPERS
    // =========================
    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // Clear current selection before the object is reparented, picked up, or destroyed
    // to avoid keeping a stale interactable reference.
    private void ClearSelectionIfMatches(GameObject targetObject)
    {
        if (currentInteractable == null || targetObject == null)
            return;

        var targetInteractable = targetObject.GetComponent<IInteractable>();

        if (ReferenceEquals(currentInteractable, targetInteractable))
            ClearCurrentInteractable();
    }

    // Register newly spawned interactables immediately so they can be selected
    // even if the player is already standing inside the trigger area.
    public void RegisterInteractable(IInteractable interactable)
    {
        if (interactable == null)
            return;

        if (!nearbyInteractables.Contains(interactable))
            nearbyInteractables.Add(interactable);

        UpdateSelection();
    }

    public void UnregisterInteractable(IInteractable interactable)
    {
        ForceClearCurrentInteractable(interactable);
        nearbyInteractables.Remove(interactable);
    }

    public void ForceClearCurrentInteractable(IInteractable interactable)
    {
        if (interactable == null)
            return;

        if (ReferenceEquals(currentInteractable, interactable))
            ClearCurrentInteractable();
    }

    private bool TryResolveInteractable(Collider other, out IInteractable interactable)
    {
        interactable = null;

        if (other == null)
            return false;

        if (other.TryGetComponent<IInteractable>(out interactable))
            return true;

        interactable = other.GetComponentInParent<IInteractable>();
        return interactable != null;
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Transform center = playerTransform != null ? playerTransform : transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center.position, interactRadius);

        if (cameraTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * interactRadius);
        }
    }
}