using UnityEngine;

public class GasolineLampFlicker : MonoBehaviour
{
    [Header("Light Reference")]
    public Light pointLight;
    
    [Header("Flicker Settings")]
    [Tooltip("Base range of the light")]
    public float baseRange = 5f;
    
    [Tooltip("Intensity of the flicker (0-1)")]
    [Range(0f, 1f)]
    public float flickerIntensity = 0.05f; // 5% modulation
    
    [Tooltip("Speed of the main flicker")]
    public float flickerSpeed = 3f;
    
    [Header("Noise Settings")]
    [Tooltip("Speed of the noise variation")]
    public float noiseSpeed = 10f;
    
    [Tooltip("Intensity of the noise")]
    [Range(0f, 1f)]
    public float noiseIntensity = 0.02f;
    
    [Header("Random Fluctuations")]
    [Tooltip("Occasional larger flickers")]
    public bool enableRandomFlickers = true;
    
    [Tooltip("Chance of random flicker per second")]
    public float randomFlickerChance = 0.3f;
    
    [Tooltip("Intensity of random flickers")]
    public float randomFlickerIntensity = 0.1f;
    
    [Tooltip("Duration of random flickers")]
    public float randomFlickerDuration = 0.1f;
    
    private float timeOffset;
    private float randomFlickerTimer = 0f;
    private float randomFlickerAmount = 0f;

    void Start()
    {
        // If no light is assigned, try to get one from this GameObject
        if (pointLight == null)
        {
            pointLight = GetComponent<Light>();
        }
        
        // Validate that we have a point light
        if (pointLight != null && pointLight.type != LightType.Point)
        {
            Debug.LogWarning("GasolineLampFlicker is designed for Point lights, but attached light is: " + pointLight.type);
        }
        
        // Set base range if light exists
        if (pointLight != null)
        {
            baseRange = pointLight.range;
        }
        
        // Random time offset for variation between instances
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (pointLight == null) return;

        float time = Time.time + timeOffset;
        
        // Base sine wave flicker
        float sineFlicker = Mathf.Sin(time * flickerSpeed) * flickerIntensity;
        
        // Perlin noise for natural variation
        float noise = Mathf.PerlinNoise(time * noiseSpeed, timeOffset) * 2f - 1f;
        float noiseFlicker = noise * noiseIntensity;
        
        // Combine base flickers
        float combinedFlicker = sineFlicker + noiseFlicker;
        
        // Handle random larger flickers
        if (enableRandomFlickers)
        {
            HandleRandomFlickers(time);
            combinedFlicker += randomFlickerAmount;
        }
        
        // Calculate final range (base range ± 5% + variations)
        float finalRange = baseRange * (1f + combinedFlicker);
        
        // Apply to light
        pointLight.range = finalRange;
        
        // Optional: Also modulate intensity for more realistic effect
        pointLight.intensity = 1f + combinedFlicker * 0.5f;
    }
    
    private void HandleRandomFlickers(float currentTime)
    {
        // Count down random flicker timer
        if (randomFlickerTimer > 0f)
        {
            randomFlickerTimer -= Time.deltaTime;
            // Fade out the random flicker
            randomFlickerAmount = Mathf.Lerp(0f, randomFlickerAmount, randomFlickerTimer / randomFlickerDuration);
        }
        else
        {
            randomFlickerAmount = 0f;
            
            // Random chance to trigger a new flicker
            if (Random.Range(0f, 1f) < randomFlickerChance * Time.deltaTime)
            {
                randomFlickerTimer = randomFlickerDuration;
                randomFlickerAmount = Random.Range(-randomFlickerIntensity, randomFlickerIntensity);
            }
        }
    }

    // Method to reset to base range
    public void ResetToBaseRange()
    {
        if (pointLight != null)
        {
            pointLight.range = baseRange;
        }
    }

    // Method to manually trigger a flicker
    public void TriggerFlicker(float intensity, float duration)
    {
        randomFlickerTimer = duration;
        randomFlickerAmount = intensity;
    }
}