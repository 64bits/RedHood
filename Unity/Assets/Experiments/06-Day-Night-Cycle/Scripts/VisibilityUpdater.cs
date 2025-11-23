using UnityEngine;

public class VisibilityUpdater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayTimeManager timeManager;
    [SerializeField] private Light sunLight;

    [Header("Sun Intensity")]
    [SerializeField] private float dayIntensity = 1.0f;
    [SerializeField] private float nightIntensity = 0.0f;

    [Header("Fog Settings")]
    [SerializeField] private bool controlFog = true;
    [SerializeField] private float maxFogDensity = 0.049f;
    [SerializeField] private Color dayFogColor = new Color(0.8f, 0.9f, 1.0f); // Default bluish gray
    [SerializeField] private Color nightFogColor = new Color(0.1f, 0.1f, 0.15f); // Default dark

    [Header("Skybox Exposure")]
    [SerializeField] private bool controlSkyboxIntensity = true;
    [SerializeField] private float daySkyboxIntensity = 1.0f; // Standard exposure
    [SerializeField] private float nightSkyboxIntensity = 0.2f;

    private Material skyboxMaterial;
    private int skyExposureShaderID;

    private void Awake()
    {
        if (timeManager == null) timeManager = FindObjectOfType<DayTimeManager>();

        if (RenderSettings.skybox != null)
        {
            skyboxMaterial = RenderSettings.skybox;
            skyExposureShaderID = Shader.PropertyToID("_Exposure");
        }
        else
        {
            controlSkyboxIntensity = false;
        }
    }

    private void OnEnable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeUpdated += UpdateVisibility;
            UpdateVisibility(timeManager.GetNormalizedTime());
        }
    }

    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeUpdated -= UpdateVisibility;
        }
    }

    private void UpdateVisibility(float normalizedTime)
    {
        if (timeManager == null) return;

        // Calculate Phase Factors (0.0 = Night, 1.0 = Day)
        float dayFactor = CalculateDayFactor(normalizedTime);
        float nightFactor = 1.0f - dayFactor;

        ApplyVisuals(dayFactor, nightFactor);
    }

    private float CalculateDayFactor(float normalizedTime)
    {
        // Normalize time markers
        float cycleDuration = timeManager.CycleDuration;
        float nDuskStart = timeManager.DuskStart / cycleDuration;
        float nNightStart = timeManager.NightStart / cycleDuration;
        float nDawnStart = timeManager.DawnStart / cycleDuration;

        if (normalizedTime < nDuskStart) return 1.0f; // Day
        if (normalizedTime >= nNightStart && normalizedTime < nDawnStart) return 0.0f; // Night

        // Dusk (Lerp 1 -> 0)
        if (normalizedTime >= nDuskStart && normalizedTime < nNightStart)
        {
            float range = nNightStart - nDuskStart;
            float progress = (normalizedTime - nDuskStart) / range;
            return 1.0f - progress;
        }
        
        // Dawn (Lerp 0 -> 1)
        // normalizedTime >= nDawnStart
        float dawnRange = 1.0f - nDawnStart;
        float dawnProgress = (normalizedTime - nDawnStart) / dawnRange;
        return dawnProgress;
    }

    private void ApplyVisuals(float dayFactor, float nightFactor)
    {
        // 1. Sun Intensity
        if (sunLight != null)
        {
            sunLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, dayFactor);
        }

        // 2. Fog Strength and Tint
        if (controlFog)
        {
            // Simple optimization: Disable fog if density is near zero during day
            bool shouldFogBeOn = maxFogDensity > 0;
            RenderSettings.fog = shouldFogBeOn;
            
            if (shouldFogBeOn)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                // Density increases at night
                RenderSettings.fogDensity = Mathf.Lerp(0f, maxFogDensity, nightFactor);
                // Color blends
                RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, dayFactor);
            }
        }

        // 3. Skybox Intensity (Exposure)
        if (controlSkyboxIntensity && skyboxMaterial != null)
        {
            float skyExposure = Mathf.Lerp(nightSkyboxIntensity, daySkyboxIntensity, dayFactor);
            skyboxMaterial.SetFloat(skyExposureShaderID, skyExposure);
        }
    }
}