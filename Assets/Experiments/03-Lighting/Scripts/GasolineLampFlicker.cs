using UnityEngine;

public class GasolineLampFlicker : MonoBehaviour
{
    public Light pointLight;
    public float baseRange = 2.75f;
    public float flickerIntensity = 0.03f;
    public float flickerSpeed = 2f;
    
    private float timeOffset;

    void Start()
    {
        if (pointLight == null) pointLight = GetComponent<Light>();
        if (pointLight != null) baseRange = pointLight.range;
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (pointLight == null) return;
        
        float noise = Mathf.PerlinNoise((Time.time + timeOffset) * flickerSpeed, timeOffset) * 2f - 1f;
        pointLight.range = baseRange * (1f + noise * flickerIntensity);
    }
}