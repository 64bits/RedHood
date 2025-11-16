using UnityEngine;

public class BrambleSlowdown : MonoBehaviour
{
    [SerializeField] private float slowedSpeed = 0.75f;
    private float originalSpeed = 1f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering has the "Player" tag
        if (other.CompareTag("Player"))
        {
            Animator playerAnimator = other.GetComponent<Animator>();
            
            if (playerAnimator != null)
            {
                // Store the original speed and apply the slowed speed
                originalSpeed = playerAnimator.speed;
                playerAnimator.speed = slowedSpeed;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the object exiting has the "Player" tag
        if (other.CompareTag("Player"))
        {
            Animator playerAnimator = other.GetComponent<Animator>();
            
            if (playerAnimator != null)
            {
                // Restore the original animator speed
                playerAnimator.speed = originalSpeed;
            }
        }
    }
}