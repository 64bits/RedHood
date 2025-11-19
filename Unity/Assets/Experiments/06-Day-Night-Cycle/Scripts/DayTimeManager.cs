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
    
    [Header("Beacon Detection")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] [Range(0f, 6f)] private float distanceFromBeacon = 0f;
    [SerializeField] private float maxBeaconDistance = 6f;
    [SerializeField] private float beaconDistanceChangeThreshold = 0.1f; // Minimum change to trigger event
    
    private float currentCycleTime = 0f;
    private float cycleDuration;
    private int beaconLayer;
    private float previousBeaconDistance = 0f;
    
    // Time pause/resume variables
    private bool isTimePaused = false;
    private float savedCycleTime = 0f;
    private bool hasNotifiedPause = false;
    
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
    public System.Action<float> OnBeaconDistanceChanged; // Passes distance from beacon
    
    private void Awake()
    {
        cycleDuration = dayDuration + nightDuration;
        beaconLayer = 1 << 9; // 9th layer (Beacon)
        previousBeaconDistance = maxBeaconDistance;
    }
    
    private void OnEnable()
    {
        // Notify all listeners of initial state
        NotifyTimeUpdate();
        NotifyBeaconDistanceUpdate();
    }
    
    private void Update()
    {
        // Update distance from beacon
        UpdateBeaconDistance();
        
        // Check for pause/resume conditions
        CheckPauseResumeConditions();
        
        // Don't update time if paused
        if (!isTimePaused)
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
        }
        
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
    
    private void CheckPauseResumeConditions()
    {
        // Pause condition: distance >= 3 and not already paused
        if (distanceFromBeacon >= 3f && !isTimePaused)
        {
            PauseTime();
        }
        // Resume condition: distance <= 2 and currently paused
        else if (distanceFromBeacon <= 2f && isTimePaused)
        {
            ResumeTime();
        }
    }
    
    private void PauseTime()
    {
        isTimePaused = true;
        savedCycleTime = currentCycleTime;
        hasNotifiedPause = false;
        
        // Send one last notification as if it's middle of night (0.75)
        currentCycleTime = 0.75f * cycleDuration;
        NotifyTimeUpdate();
        hasNotifiedPause = true;
    }
    
    private void ResumeTime()
    {
        isTimePaused = false;
        
        // Restore the saved time
        currentCycleTime = savedCycleTime;
        
        // Send notification with the restored time
        NotifyTimeUpdate();
    }
    
    private void UpdateBeaconDistance()
    {
        if (playerTransform == null)
        {
            distanceFromBeacon = maxBeaconDistance;
            NotifyBeaconDistanceUpdate();
            return;
        }
        
        // Perform overlap sphere check at player position
        Collider[] beacons = Physics.OverlapSphere(playerTransform.position, maxBeaconDistance, beaconLayer);
        
        if (beacons.Length == 0)
        {
            // No beacons nearby
            distanceFromBeacon = maxBeaconDistance;
            NotifyBeaconDistanceUpdate();
            return;
        }
        
        // Find the closest beacon
        float closestDistance = float.MaxValue;
        
        foreach (Collider beacon in beacons)
        {
            // Get closest point on beacon collider to player
            Vector3 closestPoint = beacon.ClosestPoint(playerTransform.position);
            float distanceToEdge = Vector3.Distance(playerTransform.position, closestPoint);
            
            // If player is inside the collider, ClosestPoint returns the player position
            // So distance will be 0 or very close to 0
            if (distanceToEdge < closestDistance)
            {
                closestDistance = distanceToEdge;
            }
        }
        
        // Update distance, clamped to max range
        distanceFromBeacon = Mathf.Clamp(closestDistance, 0f, maxBeaconDistance);
        NotifyBeaconDistanceUpdate();
    }
    
    private void NotifyBeaconDistanceUpdate()
    {
        // Only notify if the distance has changed beyond threshold
        if (Mathf.Abs(distanceFromBeacon - previousBeaconDistance) >= beaconDistanceChangeThreshold)
        {
            previousBeaconDistance = distanceFromBeacon;
            OnBeaconDistanceChanged?.Invoke(distanceFromBeacon);
            
            // TODO: Move this somewhere else?
            Shader.SetGlobalFloat("_VignetteSize", 1f - (0.1f * distanceFromBeacon));
        }
    }
    
    private void NotifyTimeUpdate()
    {
        // Don't send notifications if paused (except for the one-time pause notification)
        if (isTimePaused && hasNotifiedPause)
        {
            return;
        }
        
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
    
    public float GetDistanceFromBeacon()
    {
        return distanceFromBeacon;
    }
    
    public bool IsTimePaused()
    {
        return isTimePaused;
    }
}