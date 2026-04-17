using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 direction = transform.position - cameraTransform.position;
        transform.rotation = Quaternion.LookRotation(direction);
    }
}