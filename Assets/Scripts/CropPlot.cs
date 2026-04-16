using System.Collections;
using UnityEngine;

public class CropPlot : MonoBehaviour
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
    [SerializeField] private Transform timerAnchor;
    [SerializeField] private GameObject radialTimerPrefab;

    [Header("Harvest")]
    [SerializeField] private Transform pickupSpawnPoint;
    [SerializeField] private GameObject pickupPrefab;

    [Header("Timings")]
    [SerializeField] private float actionTime = 2f;
    [SerializeField] private float growTime = 10f;

    private CropState currentState = CropState.Empty;

    private GameObject currentToolInstance;
    private GameObject currentTimerInstance;

    private bool isBusy = false;

    private void Awake()
    {
        if (toolAnchor == null)
            Debug.LogError("ToolAnchor is missing!", this);

        if (timerAnchor == null)
            Debug.LogError("TimerAnchor is missing!", this);

        if (pickupSpawnPoint == null)
            Debug.LogError("PickupSpawnPoint is missing!", this);

        if (stage01 == null || stage02 == null || stage03 == null)
            Debug.LogError("Crop stages are not fully assigned!", this);
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
                StartCoroutine(WaterRoutine());
                break;

            case CropState.NeedsWater2:
                StartCoroutine(WaterRoutine());
                break;

            case CropState.Ready:
                StartCoroutine(HarvestRoutine());
                break;
        }
    }

    // =========================
    // ROUTINES
    // =========================

    private IEnumerator PlantRoutine()
    {
        isBusy = true;
        ClearTool();

        yield return StartCoroutine(DoActionTimer());

        SetState(CropState.Stage01);
        StartCoroutine(GrowToNeedsWater1());

        isBusy = false;
    }

    private IEnumerator WaterRoutine()
    {
        isBusy = true;
        ClearTool();

        yield return StartCoroutine(DoActionTimer());

        if (currentState == CropState.NeedsWater1)
        {
            SetState(CropState.Stage02);
            StartCoroutine(GrowToNeedsWater2());
        }
        else if (currentState == CropState.NeedsWater2)
        {
            SetState(CropState.Ready);
        }

        isBusy = false;
    }

    private IEnumerator HarvestRoutine()
    {
        isBusy = true;
        ClearTool();

        yield return StartCoroutine(DoActionTimer());

        SpawnPickup();
        SetState(CropState.Empty);

        isBusy = false;
    }

    // =========================
    // GROWTH
    // =========================

    private IEnumerator GrowToNeedsWater1()
    {
        yield return new WaitForSeconds(growTime);
        SetState(CropState.NeedsWater1);
    }

    private IEnumerator GrowToNeedsWater2()
    {
        yield return new WaitForSeconds(growTime);
        SetState(CropState.NeedsWater2);
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
    // TIMER
    // =========================

    private IEnumerator DoActionTimer()
    {
        if (radialTimerPrefab == null)
        {
            Debug.LogError("RadialTimer prefab is not assigned!", this);
            yield break;
        }

        if (timerAnchor == null)
        {
            Debug.LogError("TimerAnchor is not assigned!", this);
            yield break;
        }

        currentTimerInstance = Instantiate(
            radialTimerPrefab,
            timerAnchor.position,
            Quaternion.identity,
            timerAnchor
        );

        yield return new WaitForSeconds(actionTime);

        if (currentTimerInstance != null)
            Destroy(currentTimerInstance);
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
}