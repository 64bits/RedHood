using UnityEngine;

public class VisibilityUpdater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayTimeManager timeManager;
    [SerializeField] private CanopyManager canopyManager;
    [SerializeField] private Light sunLight;

    [Header("Sun Intensity")]
    [SerializeField] private float dayIntensity = 1.0f;
    [SerializeField] private float nightIntensity = 0.0f;

    [Header("Fog Settings")]
    [SerializeField] private bool controlFog = true;
    [SerializeField] private float maxFogDensity = 0.049f;
    [SerializeField] private Color dayFogColor = new Color(0.8f, 0.9f, 1.0f);
    [SerializeField] private Color nightFogColor = new Color(0.1f, 0.1f, 0.15f);

    [Header("Skybox Exposure")]
    [SerializeField] private bool controlSkyboxIntensity = true;
    [SerializeField] private float daySkyboxIntensity = 1.0f;
    [SerializeField] private float nightSkyboxIntensity = 0.2f;

    [Header("Canopy Influence")]
    [Tooltip("How much to darken the sun/skybox when at full canopy (0-1). 0.5 = 50% darker.")]
    [SerializeField] [Range(0f, 1f)] private float canopyLightDampening = 0.4f;
    
    [Tooltip("How much fog density to add at full canopy.")]
    [SerializeField] private float canopyFogGain = 0.02f;
    
    [Tooltip("The color to tint the fog towards when under the canopy.")]
    [SerializeField] private Color canopyFogTint = new Color(0.2f, 0.3f, 0.2f); // Forest Greenish

    // Internal State
    private Material skyboxMaterial;
    private int skyExposureShaderID;
    private float currentNormalizedTime = 0f;
    private float currentCanopyAmount = 0f;

    private void Awake()
    {
        if (timeManager == null) timeManager = FindObjectOfType<DayTimeManager>();
        if (canopyManager == null) canopyManager = FindObjectOfType<CanopyManager>();

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
            timeManager.OnTimeUpdated += OnTimeUpdated;
            // Init time
            currentNormalizedTime = timeManager.GetNormalizedTime();
        }

        if (canopyManager != null)
        {
            canopyManager.OnCanopyChanged += OnCanopyUpdated;
            // Init canopy
            currentCanopyAmount = canopyManager.GetCurrentCanopy();
        }

        // Force initial update
        ResolveVisibility();
    }

    private void OnDisable()
    {
        if (timeManager != null) timeManager.OnTimeUpdated -= OnTimeUpdated;
        if (canopyManager != null) canopyManager.OnCanopyChanged -= OnCanopyUpdated;
    }

    // --- Event Handlers ---

    private void OnTimeUpdated(float normalizedTime)
    {
        currentNormalizedTime = normalizedTime;
        ResolveVisibility();
    }

    private void OnCanopyUpdated(float canopyAmount)
    {
        currentCanopyAmount = canopyAmount;
        ResolveVisibility();
    }

    // --- Core Logic ---

    private void ResolveVisibility()
    {
        if (timeManager == null) return;

        // 1. Calculate Time Factors (Day vs Night)
        float dayFactor = CalculateDayFactor(currentNormalizedTime);
        float nightFactor = 1.0f - dayFactor;

        // 2. Calculate Base Visuals (Time based)
        float targetSunIntensity = Mathf.Lerp(nightIntensity, dayIntensity, dayFactor);
        float targetSkyboxExposure = Mathf.Lerp(nightSkyboxIntensity, daySkyboxIntensity, dayFactor);
        
        // 3. Apply Canopy Influence to Light/Sky
        // We reduce light by the dampening factor scaled by canopy amount
        float dampeningMultiplier = 1.0f - (currentCanopyAmount * canopyLightDampening);
        
        ApplyLighting(targetSunIntensity * dampeningMultiplier, targetSkyboxExposure * dampeningMultiplier);

        // 4. Apply Fog (Time + Canopy)
        ApplyFog(dayFactor, nightFactor);
    }

    private void ApplyLighting(float intensity, float exposure)
    {
        // Apply Sun
        if (sunLight != null)
        {
            sunLight.intensity = intensity;
        }

        // Apply Skybox
        if (controlSkyboxIntensity && skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat(skyExposureShaderID, exposure);
        }
    }

    private void ApplyFog(float dayFactor, float nightFactor)
    {
        if (!controlFog) return;

        // --- Density Calculation ---
        // Base fog from night cycle
        float baseDensity = Mathf.Lerp(0f, maxFogDensity, nightFactor);
        
        // Added fog from canopy
        float canopyAddedDensity = currentCanopyAmount * canopyFogGain;

        // Combine, but clamp to maxFogDensity as requested
        float finalDensity = Mathf.Min(baseDensity + canopyAddedDensity, maxFogDensity);

        // Optimization: If density is negligible, disable fog
        bool shouldFogBeOn = finalDensity > 0.0001f;
        RenderSettings.fog = shouldFogBeOn;

        if (shouldFogBeOn)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = finalDensity;

            // --- Color Calculation ---
            // 1. Get Time-based Color
            Color timeColor = Color.Lerp(nightFogColor, dayFogColor, dayFactor);

            // 2. Calculate canopy influence based on time of day
            // Canopy tint effect is strongest during day (dayFactor = 1), zero at night (dayFactor = 0)
            float canopyInfluence = currentCanopyAmount * dayFactor;

            // 3. Blend towards Canopy Tint based on canopy influence
            RenderSettings.fogColor = Color.Lerp(timeColor, canopyFogTint, canopyInfluence);
        }
    }

    private float CalculateDayFactor(float normalizedTime)
    {
        float cycleDuration = timeManager.CycleDuration;
        float nDuskStart = timeManager.DuskStart / cycleDuration;
        float nNightStart = timeManager.NightStart / cycleDuration;
        float nDawnStart = timeManager.DawnStart / cycleDuration;

        // Day Phase
        if (normalizedTime < nDuskStart) return 1.0f;
        
        // Night Phase
        if (normalizedTime >= nNightStart && normalizedTime < nDawnStart) return 0.0f;

        // Dusk Phase (1 -> 0)
        if (normalizedTime >= nDuskStart && normalizedTime < nNightStart)
        {
            float range = nNightStart - nDuskStart;
            float progress = (normalizedTime - nDuskStart) / range;
            return 1.0f - progress;
        }
        
        // Dawn Phase (0 -> 1)
        // normalizedTime >= nDawnStart
        float dawnRange = 1.0f - nDawnStart;
        float dawnProgress = (normalizedTime - nDawnStart) / dawnRange;
        return dawnProgress;
    }
}