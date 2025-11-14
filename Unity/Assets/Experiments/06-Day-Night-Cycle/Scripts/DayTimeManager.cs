using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DayTimeManager : MonoBehaviour
{
    [Header("Cycle Settings")]
    [SerializeField] private float cycleDuration = 30f; // Total seconds for full cycle
    [SerializeField] private float transitionDuration = 3f; // Dawn/Dusk duration
    [SerializeField] private bool useManualTimeControl = false;
    [SerializeField] [Range(0f, 2f)] private float manualTimeOfDay = 0f;
    
    [Header("Player Input")]
    [SerializeField] private PlayerInput playerInput;
    
    private float currentCycleTime = 0f;
    private bool isPlayerMoving = false;
    private Coroutine cycleCoroutine;
    
    // Shader keyword for darkness
    private const string DARKNESS_FEATURE = "ENABLE_DARKNESS";
    
    // Time periods (in seconds from start of cycle)
    public float DayStart => 0f;
    public float DuskStart => cycleDuration * 0.5f - transitionDuration;
    public float DuskEnd => cycleDuration * 0.5f;
    public float NightStart => DuskEnd;
    public float DawnStart => cycleDuration - transitionDuration;
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
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("PlayerInput component not found!");
            }
        }
    }
    
    private void OnEnable()
    {
        if (playerInput != null)
        {
            var moveAction = playerInput.actions["Move"];
            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMoveStop;
            }
            else
            {
                Debug.LogError("Move action not found in Input Actions!");
            }
        }
        
        cycleCoroutine = StartCoroutine(UpdateCycle());
        
        // Notify all listeners of initial state
        NotifyTimeUpdate();
    }
    
    private void OnDisable()
    {
        if (playerInput != null)
        {
            var moveAction = playerInput.actions["Move"];
            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMoveStop;
            }
        }
        
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
        }
    }
    
    private void Update()
    {
        // Manual time control overrides automatic cycle
        if (useManualTimeControl)
        {
            float normalizedManualTime = manualTimeOfDay % 1f;
            currentCycleTime = normalizedManualTime * cycleDuration;
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
    
    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        isPlayerMoving = moveInput.magnitude > 0.1f;
    }
    
    private void OnMoveStop(InputAction.CallbackContext context)
    {
        isPlayerMoving = false;
    }
    
    private IEnumerator UpdateCycle()
    {
        while (true)
        {
            if (isPlayerMoving)
            {
                currentCycleTime += Time.deltaTime;
                
                if (currentCycleTime >= cycleDuration)
                {
                    currentCycleTime -= cycleDuration;
                }
                
                NotifyTimeUpdate();
            }
            
            yield return null;
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