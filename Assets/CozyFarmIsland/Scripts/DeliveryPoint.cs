using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    private PlayerInteractor player;

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerInteractor>();
    }

    public void Interact()
    {
        if (player == null || !player.HasItem)
            return;

        player.DropCarriedItem();
    }

    public void SetSelected(bool selected)
    {
    }

    public Transform GetInteractionPoint()
    {
        return transform;
    }
}