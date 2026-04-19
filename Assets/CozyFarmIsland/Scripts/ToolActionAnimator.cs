using UnityEngine;

public class ToolActionAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem toolVfx;

    [Header("Tool Settings")]
    [SerializeField] private int toolActionIndex; // 1 shovel, 2 watering, 3 sickle

    private static readonly int ToolActionHash = Animator.StringToHash("ToolAction");
    private static readonly int IsInteractableHash = Animator.StringToHash("IsInteractable");
    private static readonly int IsActingHash = Animator.StringToHash("IsActing");
    private static readonly int DoInteractHash = Animator.StringToHash("DoInteract");

    public void SetInteractionReady(bool isReady)
    {
        if (animator == null) return;

        animator.SetBool(IsInteractableHash, isReady);
    }

    public void StartAction()
    {
        if (animator == null) return;

        animator.SetInteger(ToolActionHash, toolActionIndex);
        animator.SetBool(IsInteractableHash, false);
        animator.SetBool(IsActingHash, true);
        animator.SetTrigger(DoInteractHash);

        if (toolVfx != null)
            toolVfx.Play();
    }

    public void StopAction()
    {
        if (animator == null) return;

        animator.SetBool(IsActingHash, false);

        if (toolVfx != null)
            toolVfx.Stop();
    }
}