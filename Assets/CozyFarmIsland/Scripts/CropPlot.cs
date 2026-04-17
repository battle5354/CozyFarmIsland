using System.Collections;
using UnityEngine;

public class CropPlot : MonoBehaviour, IInteractable
{
    public enum CropState
    {
        Empty,
        Stage01,
        NeedsWater1,
        Stage02,
        NeedsWater2,
        Ready,
        Busy
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
    [SerializeField] private RadialTimerUI radialTimer;

    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;

    [Header("Harvest")]
    [SerializeField] private Transform pickupSpawnPoint;
    [SerializeField] private GameObject pickupPrefab;

    [Header("Timings")]
    [SerializeField] private float actionTime = 2f;
    [SerializeField] private float growTime = 10f;

    private CropState currentState = CropState.Empty;

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

        if (radialTimer == null)
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
        SetState(CropState.Empty);
        RefreshSelectionUI();
    }

    // =========================
    // PUBLIC INTERACTION
    // =========================

    public void Interact()
    {
        if (isBusy) return;

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
        isSelected = selected;
        RefreshSelectionUI();
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
        isBusy = true;
        RefreshSelectionUI();
        ClearTool();

        yield return StartCoroutine(DoActionTimer(actionTime));

        SetState(CropState.Stage01);

        isBusy = false;
        RefreshSelectionUI();

        StartCoroutine(GrowRoutine(growTime, CropState.NeedsWater1));
    }

    private IEnumerator WaterRoutine()
    {
        isBusy = true;
        RefreshSelectionUI();
        ClearTool();

        CropState stateBeforeAction = currentState;

        yield return StartCoroutine(DoActionTimer(actionTime));

        if (stateBeforeAction == CropState.NeedsWater1)
        {
            SetState(CropState.Stage02);

            isBusy = false;
            RefreshSelectionUI();

            StartCoroutine(GrowRoutine(growTime, CropState.NeedsWater2));
        }
        else if (stateBeforeAction == CropState.NeedsWater2)
        {
            SetState(CropState.Ready);

            isBusy = false;
            RefreshSelectionUI();
        }
    }

    private IEnumerator HarvestRoutine()
    {
        isBusy = true;
        RefreshSelectionUI();
        ClearTool();

        yield return StartCoroutine(DoActionTimer(actionTime));

        SpawnPickup();
        SetState(CropState.Empty);

        isBusy = false;
        RefreshSelectionUI();
    }

    // =========================
    // GROWTH
    // =========================

    private IEnumerator GrowRoutine(float duration, CropState nextState)
    {
        if (radialTimer == null)
        {
            Debug.LogError("RadialTimerUI is not assigned!", this);
            yield break;
        }

        radialTimer.Play(duration, true);
        yield return new WaitForSeconds(duration);
        radialTimer.StopAndHide();

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

        stage01.SetActive(currentState == CropState.Stage01);
        stage02.SetActive(currentState == CropState.Stage02);
        stage03.SetActive(currentState == CropState.Ready);
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
    }

    private void ClearTool()
    {
        if (currentToolInstance != null)
        {
            Destroy(currentToolInstance);
            currentToolInstance = null;
        }
    }

    // =========================
    // UI (TIMER)
    // =========================

    private IEnumerator DoActionTimer(float duration)
    {
        if (radialTimer == null)
        {
            Debug.LogError("RadialTimerUI is not assigned!", this);
            yield break;
        }

        radialTimer.Play(duration, true);
        yield return new WaitForSeconds(duration);
        radialTimer.StopAndHide();
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

        Instantiate(
            pickupPrefab,
            pickupSpawnPoint.position,
            pickupSpawnPoint.rotation
        );
    }

    // =========================
    // SELECTION & UI
    // =========================

    private void RefreshSelectionUI()
    {
        if (interactPrompt == null)
            return;

        bool canInteract =
            currentState == CropState.Empty ||
            currentState == CropState.NeedsWater1 ||
            currentState == CropState.NeedsWater2 ||
            currentState == CropState.Ready;

        interactPrompt.SetActive(isSelected && !isBusy && canInteract);
    }
}