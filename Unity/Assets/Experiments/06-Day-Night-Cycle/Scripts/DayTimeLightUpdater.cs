using UnityEngine;

public class DayTimeLightUpdater : MonoBehaviour
{
    [Header("Light Reference")]
    [SerializeField] private Light sunLight;
    
    [Header("Day Settings")]
    [SerializeField] private float dayIntensity = 1.0f;
    [SerializeField] private float dayTemperature = 6500f;
    
    [Header("Night Settings")]
    [SerializeField] private float nightIntensity = 0.05f;
    [SerializeField] private float nightTemperature = 3000f;
    
    [Header("Skybox Influence")]
    [SerializeField] private bool useSkyboxTint = true;
    [SerializeField] private float skyboxInfluence = 0.3f; // How much skybox affects light color (0-1)
    [SerializeField] private float minTemperature = 2000f; // Minimum temperature when influenced by skybox
    [SerializeField] private float maxTemperature = 10000f; // Maximum temperature when influenced by skybox
    
    [Header("Orbit Settings")]
    [SerializeField] private Transform orbitCenter; // Center point for the sun's orbit
    [SerializeField] private float orbitRadius = 100f; // Distance from center
    [SerializeField] private Vector3 dayPosition = new Vector3(0f, 100f, 0f); // Top position (noon)
    [SerializeField] private Vector3 nightPosition = new Vector3(0f, -100f, 0f); // Bottom position (midnight)
    [SerializeField] private AnimationCurve orbitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Manager Reference")]
    [SerializeField] private DayTimeManager timeManager;
    
    private Material skyboxMaterial;
    private int skyTintShaderID;
    
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
        
        // Set orbit center to this object if not assigned
        if (orbitCenter == null)
        {
            orbitCenter = this.transform;
        }
        
        // Get skybox material and shader property ID
        if (RenderSettings.skybox != null)
        {
            skyboxMaterial = RenderSettings.skybox;
            skyTintShaderID = Shader.PropertyToID("_Tint");
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
        }
        
        // Initialize to day settings
        SetLightToDaySettings();
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
        
        // Update light position and rotation based on normalized time
        UpdateLightOrbit(normalizedTime);
        
        if (currentTimeOfDay == DayTimeManager.TimeOfDay.Dusk)
        {
            // Transitioning from day to night
            float duskProgress = (currentCycleTime - timeManager.DuskStart) / timeManager.TransitionDuration;
            sunLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, duskProgress);
            float baseTemperature = Mathf.Lerp(dayTemperature, nightTemperature, duskProgress);
            sunLight.colorTemperature = ApplySkyboxInfluence(baseTemperature);
        }
        else if (currentTimeOfDay == DayTimeManager.TimeOfDay.Dawn)
        {
            // Transitioning from night to day
            float dawnProgress = (currentCycleTime - timeManager.DawnStart) / timeManager.TransitionDuration;
            sunLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, dawnProgress);
            float baseTemperature = Mathf.Lerp(nightTemperature, dayTemperature, dawnProgress);
            sunLight.colorTemperature = ApplySkyboxInfluence(baseTemperature);
        }
        else
        {
            // During day or night, still apply skybox influence
            float baseTemperature = currentTimeOfDay == DayTimeManager.TimeOfDay.Day ? dayTemperature : nightTemperature;
            sunLight.colorTemperature = ApplySkyboxInfluence(baseTemperature);
        }

        // Send tint to shaders
        Color tempColor = Mathf.CorrelatedColorTemperatureToRGB(sunLight.colorTemperature);
        Color finalColor = sunLight.color * tempColor; // include base color if you use it

        Shader.SetGlobalColor("_SunColor", finalColor);
    }
    
    private void UpdateLightOrbit(float normalizedTime)
    {
        if (orbitCenter == null) return;
        
        float orbitT = normalizedTime;
        
        // Use animation curve for smoother orbit if provided
        if (orbitCurve != null && orbitCurve.length > 0)
        {
            orbitT = orbitCurve.Evaluate(normalizedTime);
        }
        
        // Calculate position on circular orbit
        Vector3 orbitPosition = CalculateOrbitPosition(orbitT);
        
        // Update position
        sunLight.transform.position = orbitCenter.position + orbitPosition;
        
        // Make the light look at the orbit center (so shadows and lighting work correctly)
        sunLight.transform.LookAt(orbitCenter.position);
        
        // Alternative: Manual rotation control
        // sunLight.transform.rotation = Quaternion.LookRotation(-orbitPosition.normalized);
    }
    
    private Vector3 CalculateOrbitPosition(float t)
    {
        // Convert normalized time to radians (full circle)
        float angle = t * Mathf.PI * 2f;
        
        // Calculate position on circular orbit around the center
        float x = Mathf.Sin(angle) * orbitRadius;
        float z = Mathf.Cos(angle) * orbitRadius;
        float y = Mathf.Sin(angle) * orbitRadius * 0.5f; // Slight vertical variation
        
        return new Vector3(x, y, z);
        
        // Alternative: Simple lerp between predefined positions
        // return Vector3.Lerp(dayPosition, nightPosition, t);
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
        // This is a simplified approximation - you might want to use a more sophisticated method
        
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
        
        // Set to day position and rotation
        if (orbitCenter != null)
        {
            sunLight.transform.position = orbitCenter.position + dayPosition;
            sunLight.transform.LookAt(orbitCenter.position);
        }
    }
    
    private void SetLightToNightSettings()
    {
        if (sunLight == null) return;
        sunLight.intensity = nightIntensity;
        sunLight.colorTemperature = ApplySkyboxInfluence(nightTemperature);
        
        // Set to night position and rotation
        if (orbitCenter != null)
        {
            sunLight.transform.position = orbitCenter.position + nightPosition;
            sunLight.transform.LookAt(orbitCenter.position);
        }
    }
    
    // Optional: Visualize the orbit path in the editor
    private void OnDrawGizmosSelected()
    {
        if (orbitCenter == null) return;
        
        Gizmos.color = Color.yellow;
        
        // Draw orbit path
        int segments = 36;
        Vector3 previousPoint = orbitCenter.position + CalculateOrbitPosition(0);
        
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = orbitCenter.position + CalculateOrbitPosition(t);
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
        
        // Draw center point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(orbitCenter.position, 1f);
    }
}