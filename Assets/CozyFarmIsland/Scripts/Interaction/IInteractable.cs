using UnityEngine;

public interface IInteractable
{
    void Interact();
    void SetSelected(bool selected);
    Transform GetInteractionPoint();
}