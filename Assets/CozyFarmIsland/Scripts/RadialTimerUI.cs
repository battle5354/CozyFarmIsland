using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RadialTimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (fillImage == null)
            Debug.LogError("Fill Image is not assigned on RadialTimerUI!", this);

        SetFill(0f);
        gameObject.SetActive(false);
    }

    public void Play(float duration, bool fillFromZeroToOne = true)
    {
        if (fillImage == null)
        {
            Debug.LogError("Cannot play radial timer because Fill Image is missing!", this);
            return;
        }

        if (duration <= 0f)
        {
            Debug.LogError("Radial timer duration must be greater than 0!", this);
            return;
        }

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        gameObject.SetActive(true);
        currentRoutine = StartCoroutine(PlayRoutine(duration, fillFromZeroToOne));
    }

    public void StopAndHide()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        SetFill(0f);
        gameObject.SetActive(false);
    }

    private IEnumerator PlayRoutine(float duration, bool fillFromZeroToOne)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);

            SetFill(fillFromZeroToOne ? normalized : 1f - normalized);

            yield return null;
        }

        SetFill(fillFromZeroToOne ? 1f : 0f);
        currentRoutine = null;
        gameObject.SetActive(false);
    }

    private void SetFill(float value)
    {
        if (fillImage != null)
            fillImage.fillAmount = value;
    }
}