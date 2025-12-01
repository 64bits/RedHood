using UnityEngine;

[RequireComponent(typeof(Highlightable))]
public class HighlightToggle : MonoBehaviour
{
    private Highlightable highlightComponent;

    private void Awake()
    {
        // Get the Highlight component on this GameObject
        highlightComponent = GetComponent<Highlightable>();
    }

    private void OnEnable()
    {
        // Subscribe to the special state changed event
        SpecialController.OnSpecialStateChanged += OnSpecialStateChanged;

        // Initialize the highlight state based on current special state
        if (SpecialController.Instance != null)
        {
            UpdateHighlight(SpecialController.Instance.IsSpecialEnabled);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the event
        SpecialController.OnSpecialStateChanged -= OnSpecialStateChanged;
    }

    private void OnSpecialStateChanged(bool isSpecialEnabled)
    {
        UpdateHighlight(isSpecialEnabled);
    }

    private void UpdateHighlight(bool enabled)
    {
        if (highlightComponent != null)
        {
            highlightComponent.enabled = enabled;
        }
    }
}