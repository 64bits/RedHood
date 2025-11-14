using UnityEngine;
using System;

[ExecuteAlways]
public class SkyboxTimelapseManager : MonoBehaviour
{
    [Header("Skybox Material")]
    [SerializeField] private Material skyboxMaterial;
    
    [Header("Manager Reference")]
    [SerializeField] private DayTimeManager timeManager;
    
    [Header("Cubemap Settings")]
    [SerializeField] private Cubemap dayCubemap;
    [SerializeField] private Cubemap nightCubemap;
    [Range(0f, 1f)]
    [SerializeField] private float cubemapBlendThreshold = 0.3f; // How much overlap in transitions
    
    [Header("Color Tints")]
    [SerializeField] private Color dawnTint = new Color(1.2f, 0.9f, 0.8f, 1f);
    [SerializeField] private Color dayTint = Color.white;
    [SerializeField] private Color duskTint = new Color(1.1f, 0.8f, 0.9f, 1f);
    [SerializeField] private Color nightTint = new Color(0.4f, 0.5f, 1.0f, 1f);
    
    [Header("Exposure Settings")]
    [SerializeField] private float dawnExposure = 0.5f;
    [SerializeField] private float dayExposure = 1.2f;
    [SerializeField] private float duskExposure = 0.4f;
    [SerializeField] private float nightExposure = 0.1f;
    
    [Header("Transition Times (24-hour format)")]
    [SerializeField] private float dawnStart = 5f;   // 05:00 - earlier for blending
    [SerializeField] private float dawnEnd = 7f;     // 07:00
    [SerializeField] private float dayEnd = 17f;     // 17:00 - earlier for blending
    [SerializeField] private float duskEnd = 19f;    // 19:00
    
    // Shader property IDs for better performance
    private static readonly int ExposureKey = Shader.PropertyToID("_Exposure");
    private static readonly int TintKey = Shader.PropertyToID("_Tint");
    private static readonly int TexKey = Shader.PropertyToID("_Tex");
    private static readonly int SecondTexKey = Shader.PropertyToID("_Tex2");
    private static readonly int BlendFactorKey = Shader.PropertyToID("_BlendFactor");
    
    // For custom shader with blending support
    [Header("Advanced Blending")]
    [SerializeField] private bool useCubemapBlending = true;
    [SerializeField] private Material blendedSkyboxMaterial; // Optional: material with blend support

    private void Awake() {
        if (timeManager == null)
        {
            timeManager = FindObjectOfType<DayTimeManager>();
            if (timeManager == null)
            {
                Debug.LogError("DayTimeManager not found!");
            }
        }
    }

    private void OnEnable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeUpdated += UpdateSkybox;
        }
    }
    
    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeUpdated -= UpdateSkybox;
        }
    }

    private void Start()
    {
        if (skyboxMaterial == null)
        {
            Debug.LogWarning("Skybox material not assigned. Using current skybox material.");
            skyboxMaterial = RenderSettings.skybox;
        }
        
        // If blended material is assigned, use it instead
        if (blendedSkyboxMaterial != null)
        {
            skyboxMaterial = blendedSkyboxMaterial;
        }
        
        UpdateSkybox();
    }
    
    public void UpdateSkybox(float normalizedTime = 0)
    {
        if (skyboxMaterial == null) return;
        
        // Calculate current hour (0-24 scale)
        float currentHour = GetCurrentHour(normalizedTime);
        
        // Determine day/night cycle phase and calculate blend factors
        CalculateSkyParameters(currentHour, out Color currentTint, out float currentExposure, out float cubemapBlend);
        
        // Apply to skybox material
        skyboxMaterial.SetFloat(ExposureKey, currentExposure);
        skyboxMaterial.SetColor(TintKey, currentTint);
        
        // Apply cubemap blending
        ApplyCubemapBlending(cubemapBlend);
        
        // Apply to render settings
        RenderSettings.skybox = skyboxMaterial;
        
        // Force Unity to update the skybox
        DynamicGI.UpdateEnvironment();
    }
    
    private float GetCurrentHour(float normalizedTime)
    {
        // Convert 0-1 time to 0-24 hours (starting at 06:00)
        // 0.0 = 06:00, 0.5 = 18:00, 1.0 = 06:00 next day
        return (normalizedTime * 24f + 6f) % 24f;
    }
    
    private void CalculateSkyParameters(float currentHour, out Color tint, out float exposure, out float cubemapBlend)
    {
        cubemapBlend = 0f; // 0 = full day, 1 = full night
        
        if (currentHour >= dawnStart && currentHour < dawnEnd)
        {
            // Dawn transition
            float dawnProgress = (currentHour - dawnStart) / (dawnEnd - dawnStart);
            tint = Color.Lerp(nightTint, dawnTint, dawnProgress);
            exposure = Mathf.Lerp(nightExposure, dawnExposure, dawnProgress);
            
            // Cubemap blend: transition from night to day during dawn
            cubemapBlend = Mathf.Clamp01(1f - dawnProgress * (1f + cubemapBlendThreshold));
        }
        else if (currentHour >= dawnEnd && currentHour < dayEnd)
        {
            // Day
            tint = dayTint;
            exposure = dayExposure;
            cubemapBlend = 0f; // Full day cubemap
        }
        else if (currentHour >= dayEnd && currentHour < duskEnd)
        {
            // Dusk transition
            float duskProgress = (currentHour - dayEnd) / (duskEnd - dayEnd);
            tint = Color.Lerp(dayTint, duskTint, duskProgress);
            exposure = Mathf.Lerp(dayExposure, duskExposure, duskProgress);
            
            // Cubemap blend: transition from day to night during dusk
            cubemapBlend = Mathf.Clamp01(duskProgress * (1f + cubemapBlendThreshold));
        }
        else
        {
            // Night (before dawn or after dusk)
            float nightProgress;
            if (currentHour < dawnStart)
            {
                // Early morning (before dawn)
                nightProgress = (currentHour + (24f - duskEnd)) / (dawnStart + (24f - duskEnd));
                tint = Color.Lerp(duskTint, nightTint, nightProgress);
                exposure = Mathf.Lerp(duskExposure, nightExposure, nightProgress);
            }
            else
            {
                // Late night (after dusk)
                nightProgress = (currentHour - duskEnd) / (24f - duskEnd + dawnStart);
                tint = Color.Lerp(duskTint, nightTint, nightProgress);
                exposure = Mathf.Lerp(duskExposure, nightExposure, nightProgress);
            }
            
            cubemapBlend = 1f; // Full night cubemap
            
            // For very late night, blend from night to dawn
            if (currentHour > duskEnd)
            {
                float lateNightProgress = (currentHour - duskEnd) / (24f - duskEnd + dawnStart);
                if (lateNightProgress > 0.7f) // Start blending back to day later in the night
                {
                    float dawnApproach = (lateNightProgress - 0.7f) / 0.3f;
                    tint = Color.Lerp(nightTint, dawnTint, dawnApproach);
                    exposure = Mathf.Lerp(nightExposure, dawnExposure, dawnApproach);
                    cubemapBlend = Mathf.Clamp01(1f - dawnApproach * cubemapBlendThreshold);
                }
            }
        }
    }
    
    private void ApplyCubemapBlending(float blendFactor)
    {
        if (!useCubemapBlending || dayCubemap == null || nightCubemap == null)
        {
            // Fallback: simple cubemap switching
            Cubemap currentCubemap = blendFactor < 0.5f ? dayCubemap : nightCubemap;
            if (currentCubemap != null)
            {
                skyboxMaterial.SetTexture(TexKey, currentCubemap);
            }
            return;
        }
        
        // Check if shader supports blending (has the required properties)
        bool shaderSupportsBlending = skyboxMaterial.HasProperty(SecondTexKey) && 
                                     skyboxMaterial.HasProperty(BlendFactorKey);
        
        if (shaderSupportsBlending)
        {
            // Use advanced cubemap blending
            skyboxMaterial.SetTexture(TexKey, dayCubemap);
            skyboxMaterial.SetTexture(SecondTexKey, nightCubemap);
            skyboxMaterial.SetFloat(BlendFactorKey, blendFactor);
        }
        else
        {
            // Fallback: manually blend between two materials or use lerping
            ApplyFallbackBlending(blendFactor);
        }
    }
    
    private void ApplyFallbackBlending(float blendFactor)
    {
        // Simple approach: choose which cubemap to use based on blend factor
        // You could extend this to use two materials and crossfade between them
        if (blendFactor <= 0.1f)
        {
            skyboxMaterial.SetTexture(TexKey, dayCubemap);
        }
        else if (blendFactor >= 0.9f)
        {
            skyboxMaterial.SetTexture(TexKey, nightCubemap);
        }
        else
        {
            // During transitions, you might want to use a pre-blended cubemap
            // or implement a more sophisticated fallback
            skyboxMaterial.SetTexture(TexKey, blendFactor < 0.5f ? dayCubemap : nightCubemap);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up material properties if needed
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat(ExposureKey, 1f);
            skyboxMaterial.SetColor(TintKey, Color.white);
            if (skyboxMaterial.HasProperty(BlendFactorKey))
            {
                skyboxMaterial.SetFloat(BlendFactorKey, 0f);
            }
        }
    }
}