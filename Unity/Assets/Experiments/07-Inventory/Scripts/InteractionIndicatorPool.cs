using UnityEngine;
using System.Collections.Generic;

public class InteractionIndicatorPool : MonoBehaviour
{
    public static InteractionIndicatorPool Instance { get; private set; }
    
    [Header("Prefab Setup")]
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private int poolSize = 10;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        InitializePool();
    }
    
    private void InitializePool()
    {
        if (indicatorPrefab == null)
        {
            Debug.LogError("No indicator prefab provided");
        }
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(indicatorPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    public GameObject GetIndicator()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        
        // Pool exhausted, create new instance
        GameObject obj = Instantiate(indicatorPrefab);
        return obj;
    }
    
    public void ReturnIndicator(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pool.Enqueue(obj);
    }
}