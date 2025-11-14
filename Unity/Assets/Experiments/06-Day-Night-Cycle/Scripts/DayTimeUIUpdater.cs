using UnityEngine;

public class DayTimeUIUpdater : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private RectTransform radialFillImage;
    [SerializeField] private float arcRadius = 100f;
    [SerializeField] private TMPro.TextMeshProUGUI timeText;
    
    [Header("Manager Reference")]
    [SerializeField] private DayTimeManager timeManager;
    
    private void Awake()
    {
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
            timeManager.OnTimeUpdated += UpdateUI;
        }
    }
    
    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnTimeUpdated -= UpdateUI;
        }
    }
    
    private void UpdateUI(float normalizedTime)
    {
        // Rotate the radial fill image
        // normalizedTime 0.5 = midday = -135°
        if (radialFillImage != null)
        {
            float rotation = Mathf.Lerp(-270f, 90f, normalizedTime);
            radialFillImage.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }
        
        // Update time text in HH:MM format starting at 06:00
        if (timeText != null)
        {
            // Convert normalized time (0-1) to 24 hours starting at 06:00
            float totalMinutes = normalizedTime * 24f * 60f; // Total minutes in the day cycle
            float currentHour = 6f + (totalMinutes / 60f); // Start at 06:00
            
            // Wrap around after 24 hours
            if (currentHour >= 24f)
            {
                currentHour -= 24f;
            }
            
            int hours = Mathf.FloorToInt(currentHour);
            int minutes = Mathf.FloorToInt((currentHour - hours) * 60f);
            
            timeText.text = string.Format("{0:D2}:{1:D2}", hours, minutes);
        }
    }
}