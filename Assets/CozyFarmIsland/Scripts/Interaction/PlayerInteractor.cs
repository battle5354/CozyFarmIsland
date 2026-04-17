using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;

    [Header("Detection")]
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private SphereCollider detectionTrigger;
    [SerializeField] private float minLookDot = 0.35f;

    [Header("Score Weights")]
    [SerializeField][Range(0f, 1f)] private float distanceWeight = 0.4f;
    [SerializeField][Range(0f, 1f)] private float lookWeight = 0.6f;

    [Header("Selection Stability")]
    [SerializeField] private float switchThreshold = 0.1f;

    private readonly List<IInteractable> nearbyInteractables = new();
    private IInteractable currentInteractable;
    private float currentScore = float.MinValue;
    private float interactRadius;

    // =========================
    // INITIALIZATION & UPDATE
    // =========================

    private void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (detectionTrigger == null)
            detectionTrigger = GetComponent<SphereCollider>();

        if (detectionTrigger != null)
            interactRadius = detectionTrigger.radius;
    }

    private void Update()
    {
        UpdateSelection();
        HandleInteractInput();
    }

    // =========================
    // TRIGGER DETECTION
    // =========================

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger enter: " + other.name);

        if (!IsInLayerMask(other.gameObject.layer, interactableLayer))
            return;

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable == null)
            interactable = other.GetComponentInParent<IInteractable>();

        Debug.Log(interactable != null
            ? "Interactable found: " + other.name
            : "No IInteractable on: " + other.name);

        if (interactable == null)
            return;

        if (!nearbyInteractables.Contains(interactable))
            nearbyInteractables.Add(interactable);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, interactableLayer))
            return;

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable == null)
            interactable = other.GetComponentInParent<IInteractable>();

        if (interactable == null)
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
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact();
        }
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
            {
                Debug.Log("Null interactable in list");
                continue;
            }


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
            Debug.Log("Evaluating: " + interactable);
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