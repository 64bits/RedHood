using UnityEngine;

public class CharacterGravity : MonoBehaviour
{
    [Header("Gravity Settings")]
    public float gravity = -9.81f;
    public float groundedGravity = -0.5f; // Small downward force when grounded
    
    [Header("References")]
    private CharacterController characterController;
    private Vector3 velocity;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        ApplyGravity();
    }

    void ApplyGravity()
    {
        if (characterController.isGrounded && velocity.y < 0)
        {
            // When grounded, apply a small downward force
            velocity.y = groundedGravity;
        }
        else
        {
            // Apply gravity acceleration
            velocity.y += gravity * Time.deltaTime;
        }

        // Apply the vertical movement
        characterController.Move(velocity * Time.deltaTime);
    }

    // Call this when the character jumps
    public void Jump(float jumpHeight)
    {
        if (characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}