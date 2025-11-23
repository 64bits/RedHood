using UnityEngine;
using System.Collections;

public class DayTimeManager : MonoBehaviour
{
    [Header("Cycle Settings")]
    [SerializeField] private float dayDuration = 180f; // Day duration in seconds
    [SerializeField] private float nightDuration = 180f; // Night duration in seconds
    [SerializeField] private float transitionDuration = 3f; // Dawn/Dusk duration
    
    [Header("Debug / Manual Control")]
    [SerializeField] private bool useManualTimeControl = false;
    [SerializeField] [Range(0f, 2f)] private float manualTimeOfDay = 0f;

    // Time State
    private float currentCycleTime = 0f;
    private float cycleDuration;
    
    // Shader keyword for darkness
    private const string DARKNESS_FEATURE = "ENABLE_DARKNESS";

    // Time periods (calculated properties)
    public float DayStart => 0f;
    public float DuskStart => dayDuration - transitionDuration;
    public float DuskEnd => dayDuration;
    public float NightStart => DuskEnd;
    public float DawnStart => dayDuration + nightDuration - transitionDuration;
    public float DawnEnd => cycleDuration;
    public float CycleDuration => cycleDuration;

    public enum TimeOfDay { Day, Dusk, Night, Dawn }
    private TimeOfDay currentTimeOfDay = TimeOfDay.Day;

    // Events
    public System.Action<TimeOfDay> OnTimeOfDayChanged;
    public System.Action<float> OnTimeUpdated; // Passes normalized time (0-1)

    private void Awake()
    {
        cycleDuration = dayDuration + nightDuration;
    }

    private void OnEnable()
    {
        NotifyTimeUpdate();
    }

    private void Update()
    {
        // 1. Calculate Time
        if (useManualTimeControl)
        {
            float normalizedManualTime = manualTimeOfDay % 1f;
            currentCycleTime = normalizedManualTime * cycleDuration;
        }
        else
        {
            currentCycleTime += Time.deltaTime;

            if (currentCycleTime >= cycleDuration)
            {
                currentCycleTime -= cycleDuration;
            }
        }

        // 2. Notify Listeners
        NotifyTimeUpdate();

        // 3. Update Shader Visuals
        UpdateShaderKeywords();
    }

    private void UpdateShaderKeywords()
    {
        // Update shader darkness keyword based on time of day
        // Night starts after 0.5 normalized time in this setup
        if (GetNormalizedTime() > 0.5f)
        {
            Shader.EnableKeyword(DARKNESS_FEATURE);
        }
        else
        {
            Shader.DisableKeyword(DARKNESS_FEATURE);
        }
    }

    private void NotifyTimeUpdate()
    {
        TimeOfDay newTimeOfDay = GetCurrentTimeOfDay();

        if (newTimeOfDay != currentTimeOfDay)
        {
            currentTimeOfDay = newTimeOfDay;
            OnTimeOfDayChanged?.Invoke(newTimeOfDay);
        }

        OnTimeUpdated?.Invoke(GetNormalizedTime());
    }

    // --- Getters & Helpers ---

    public TimeOfDay GetCurrentTimeOfDay()
    {
        if (currentCycleTime >= DayStart && currentCycleTime < DuskStart) return TimeOfDay.Day;
        else if (currentCycleTime >= DuskStart && currentCycleTime < DuskEnd) return TimeOfDay.Dusk;
        else if (currentCycleTime >= NightStart && currentCycleTime < DawnStart) return TimeOfDay.Night;
        else return TimeOfDay.Dawn;
    }

    public float GetNormalizedTime()
    {
        return currentCycleTime / cycleDuration;
    }

    public float GetCurrentCycleTime()
    {
        return currentCycleTime;
    }
    
    public void SetTimeOfDay(float normalizedTime)
    {
        currentCycleTime = Mathf.Clamp01(normalizedTime) * cycleDuration;
        NotifyTimeUpdate();
    }
}