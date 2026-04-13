using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 1f;

    private float currentRotation = 0f;

    private void Update()
    {
        if (RenderSettings.skybox == null)
            return;

        currentRotation += rotationSpeed * Time.deltaTime;

        RenderSettings.skybox.SetFloat("_Rotation", currentRotation);

        DynamicGI.UpdateEnvironment();
    }
}