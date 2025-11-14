using UnityEngine;
using System.Collections;

public class DayTimeManager : MonoBehaviour
{
    [Header("Cycle Settings")]
    [SerializeField] private float dayDuration = 180f; // Day duration in seconds
    [SerializeField] private float nightDuration = 180f; // Night duration in seconds
    [SerializeField] private float transitionDuration = 3f; // Dawn/Dusk duration
    [SerializeField] private bool useManualTimeControl = false;
    [SerializeField] [Range(0f, 2f)] private float manualTimeOfDay = 0f;
    
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
    public float DayEnd => cycleDuration;
    public float TransitionDuration => transitionDuration;
    public float CycleDuration => cycleDuration;
    
    public enum TimeOfDay { Day, Dusk, Night, Dawn }
    private TimeOfDay currentTimeOfDay = TimeOfDay.Day;
    
    // Events for notifying listeners of time changes
    public System.Action<TimeOfDay> OnTimeOfDayChanged;
    public System.Action<float> OnTimeUpdated; // Passes normalized time (0-1)
    
    private void Awake()
    {
        cycleDuration = dayDuration + nightDuration;
    }
    
    private void OnEnable()
    {
        // Notify all listeners of initial state
        NotifyTimeUpdate();
    }
    
    private void Update()
    {
        // Manual time control overrides automatic cycle
        if (useManualTimeControl)
        {
            float normalizedManualTime = manualTimeOfDay % 1f;
            currentCycleTime = normalizedManualTime * cycleDuration;
        }
        else
        {
            // Automatic time progression
            currentCycleTime += Time.deltaTime;
            
            if (currentCycleTime >= cycleDuration)
            {
                currentCycleTime -= cycleDuration;
            }
        }
        
        NotifyTimeUpdate();
        
        // Update shader darkness keyword based on time of day
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
        
        // Notify if time of day changed
        if (newTimeOfDay != currentTimeOfDay)
        {
            currentTimeOfDay = newTimeOfDay;
            OnTimeOfDayChanged?.Invoke(newTimeOfDay);
        }
        
        // Always notify of time update
        OnTimeUpdated?.Invoke(GetNormalizedTime());
    }
    
    public TimeOfDay GetCurrentTimeOfDay()
    {
        if (currentCycleTime >= DayStart && currentCycleTime < DuskStart)
        {
            return TimeOfDay.Day;
        }
        else if (currentCycleTime >= DuskStart && currentCycleTime < DuskEnd)
        {
            return TimeOfDay.Dusk;
        }
        else if (currentCycleTime >= NightStart && currentCycleTime < DawnStart)
        {
            return TimeOfDay.Night;
        }
        else
        {
            return TimeOfDay.Dawn;
        }
    }
    
    public void SetTimeOfDay(float normalizedTime)
    {
        currentCycleTime = Mathf.Clamp01(normalizedTime) * cycleDuration;
        NotifyTimeUpdate();
    }
    
    public float GetNormalizedTime()
    {
        return currentCycleTime / cycleDuration;
    }
    
    public float GetCurrentCycleTime()
    {
        return currentCycleTime;
    }
    
    public string GetTimeOfDayString()
    {
        return currentTimeOfDay.ToString();
    }
    
    public TimeOfDay GetCurrentPeriod()
    {
        return currentTimeOfDay;
    }
}