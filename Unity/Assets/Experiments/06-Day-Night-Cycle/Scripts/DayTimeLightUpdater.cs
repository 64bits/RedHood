using UnityEngine;

public class DayTimeLightUpdater : MonoBehaviour
{
    [Header("Light Reference")]
    [SerializeField] private Light sunLight;

    [Header("Day Settings")]
    [SerializeField] private float dayIntensity = 1.0f;
    [SerializeField] private float dayTemperature = 6500f;

    [Header("Night Settings")]
    [SerializeField] private float nightTemperature = 3000f;

    [Header("Skybox Influence")]
    [SerializeField] private bool useSkyboxTint = true;
    [SerializeField] [Range(0f, 1f)] private float skyboxInfluence = 0.3f;
    [SerializeField] private float minTemperature = 2000f;
    [SerializeField] private float maxTemperature = 10000f;

    [Header("Rotation Settings")]
    [Tooltip("Rotation at normalized time 0.0 (Start of Day)")]
    [SerializeField] private Vector3 dayStartRotation = new Vector3(5, 0, 0);
    [Tooltip("Rotation at normalized time 0.5 (Start of Night)")]
    [SerializeField] private Vector3 dayEndRotation = new Vector3(175, 0, 0);

    [Header("Fog Settings")]
    [SerializeField] private bool controlFog = true;
    [SerializeField] private float maxFogDensity = 0.049f;

    [Header("Skybox Settings")]
    [SerializeField] private bool controlSkyboxIntensity = true;
    [SerializeField] private float daySkyboxIntensity = 0.24f;
    [SerializeField] private float nightSkyboxIntensity = 0f;

    [Header("Manager Reference")]
    [SerializeField] private DayTimeManager timeManager;

    private Material skyboxMaterial;
    private int skyTintShaderID;
    private int skyExposureShaderID;

    private void Awake()
    {
        if (timeManager == null) timeManager = FindObjectOfType<DayTimeManager>();
        
        if (RenderSettings.skybox != null)
        {
            skyboxMaterial = RenderSettings.skybox;
            skyTintShaderID = Shader.PropertyToID("_Tint");
            skyExposureShaderID = Shader.PropertyToID("_Exposure");
        }
        else
        {
            useSkyboxTint = false;
        }
    }

    private void OnEnable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeUpdated += UpdateLight;
            // Initialize immediately
            UpdateLight(timeManager.GetNormalizedTime());
        }
    }

    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeUpdated -= UpdateLight;
        }
    }

    private void UpdateLight(float normalizedTime)
    {
        if (sunLight == null || timeManager == null) return;

        // 1. Calculate Normalized Markers
        // We convert the absolute times from the manager into 0-1 values
        float cycleDuration = timeManager.CycleDuration;
        float nDuskStart = timeManager.DuskStart / cycleDuration;
        float nNightStart = timeManager.NightStart / cycleDuration;
        float nDawnStart = timeManager.DawnStart / cycleDuration;
        
        // 2. Update Sun Rotation
        // We map the sun's arc from 0.0 to 0.5 (Day through Dusk).
        // From 0.5 to 1.0, we rotate it back (or under the world).
        if (normalizedTime <= 0.5f)
        {
            // Day Arc (0 -> 0.5 maps to 0 -> 1 lerp)
            float rotProgress = normalizedTime / 0.5f;
            sunLight.transform.rotation = Quaternion.Lerp(
                Quaternion.Euler(dayStartRotation),
                Quaternion.Euler(dayEndRotation),
                rotProgress
            );
        }
        else
        {
            // Night Return Arc (0.5 -> 1.0 maps to 0 -> 1 lerp)
            float rotProgress = (normalizedTime - 0.5f) / 0.5f;
            sunLight.transform.rotation = Quaternion.Lerp(
                Quaternion.Euler(dayEndRotation),
                Quaternion.Euler(dayStartRotation),
                rotProgress
            );
        }

        // 3. Update Visuals based on Normalized Phases
        // Cycle: Day (0.0) -> Dusk -> Night -> Dawn -> Day (1.0)
        
        if (normalizedTime < nDuskStart)
        {
            // --- DAY PHASE ---
            ApplyVisuals(1.0f, 0.0f); // 1.0 Intensity, 0.0 Fog
        }
        else if (normalizedTime >= nDuskStart && normalizedTime < nNightStart)
        {
            // --- DUSK PHASE ---
            // Calculate 0-1 progress within the dusk window
            float range = nNightStart - nDuskStart;
            float progress = (normalizedTime - nDuskStart) / range;
            
            // Lerp Day -> Night
            ApplyVisuals(1.0f - progress, progress); 
        }
        else if (normalizedTime >= nNightStart && normalizedTime < nDawnStart)
        {
            // --- NIGHT PHASE ---
            ApplyVisuals(0.0f, 1.0f); // 0.0 Intensity, 1.0 Fog
        }
        else // normalizedTime >= nDawnStart
        {
            // --- DAWN PHASE ---
            // Calculate 0-1 progress within the dawn window
            // Note: Dawn goes from DawnStart to CycleEnd (1.0)
            float range = 1.0f - nDawnStart;
            float progress = (normalizedTime - nDawnStart) / range;
            
            // Lerp Night -> Day
            ApplyVisuals(progress, 1.0f - progress);
        }
    }

    private void ApplyVisuals(float dayFactor, float nightFactor)
    {
        // dayFactor: 1 = Full Day, 0 = Full Night
        // nightFactor: 1 = Full Night, 0 = Full Day
        // (They are usually inverses, but passed separately for clarity)

        // 1. Sun Intensity
        sunLight.intensity = Mathf.Lerp(0f, dayIntensity, dayFactor);

        // 2. Sun Temperature
        // Blend base temp
        float currentBaseTemp = Mathf.Lerp(nightTemperature, dayTemperature, dayFactor);
        // Apply Skybox tint influence
        sunLight.colorTemperature = ApplySkyboxInfluence(currentBaseTemp);
        
        // Apply actual color to light
        Color tempColor = Mathf.CorrelatedColorTemperatureToRGB(sunLight.colorTemperature);
        sunLight.color = tempColor;
        Shader.SetGlobalColor("_SunColor", sunLight.color);

        // 3. Fog
        if (controlFog)
        {
            // Use nightFactor because fog increases at night
            RenderSettings.fog = nightFactor > 0.01f; // Optimization: Disable fog fully during day
            if (RenderSettings.fog)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = Mathf.Lerp(0f, maxFogDensity, nightFactor);
            }
        }

        // 4. Skybox Intensity
        if (controlSkyboxIntensity && skyboxMaterial != null)
        {
            float skyExposure = Mathf.Lerp(nightSkyboxIntensity, daySkyboxIntensity, dayFactor);
            skyboxMaterial.SetFloat(skyExposureShaderID, skyExposure);
        }
    }

    private float ApplySkyboxInfluence(float baseTemperature)
    {
        if (!useSkyboxTint || skyboxMaterial == null) return baseTemperature;

        Color skyTint = skyboxMaterial.GetColor(skyTintShaderID);
        float skyTemp = ColorToTemperature(skyTint);
        
        return Mathf.Clamp(Mathf.Lerp(baseTemperature, skyTemp, skyboxInfluence), minTemperature, maxTemperature);
    }

    private float ColorToTemperature(Color color)
    {
        // Approximate warmth from RGB
        float warmth = (color.r + color.g * 0.5f) / (color.b + 0.1f);
        return Mathf.Lerp(2000f, 12000f, 1f / (warmth + 0.5f));
    }
}