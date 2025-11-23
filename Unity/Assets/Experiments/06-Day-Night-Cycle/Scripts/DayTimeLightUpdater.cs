using UnityEngine;

public class DayTimeLightUpdater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayTimeManager timeManager;
    [SerializeField] private Light sunLight;

    [Header("Color Temperature")]
    [SerializeField] private float dayTemperature = 6500f;
    [SerializeField] private float nightTemperature = 3000f;

    [Header("Skybox Color Influence")]
    [SerializeField] private bool useSkyboxTint = true;
    [SerializeField] [Range(0f, 1f)] private float skyboxInfluence = 0.3f;
    [SerializeField] private float minTemperature = 2000f;
    [SerializeField] private float maxTemperature = 10000f;

    [Header("Rotation Settings")]
    [Tooltip("Rotation at normalized time 0.0 (Start of Day)")]
    [SerializeField] private Vector3 dayStartRotation = new Vector3(5, 0, 0);
    [Tooltip("Rotation at normalized time 0.5 (Start of Night)")]
    [SerializeField] private Vector3 dayEndRotation = new Vector3(175, 0, 0);

    private Material skyboxMaterial;
    private int skyTintShaderID;

    private void Awake()
    {
        if (timeManager == null) timeManager = FindObjectOfType<DayTimeManager>();
        
        if (RenderSettings.skybox != null)
        {
            skyboxMaterial = RenderSettings.skybox;
            skyTintShaderID = Shader.PropertyToID("_Tint");
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

        // 1. Update Rotation (Independent of visual phase, depends on time arc)
        UpdateRotation(normalizedTime);

        // 2. Calculate Visual Phase for Color
        float dayFactor = CalculateDayFactor(normalizedTime);
        
        // 3. Update Color
        UpdateColor(dayFactor);
    }

    private void UpdateRotation(float normalizedTime)
    {
        // Day Arc (0 -> 0.5)
        if (normalizedTime <= 0.5f)
        {
            float rotProgress = normalizedTime / 0.5f;
            sunLight.transform.rotation = Quaternion.Lerp(
                Quaternion.Euler(dayStartRotation),
                Quaternion.Euler(dayEndRotation),
                rotProgress
            );
        }
        // Night Return Arc (0.5 -> 1.0)
        else
        {
            float rotProgress = (normalizedTime - 0.5f) / 0.5f;
            sunLight.transform.rotation = Quaternion.Lerp(
                Quaternion.Euler(dayEndRotation),
                Quaternion.Euler(dayStartRotation),
                rotProgress
            );
        }
    }

    private float CalculateDayFactor(float normalizedTime)
    {
        float cycleDuration = timeManager.CycleDuration;
        float nDuskStart = timeManager.DuskStart / cycleDuration;
        float nNightStart = timeManager.NightStart / cycleDuration;
        float nDawnStart = timeManager.DawnStart / cycleDuration;

        if (normalizedTime < nDuskStart) return 1.0f;
        if (normalizedTime >= nNightStart && normalizedTime < nDawnStart) return 0.0f;

        if (normalizedTime >= nDuskStart && normalizedTime < nNightStart)
        {
            float range = nNightStart - nDuskStart;
            float progress = (normalizedTime - nDuskStart) / range;
            return 1.0f - progress;
        }
        
        float rangeDawn = 1.0f - nDawnStart;
        float progressDawn = (normalizedTime - nDawnStart) / rangeDawn;
        return progressDawn;
    }

    private void UpdateColor(float dayFactor)
    {
        // Blend base temperature
        float currentBaseTemp = Mathf.Lerp(nightTemperature, dayTemperature, dayFactor);
        
        // Apply Skybox tint influence
        sunLight.colorTemperature = ApplySkyboxInfluence(currentBaseTemp);
        
        // Apply actual color to light
        Color tempColor = Mathf.CorrelatedColorTemperatureToRGB(sunLight.colorTemperature);
        sunLight.color = tempColor;
        
        // Set global shader variable for other objects (water, foliage, etc)
        Shader.SetGlobalColor("_SunColor", sunLight.color);
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