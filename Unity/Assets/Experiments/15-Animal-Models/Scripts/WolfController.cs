using UnityEngine;

public class WolfController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float runSpeed = 3f;
    public float runDelay = 3f;
    public float runDuration = 3f;

    private bool isRunning = false;
    private float runTimer = 0f;
    private float delayTimer = 0f;
    private bool isWaiting = true;

    void Update()
    {
        // Handle running behavior
        if (isWaiting)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= runDelay)
            {
                StartRunning(transform);
                isWaiting = false;
                delayTimer = 0f;
            }
        }

        if (isRunning)
        {
            // Move in the transform's forward direction
            transform.position += transform.forward * runSpeed * Time.deltaTime;

            runTimer += Time.deltaTime;
            if (runTimer >= runDuration)
            {
                StopRunning();
            }
        }
    }

    void StartRunning(Transform player)
    {
        isRunning = true;
        runTimer = 0f;
    }

    void StopRunning()
    {
        isRunning = false;
        isWaiting = true;
        delayTimer = 0f;
    }
}