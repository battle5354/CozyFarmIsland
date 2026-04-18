using UnityEngine;

public class ToolActionAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem toolVfx;

    [Header("Tool Settings")]
    [SerializeField] private int toolActionIndex; // 0 shovel, 1 watering, 2 sickle

    private static readonly int ToolActionHash = Animator.StringToHash("ToolAction");
    private static readonly int IsInteractableHash = Animator.StringToHash("IsInteractable");
    private static readonly int DoInteractHash = Animator.StringToHash("DoInteract");

    public void StartIdle()
    {
        if (animator == null) return;

        animator.SetBool(IsInteractableHash, true);
    }

    public void StartAction()
    {
        if (animator == null) return;

        animator.SetInteger(ToolActionHash, toolActionIndex);
        animator.SetTrigger(DoInteractHash);
        animator.SetBool(IsInteractableHash, false);

        if (toolVfx != null)
            toolVfx.Play();
    }

    public void StopAction()
    {
        if (animator == null) return;

        animator.SetBool(IsInteractableHash, true);

        if (toolVfx != null)
            toolVfx.Stop();
    }
}