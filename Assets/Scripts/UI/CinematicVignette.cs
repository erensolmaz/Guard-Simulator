using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CinematicVignette : MonoBehaviour
{
    [Header("Vignette Settings")]
    [SerializeField] private bool enableVignette = true;
    [SerializeField] private Volume postProcessVolume;
    
    [Header("Vignette Properties")]
    [SerializeField][Range(0f, 1f)] private float intensity = 0.3f;
    [SerializeField][Range(0f, 1f)] private float smoothness = 0.4f;
    [SerializeField] private Color vignetteColor = Color.black;
    [SerializeField] private bool rounded = true;
    
    [Header("Dynamic Animation")]
    [SerializeField] private bool animateIntensity = false;
    [SerializeField] private float minIntensity = 0.2f;
    [SerializeField] private float maxIntensity = 0.4f;
    [SerializeField] private float animationSpeed = 0.5f;

    private Vignette vignette;
    private float animationTime = 0f;

    private void Start()
    {
        SetupVignette();
    }

    private void Update()
    {
        if (vignette != null && animateIntensity && enableVignette)
        {
            AnimateVignetteIntensity();
        }
    }

    private void SetupVignette()
    {
        if (postProcessVolume == null)
        {
            postProcessVolume = GetComponent<Volume>();
        }

        if (postProcessVolume == null)
        {
            postProcessVolume = gameObject.AddComponent<Volume>();
            postProcessVolume.isGlobal = true;
            postProcessVolume.priority = 1;
        }

        if (postProcessVolume.profile == null)
        {
            postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        if (!postProcessVolume.profile.TryGet(out vignette))
        {
            vignette = postProcessVolume.profile.Add<Vignette>(true);
        }

        ApplyVignetteSettings();
    }

    private void ApplyVignetteSettings()
    {
        if (vignette == null) return;

        vignette.active = enableVignette;
        vignette.intensity.value = intensity;
        vignette.smoothness.value = smoothness;
        vignette.color.value = vignetteColor;
        vignette.rounded.value = rounded;
    }

    private void AnimateVignetteIntensity()
    {
        animationTime += Time.deltaTime * animationSpeed;
        float newIntensity = Mathf.Lerp(minIntensity, maxIntensity, (Mathf.Sin(animationTime) + 1f) / 2f);
        vignette.intensity.value = newIntensity;
    }

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
        if (vignette != null)
        {
            vignette.intensity.value = intensity;
        }
    }

    public void SetSmoothness(float value)
    {
        smoothness = Mathf.Clamp01(value);
        if (vignette != null)
        {
            vignette.smoothness.value = smoothness;
        }
    }

    public void EnableVignette(bool enable)
    {
        enableVignette = enable;
        if (vignette != null)
        {
            vignette.active = enable;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && vignette != null)
        {
            ApplyVignetteSettings();
        }
    }
}
