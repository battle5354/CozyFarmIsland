using System.Collections;
using UnityEngine;

public class CropPlot : MonoBehaviour, IInteractable
{
    private const float ACTION_TIME = 4f;
    private const float GROW_TIME = 7f;
    public enum CropState
    {
        Empty,
        Stage01,
        NeedsWater1,
        Stage02,
        NeedsWater2,
        Ready,
        AwaitingPickup,
    }

    [Header("Crop Visuals")]
    [SerializeField] private GameObject stage01;
    [SerializeField] private GameObject stage02;
    [SerializeField] private GameObject stage03;

    [Header("Tools")]
    [SerializeField] private Transform toolAnchor;
    [SerializeField] private GameObject shovelPrefab;
    [SerializeField] private GameObject wateringCanPrefab;
    [SerializeField] private GameObject sicklePrefab;

    [Header("UI")]
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private RadialTimerUI actionTimer;
    [SerializeField] private RadialTimerUI growTimer;

    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;

    [Header("Harvest")]
    [SerializeField] private Transform pickupSpawnPoint;
    [SerializeField] private GameObject pickupPrefab;

    private CropState currentState = CropState.Empty;
    private PlayerInteractor playerInteractor;

    private GameObject currentToolInstance;
    private bool isBusy = false;
    private bool isSelected = false;

    // =========================
    // INITIALIZATION & VALIDATION
    // =========================
    private void Awake()
    {
        if (toolAnchor == null)
            Debug.LogError("ToolAnchor is missing!", this);

        if (stage01 == null || stage02 == null || stage03 == null)
            Debug.LogError("Crop stages are not fully assigned!", this);

        if (interactPrompt == null)
            Debug.LogError("InteractPrompt is missing!", this);

        if (actionTimer == null)
            Debug.LogError("RadialTimerUI is missing!", this);

        if (pickupSpawnPoint == null)
            Debug.LogError("PickupSpawnPoint is missing!", this);

        if (pickupPrefab == null)
            Debug.LogError("Pickup prefab is missing!", this);

        if (interactionPoint == null)
            Debug.LogError("InteractionPoint is not assigned. CropPlot transform will be used instead.", this);
    }

    private void Start()
    {
        playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        SetState(CropState.Empty);
        RefreshSelectionUI();
    }

    // =========================
    // PUBLIC INTERACTION
    // =========================
    public void Interact()
    {
        if (isBusy) return;

        if (playerInteractor != null && playerInteractor.HasItem)
            return;

        switch (currentState)
        {
            case CropState.Empty:
                StartCoroutine(PlantRoutine());
                break;

            case CropState.NeedsWater1:
            case CropState.NeedsWater2:
                StartCoroutine(WaterRoutine());
                break;

            case CropState.Ready:
                StartCoroutine(HarvestRoutine());
                break;
        }
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected)
            return;

        isSelected = selected;
        RefreshSelectionUI();
    }

    private bool CanInteractInCurrentState()
    {
        return currentState == CropState.Empty
            || currentState == CropState.NeedsWater1
            || currentState == CropState.NeedsWater2
            || currentState == CropState.Ready;
    }

    public Transform GetInteractionPoint()
    {
        return interactionPoint != null ? interactionPoint : transform;
    }

    // =========================
    // ROUTINES
    // =========================
    private IEnumerator PlantRoutine()
    {
        yield return StartCoroutine(PerformTimedAction(ACTION_TIME, () =>
        {
            SetState(CropState.Stage01);
            StartCoroutine(GrowRoutine(GROW_TIME, CropState.NeedsWater1));
        }));
    }

    private IEnumerator WaterRoutine()
    {
        CropState stateBeforeAction = currentState;

        yield return StartCoroutine(PerformTimedAction(ACTION_TIME, () =>
        {
            if (stateBeforeAction == CropState.NeedsWater1)
            {
                SetState(CropState.Stage02);
                StartCoroutine(GrowRoutine(GROW_TIME, CropState.NeedsWater2));
            }
            else if (stateBeforeAction == CropState.NeedsWater2)
            {
                SetState(CropState.Ready);
            }
        }));
    }

    private IEnumerator HarvestRoutine()
    {
        yield return StartCoroutine(PerformTimedAction(ACTION_TIME, () =>
        {
            SetState(CropState.AwaitingPickup);

            // Clear plot selection before spawning pickup so interaction can switch cleanly
            // from the plot to the harvested item.
            if (playerInteractor != null)
                playerInteractor.ForceClearCurrentInteractable(this);

            SpawnPickup();
        }));
    }

    private IEnumerator PerformTimedAction(float duration, System.Action onCompleted)
    {
        isBusy = true;
        RefreshSelectionUI();
        StartToolAction();

        yield return StartCoroutine(DoActionTimer(duration));

        StopToolAction();

        onCompleted?.Invoke();

        isBusy = false;
        RefreshSelectionUI();
    }

    // =========================
    // GROWTH
    // =========================
    private IEnumerator GrowRoutine(float duration, CropState nextState)
    {
        if (growTimer == null)
        {
            Debug.LogError("RadialTimerUI is not assigned!", this);
            yield break;
        }

        growTimer.Play(duration, true);
        yield return new WaitForSeconds(duration);
        growTimer.StopAndHide();

        SetState(nextState);
        RefreshSelectionUI();
    }

    // =========================
    // STATE LOGIC
    // =========================
    private void SetState(CropState newState)
    {
        currentState = newState;
        UpdateVisuals();
        UpdateTool();
    }

    private void UpdateVisuals()
    {
        if (stage01 == null || stage02 == null || stage03 == null)
        {
            Debug.LogError("One or more crop stage visuals are not assigned!", this);
            return;
        }

        stage01.SetActive(
            currentState == CropState.Stage01 ||
            currentState == CropState.NeedsWater1
        );

        stage02.SetActive(
            currentState == CropState.Stage02 ||
            currentState == CropState.NeedsWater2
        );

        stage03.SetActive(
            currentState == CropState.Ready
        );
    }

    private void UpdateTool()
    {
        ClearTool();

        switch (currentState)
        {
            case CropState.Empty:
                ShowTool(shovelPrefab);
                break;

            case CropState.NeedsWater1:
            case CropState.NeedsWater2:
                ShowTool(wateringCanPrefab);
                break;

            case CropState.Ready:
                ShowTool(sicklePrefab);
                break;

            case CropState.AwaitingPickup:
                break;
        }
    }

    // =========================
    // TOOL LOGIC
    // =========================
    private void ShowTool(GameObject prefab)
    {
        if (toolAnchor == null)
        {
            Debug.LogError("ToolAnchor is not assigned!", this);
            return;
        }

        if (prefab == null)
        {
            Debug.LogError("Tool prefab is missing!", this);
            return;
        }

        currentToolInstance = Instantiate(
            prefab,
            toolAnchor.position,
            toolAnchor.rotation,
            toolAnchor
        );

        if (currentToolInstance.TryGetComponent<ToolActionAnimator>(out var toolAnimator))
            toolAnimator.SetInteractionReady(false);
    }

    private void ClearTool()
    {
        if (currentToolInstance != null)
        {
            Destroy(currentToolInstance);
            currentToolInstance = null;
        }
    }

    private void StartToolAction()
    {
        if (TryGetToolAnimator(out var animator))
            animator.StartAction();
    }

    private void StopToolAction()
    {
        if (TryGetToolAnimator(out var animator))
            animator.StopAction();
    }

    private bool TryGetToolAnimator(out ToolActionAnimator animator)
    {
        if (currentToolInstance != null)
            return currentToolInstance.TryGetComponent(out animator);

        animator = null;
        return false;
    }

    // =========================
    // TIMER UI
    // =========================
    private IEnumerator DoActionTimer(float duration)
    {
        if (actionTimer == null)
        {
            Debug.LogError("RadialTimerUI is not assigned!", this);
            yield break;
        }

        actionTimer.Play(duration, true);
        yield return new WaitForSeconds(duration);
        actionTimer.StopAndHide();
    }


    // =========================
    // PICKUP
    // =========================
    private void SpawnPickup()
    {
        if (pickupPrefab == null)
        {
            Debug.LogError("Pickup prefab is not assigned!", this);
            return;
        }

        if (pickupSpawnPoint == null)
        {
            Debug.LogError("PickupSpawnPoint is not assigned!", this);
            return;
        }

        var pickup = Instantiate(
            pickupPrefab,
            pickupSpawnPoint.position,
            pickupSpawnPoint.rotation,
            pickupSpawnPoint
            );

        if (pickup.TryGetComponent(out PickupItem item))
        {
            item.Init(this);

            // Register the spawned pickup immediately in case the player is already
            // inside its trigger range and new trigger enter event will not work.
            if (playerInteractor != null)
                playerInteractor.RegisterInteractable(item);
        }
    }

    public void OnPickupCollected()
    {
        SetState(CropState.Empty);
        RefreshSelectionUI();
    }

    // =========================
    // SELECTION & PROMPT UI
    // =========================
    private void RefreshSelectionUI()
    {
        bool canInteract = IsInteractionAvailable();

        bool showPrompt = isSelected && canInteract;
        ShowSharedPrompt(showPrompt);

        if (TryGetToolAnimator(out var toolAnimator))
            toolAnimator.SetInteractionReady(isSelected && canInteract);
    }

    public bool IsInteractionAvailable()
    {
        bool playerHasItem = playerInteractor != null && playerInteractor.HasItem;
        return !isBusy && !playerHasItem && CanInteractInCurrentState();
    }

    public void ShowSharedPrompt(bool show)
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(show);
    }
}