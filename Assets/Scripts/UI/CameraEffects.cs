using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("Field of View Animation")]
    [SerializeField] private bool animateFOV = true;
    [SerializeField] private float minFOV = 50f;
    [SerializeField] private float maxFOV = 70f;
    [SerializeField] private float fovSpeed = 0.5f;
    
    [Header("Rotation Sway")]
    [SerializeField] private bool enableSway = true;
    [SerializeField] private float swayAmount = 2f;
    [SerializeField] private float swaySpeed = 1f;
    
    [Header("Smooth Random Look")]
    [SerializeField] private bool enableRandomLook = true;
    [SerializeField] private float lookChangeInterval = 5f;
    [SerializeField] private float maxLookOffset = 3f;
    [SerializeField] private float lookSmoothness = 2f;
    
    private Camera cam;
    private float fovTime = 0f;
    private float lookTimer = 0f;
    private Vector3 targetLookOffset = Vector3.zero;
    private Vector3 currentLookOffset = Vector3.zero;
    private Quaternion baseRotation;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    private void Update()
    {
        if (cam == null) return;

        if (animateFOV)
        {
            AnimateFieldOfView();
        }

        if (enableRandomLook)
        {
            ApplyRandomLook();
        }

        if (enableSway)
        {
            ApplySway();
        }
    }

    private void AnimateFieldOfView()
    {
        fovTime += Time.deltaTime * fovSpeed;
        float fov = Mathf.Lerp(minFOV, maxFOV, (Mathf.Sin(fovTime) + 1f) / 2f);
        cam.fieldOfView = fov;
    }

    private void ApplyRandomLook()
    {
        lookTimer += Time.deltaTime;
        
        if (lookTimer >= lookChangeInterval)
        {
            lookTimer = 0f;
            targetLookOffset = new Vector3(
                Random.Range(-maxLookOffset, maxLookOffset),
                Random.Range(-maxLookOffset, maxLookOffset),
                0f
            );
        }

        currentLookOffset = Vector3.Lerp(
            currentLookOffset,
            targetLookOffset,
            Time.deltaTime * lookSmoothness
        );

        transform.localRotation = Quaternion.Euler(currentLookOffset) * transform.localRotation;
    }

    private void ApplySway()
    {
        float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        float swayY = Mathf.Cos(Time.time * swaySpeed * 0.7f) * swayAmount * 0.5f;
        
        Vector3 swayRotation = new Vector3(swayY, swayX, 0f);
        transform.localRotation = Quaternion.Euler(swayRotation) * transform.localRotation;
    }
}
