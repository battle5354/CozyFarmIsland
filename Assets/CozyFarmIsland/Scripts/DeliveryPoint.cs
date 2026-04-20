using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    [Header("UI")]
    [SerializeField] private GameObject interactPrompt;

    private PlayerInteractor player;
    private bool isSelected;

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerInteractor>();

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    public void Interact()
    {
        Debug.Log($"DeliveryPoint.Interact called. player null: {player == null}, has item: {player != null && player.HasItem}", this);
        if (player == null || !player.HasItem)
            return;

        player.DropCarriedItem();
        RefreshUI();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshUI();
    }

    public Transform GetInteractionPoint()
    {
        return transform;
    }

    public bool IsInteractionAvailable()
    {
        return player != null && player.HasItem;
    }

    private void RefreshUI()
    {
        if (interactPrompt == null)
            return;

        interactPrompt.SetActive(isSelected && IsInteractionAvailable());
    }
}