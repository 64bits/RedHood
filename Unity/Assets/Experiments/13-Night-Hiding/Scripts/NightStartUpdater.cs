using UnityEngine;

public class NightStartUpdater : MonoBehaviour
{
    private DayTimeManager dayTimeManager;
    
    private void Start()
    {
        // Find the DayTimeManager in the scene
        dayTimeManager = FindObjectOfType<DayTimeManager>();
        
        if (dayTimeManager != null)
        {
            // Subscribe to the time of day changed event
            dayTimeManager.OnTimeOfDayChanged += HandleTimeOfDayChanged;
        }
        else
        {
            Debug.LogError("DayTimeManager not found in scene!");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe when this object is destroyed to prevent memory leaks
        if (dayTimeManager != null)
        {
            dayTimeManager.OnTimeOfDayChanged -= HandleTimeOfDayChanged;
        }
    }
    
    private void HandleTimeOfDayChanged(DayTimeManager.TimeOfDay newTimeOfDay)
    {
        if (newTimeOfDay == DayTimeManager.TimeOfDay.Night)
        {
            Debug.Log("Night Started");
        }
    }
}