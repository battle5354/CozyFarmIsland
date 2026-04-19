using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    private PlayerInteractor player;
    private CropPlot sourcePlot;

    public void Init(CropPlot plot)
    {
        sourcePlot = plot;
    }

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerInteractor>();
    }

    public void Interact()
    {
        if (player == null || player.HasItem)
            return;

        player.PickUp(gameObject);

        if (sourcePlot != null)
            sourcePlot.OnPickupCollected();
    }

    public void SetSelected(bool selected) { }

    public Transform GetInteractionPoint()
    {
        return transform;
    }
}