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
    [SerializeField] private float skyboxInfluence = 0.3f; // How much skybox affects light color (0-1)
    [SerializeField] private float minTemperature = 2000f; // Minimum temperature when influenced by skybox
    [SerializeField] private float maxTemperature = 10000f; // Maximum temperature when influenced by skybox

    [Header("Rotation Settings")]
    [Tooltip("Euler angles for the sun's rotation at the start of Dawn.")]
    [SerializeField] private Vector3 dayStartRotation = new Vector3(5, 0, 0);
    [Tooltip("Euler angles for the sun's rotation at the start of Dusk.")]
    [SerializeField] private Vector3 dayEndRotation = new Vector3(175, 0, 0);

    [Header("Fog Settings")]
    [Tooltip("Enable to control fog settings during time-of-day transitions.")]
    [SerializeField] private bool controlFog = true;
    [Tooltip("The fog density to be reached at night.")]
    [SerializeField] private float maxFogDensity = 0.049f;

    [Header("Skybox Settings")]
    [Tooltip("Enable to control the skybox's exposure/intensity.")]
    [SerializeField] private bool controlSkyboxIntensity = true;
    [Tooltip("The skybox exposure/intensity during the day.")]
    [SerializeField] private float daySkyboxIntensity = 0.24f;
    [Tooltip("The skybox exposure/intensity at night.")]
    [SerializeField] private float nightSkyboxIntensity = 0f;

    [Header("Manager Reference")]
    [SerializeField] private DayTimeManager timeManager;

    private Material skyboxMaterial;
    private int skyTintShaderID;
    private int skyExposureShaderID;

    private void Awake()
    {
        if (sunLight == null)
        {
            Debug.LogError("Sun Light is not assigned!");
        }

        if (timeManager == null)
        {
            timeManager = FindObjectOfType<DayTimeManager>();
            if (timeManager == null)
            {
                Debug.LogError("DayTimeManager not found!");
            }
        }

        // Get skybox material and shader property ID
        if (RenderSettings.skybox != null)
        {
            skyboxMaterial = RenderSettings.skybox;
            skyTintShaderID = Shader.PropertyToID("_Tint");
            skyExposureShaderID = Shader.PropertyToID("_Exposure");
        }
        else
        {
            Debug.LogWarning("No skybox material found! Skybox tint influence will be disabled.");
            useSkyboxTint = false;
        }
    }

    private void OnEnable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeUpdated += UpdateLight;
            timeManager.OnTimeOfDayChanged += OnTimeOfDayChanged;

            // Manually call UpdateLight to set the initial state
            if(timeManager != null)
            {
                UpdateLight(timeManager.GetNormalizedTime());
            }
        }
        else
        {
            // Fallback if no manager is found
            SetLightToDaySettings();
        }
    }

    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeUpdated -= UpdateLight;
            timeManager.OnTimeOfDayChanged -= OnTimeOfDayChanged;
        }
    }

    private void UpdateLight(float normalizedTime)
    {
        if (sunLight == null || timeManager == null) return;

        DayTimeManager.TimeOfDay currentTimeOfDay = timeManager.GetCurrentTimeOfDay();
        float currentCycleTime = timeManager.GetCurrentCycleTime();

        // --- Sun Rotation Logic ---
        // We rotate from the start of Dawn to the start of Dusk.
        float rotationStartTime = timeManager.DawnStart;
        float rotationEndTime = timeManager.DuskStart;
        float rotationDuration = rotationEndTime - rotationStartTime;

        // Handle time wrapping around 1.0 (e.g., if dusk is 0.8 and dawn is 0.2)
        if (rotationDuration <= 0)
        {
            rotationDuration += 1.0f;
        }

        float timeSinceRotationStart = currentCycleTime - rotationStartTime;
        if (timeSinceRotationStart < 0)
        {
            timeSinceRotationStart += 1.0f;
        }
        
        // Update rotation - a bit off since day start and day end mean something else
        float rotationProgress = timeManager.GetNormalizedTime();
        rotationProgress = Mathf.Clamp01(rotationProgress);

        if (rotationProgress <= 0.5f) {
            sunLight.transform.rotation = Quaternion.Lerp(
                Quaternion.Euler(dayStartRotation),
                Quaternion.Euler(dayEndRotation),
                rotationProgress * 2f
            );
        } else {
            sunLight.transform.rotation = Quaternion.Lerp(
                Quaternion.Euler(dayEndRotation),
                Quaternion.Euler(dayStartRotation),
                (rotationProgress - 0.5f) * 2f
            );
        }

        // --- Intensity, Temperature, and Fog Logic ---
        if (currentTimeOfDay == DayTimeManager.TimeOfDay.Dusk)
        {
            // Transitioning from day to night
            float duskProgress = (currentCycleTime - timeManager.DuskStart) / timeManager.TransitionDuration;
            duskProgress = Mathf.Clamp01(duskProgress); // Ensure progress is 0-1

            sunLight.intensity = Mathf.Lerp(dayIntensity, 0f, duskProgress);
            float baseTemperature = Mathf.Lerp(dayTemperature, nightTemperature, duskProgress);
            sunLight.colorTemperature = ApplySkyboxInfluence(baseTemperature);

            // Fog rolls in
            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = Mathf.Lerp(0, maxFogDensity, duskProgress);
            }
            
            // Update skybox intensity
            if (controlSkyboxIntensity && skyboxMaterial != null)
            {
                float skyIntensity = Mathf.Lerp(daySkyboxIntensity, nightSkyboxIntensity, duskProgress);
                skyboxMaterial.SetFloat(skyExposureShaderID, skyIntensity);
            }
        }
        else if (currentTimeOfDay == DayTimeManager.TimeOfDay.Dawn)
        {
            // Transitioning from night to day
            float dawnProgress = (currentCycleTime - timeManager.DawnStart) / timeManager.TransitionDuration;
            dawnProgress = Mathf.Clamp01(dawnProgress); // Ensure progress is 0-1

            sunLight.intensity = Mathf.Lerp(0f, dayIntensity, dawnProgress);
            float baseTemperature = Mathf.Lerp(nightTemperature, dayTemperature, dawnProgress);
            sunLight.colorTemperature = ApplySkyboxInfluence(baseTemperature);

            // Fog rolls out
            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = Mathf.Lerp(maxFogDensity, 0, dawnProgress);
            }
            
            // Update skybox intensity
            if (controlSkyboxIntensity && skyboxMaterial != null)
            {
                float skyIntensity = Mathf.Lerp(nightSkyboxIntensity, daySkyboxIntensity, dawnProgress);
                skyboxMaterial.SetFloat(skyExposureShaderID, skyIntensity);
            }
        }
        else
        {
            // During stable Day or Night, just apply skybox influence
            float baseTemperature = currentTimeOfDay == DayTimeManager.TimeOfDay.Day ? dayTemperature : nightTemperature;
            sunLight.colorTemperature = ApplySkyboxInfluence(baseTemperature);
        }

        // Send tint to shaders
        Color tempColor = Mathf.CorrelatedColorTemperatureToRGB(sunLight.colorTemperature);
        Color finalColor = sunLight.color * tempColor; // include base color if you use it

        Shader.SetGlobalColor("_SunColor", finalColor);
    }

    private float ApplySkyboxInfluence(float baseTemperature)
    {
        if (!useSkyboxTint || skyboxMaterial == null)
            return baseTemperature;

        // Get skybox tint color
        Color skyTint = skyboxMaterial.GetColor(skyTintShaderID);

        // Convert RGB to approximate color temperature
        float temperatureFromSkybox = ColorToTemperature(skyTint);

        // Blend between base temperature and skybox-influenced temperature
        float influencedTemperature = Mathf.Lerp(baseTemperature, temperatureFromSkybox, skyboxInfluence);

        // Clamp to reasonable values
        return Mathf.Clamp(influencedTemperature, minTemperature, maxTemperature);
    }

    private float ColorToTemperature(Color color)
    {
        // Simple method to convert color to approximate temperature
        // This is a simplified approximation

        // Calculate color "warmth" based on RGB ratios
        float warmth = (color.r + color.g * 0.5f) / (color.b + 0.1f);

        // Map warmth to temperature range (warmer = lower temperature, cooler = higher temperature)
        float temperature = Mathf.Lerp(2000f, 12000f, 1f / (warmth + 0.5f));

        return temperature;
    }

    private void OnTimeOfDayChanged(DayTimeManager.TimeOfDay newTimeOfDay)
    {
        // Snap to fixed values when entering day or night
        if (newTimeOfDay == DayTimeManager.TimeOfDay.Day)
        {
            SetLightToDaySettings();
        }
        else if (newTimeOfDay == DayTimeManager.TimeOfDay.Night)
        {
            SetLightToNightSettings();
        }
    }

    private void SetLightToDaySettings()
    {
        if (sunLight == null) return;
        sunLight.intensity = dayIntensity;
        sunLight.colorTemperature = ApplySkyboxInfluence(dayTemperature);
        
        // Rotation is handled by UpdateLight, no need to snap it here
        
        // Snap fog to off
        if (controlFog)
        {
            RenderSettings.fog = false;
            RenderSettings.fogDensity = 0f;
        }

        // Snap skybox intensity
        if (controlSkyboxIntensity && skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat(skyExposureShaderID, daySkyboxIntensity);
        }
    }

    private void SetLightToNightSettings()
    {
        if (sunLight == null) return;
        sunLight.intensity = 0f;
        sunLight.colorTemperature = ApplySkyboxInfluence(nightTemperature);

        // Snap rotation to the final "day end" rotation
        // sunLight.transform.rotation = Quaternion.Euler(dayEndRotation);

        // Snap fog to on
        if (controlFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = maxFogDensity;
        }

        // Snap skybox intensity
        if (controlSkyboxIntensity && skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat(skyExposureShaderID, nightSkyboxIntensity);
        }
    }
}