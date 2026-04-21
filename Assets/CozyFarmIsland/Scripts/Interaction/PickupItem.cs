using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField] private Collider interactionCollider; // trigger
    [SerializeField] private Collider physicalCollider;    // solid collider
    private PlayerInteractor player;
    private CropPlot sourcePlot;


    public void Init(CropPlot plot)
    {
        sourcePlot = plot;
    }

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerInteractor>();

        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider>();

        if (physicalCollider == null)
        {
            var colliders = GetComponentsInChildren<Collider>(true);

            foreach (var col in colliders)
            {
                if (col != interactionCollider)
                {
                    physicalCollider = col;
                    break;
                }
            }
        }
    }

    public void Interact()
    {
        if (player == null || player.HasItem)
            return;

        player.UnregisterInteractable(this);
        player.PickUp(gameObject);

        if (sourcePlot != null)
            sourcePlot.OnPickupCollected();
    }

    public void SetSelected(bool selected)
    {
        // Pickup reuses the plot's shared interaction prompt instead of owning separate UI.
        if (sourcePlot != null)
            sourcePlot.ShowSharedPrompt(selected && IsInteractionAvailable());
    }

    public Transform GetInteractionPoint()
    {
        return transform;
    }

    public bool IsInteractionAvailable()
    {
        return player != null && !player.HasItem;
    }

    public void OnPickedUp()
    {
        // Disable interaction trigger after pickup so the carried item cannot be selected again.
        if (interactionCollider != null)
            interactionCollider.enabled = false;
    }
}