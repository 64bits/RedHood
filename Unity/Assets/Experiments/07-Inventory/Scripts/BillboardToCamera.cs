using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    private Camera mainCam;
    
    private void Start()
    {
        mainCam = Camera.main;
    }
    
    private void LateUpdate()
    {
        if (mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }
    }
}