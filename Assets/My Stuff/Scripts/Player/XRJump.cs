using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class XRJump : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionProperty jumpAction;

    [Header("Jump Settings")]
    [SerializeField] private float jumpPower = 1.5f;        
    [SerializeField] private float gravity = -9.81f;        
    [SerializeField] private int maxJumps = 3;
    [SerializeField] private float hangTime = 1.0f;         

    private CharacterController controller;
    private Vector3 velocity;

    private int jumpsRemaining;
    private float hangTimer;
    private bool isHanging;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        jumpsRemaining = maxJumps;
    }

    private void OnEnable()
    {
        jumpAction.action.Enable();
        jumpAction.action.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        jumpAction.action.performed -= OnJumpPerformed;
        jumpAction.action.Disable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (jumpsRemaining > 0)
        {
            PerformJump();
        }
    }

    private void Update()
    {
        bool grounded = controller.isGrounded;

        // Reset state when touching ground
        if (grounded && velocity.y <= 0f)
        {
            velocity.y = -2f;
            jumpsRemaining = maxJumps;
            isHanging = false;
            hangTimer = 0f;
        }

        if (isHanging)
        {
            hangTimer -= Time.deltaTime;
            velocity.y = 0f; 

            if (hangTimer <= 0f)
            {
                isHanging = false;
            }
        }
        else if (!grounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void PerformJump()
    {
        jumpsRemaining--;

        controller.Move(Vector3.up * jumpPower);

        velocity.y = 0f; 
        isHanging = true;
        hangTimer = hangTime;

        Debug.Log($"Jump Triggered! {jumpsRemaining} left.");
    }
}