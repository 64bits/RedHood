using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour
{
    [Header("Indicator Settings")]
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private bool useColliderTop = true;
    
    [Header("Events")]
    public UnityEvent onIndicatorShow;
    public UnityEvent onIndicatorHide;
    public UnityEvent onInteract;
    
    private GameObject indicatorInstance;
    private bool isIndicatorActive;
    private Collider objectCollider;
    
    private void Awake()
    {
        objectCollider = GetComponent<Collider>();
    }
    
    public void ShowIndicator()
    {
        if (isIndicatorActive) return;
        
        indicatorInstance = InteractionIndicatorPool.Instance.GetIndicator();
        indicatorInstance.transform.SetParent(transform);
        indicatorInstance.transform.position = GetIndicatorPosition();
        indicatorInstance.SetActive(true);
        
        isIndicatorActive = true;
        onIndicatorShow?.Invoke();
    }
    
    private Vector3 GetIndicatorPosition()
    {
        if (useColliderTop && objectCollider != null)
        {
            Bounds bounds = objectCollider.bounds;
            Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            return topCenter + indicatorOffset;
        }
        
        return transform.position + indicatorOffset;
    }
    
    public void HideIndicator()
    {
        if (!isIndicatorActive) return;
        
        if (indicatorInstance != null)
        {
            InteractionIndicatorPool.Instance.ReturnIndicator(indicatorInstance);
            indicatorInstance = null;
        }
        
        isIndicatorActive = false;
        onIndicatorHide?.Invoke();
    }
    
    public void Interact()
    {
        onInteract?.Invoke();
        Debug.Log($"Interacted with {gameObject.name}");
    }
    
    private void OnDisable()
    {
        HideIndicator();
    }
    
    // Update indicator position if object moves
    private void LateUpdate()
    {
        if (isIndicatorActive && indicatorInstance != null && useColliderTop)
        {
            indicatorInstance.transform.position = GetIndicatorPosition();
        }
    }
}