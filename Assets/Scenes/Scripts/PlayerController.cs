using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private IKFootSolver ikFootSolver;

    [Header("Core transforms")]
    [SerializeField] private Transform hipTransform;
    [SerializeField] private Transform headTransform;
    [SerializeField] private bool onlyMoveWhenBothGrounded = false;

    [Header("Forward Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float throttleSpeed = 5f;

    [Header("Height Control Settings")]
    [SerializeField] private float desiredBodyHeight = 2.5f;
    [SerializeField] private float maxHeight = 0.1f;
    [SerializeField] private float minHeight = -0.2f;
    [SerializeField] private float heightInputSpeed = 5f;

    [Header("Head Movement")]
    [SerializeField] private float desiredRotation = 0f;
    [Range(0, 180)] [SerializeField] private float maxYawAngle = 90f;
    [SerializeField] private float headRotationSmoothTime = 0.1f;
    [SerializeField] private float headRotationSpeed = 5f;
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;
	
	private float leftThrottle;
    private float rightThrottle;
    private Quaternion headRotation;
    private float currentHeadYaw;
	
    void Start()
    {
        headRotation = headTransform.localRotation;
    }

    void Update()
    {
		HandleMovementInput();
        HandleHeightInput();
        HandleCockpitRotation();
		
		ikFootSolver.DesiredBodyHeight = this.desiredBodyHeight;
        ikFootSolver.LeftThrottle = this.leftThrottle;
        ikFootSolver.RightThrottle = this.rightThrottle;
        ikFootSolver.MoveSpeed = this.moveSpeed;
		
		ApplyMovement();
    }

	void HandleHeightInput()
    {
        if (Input.GetKey(KeyCode.R)) { desiredBodyHeight += Time.deltaTime * heightInputSpeed; }
        else if (Input.GetKey(KeyCode.F)) { desiredBodyHeight -= Time.deltaTime * heightInputSpeed; }
        desiredBodyHeight = Mathf.Clamp(desiredBodyHeight, minHeight, maxHeight);
    }

    void HandleMovementInput()
    {
        float leftTarget = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        float rightTarget = Input.GetKey(KeyCode.I) ? 1f : Input.GetKey(KeyCode.K) ? -1f : 0f;

        leftThrottle = Mathf.Lerp(leftThrottle, leftTarget, Time.deltaTime * throttleSpeed);
        rightThrottle = Mathf.Lerp(rightThrottle, rightTarget, Time.deltaTime * throttleSpeed);
    }
	
    void HandleCockpitRotation()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            desiredRotation -= Time.deltaTime * headRotationSpeed;
        }

        if (Input.GetKey(KeyCode.E))
        {
            desiredRotation += Time.deltaTime * headRotationSpeed;
        }

        desiredRotation = Mathf.Clamp(desiredRotation, -maxYawAngle, maxYawAngle);

        currentHeadYaw = Mathf.Lerp(currentHeadYaw, desiredRotation, headRotationSmoothTime * Time.deltaTime);

        if (rotationAxis == RotationAxis.X) headRotation = Quaternion.Euler(currentHeadYaw, 0f, 0f);
        if (rotationAxis == RotationAxis.Y) headRotation = Quaternion.Euler(0f, currentHeadYaw, 0f);
        if (rotationAxis == RotationAxis.Z) headRotation = Quaternion.Euler(0f, 0f, currentHeadYaw);

        headTransform.localRotation = headRotation;
    }
	
    void ApplyMovement()
    {
        bool isMovingForward = leftThrottle > 0 || rightThrottle > 0;

        float forwardMovement = isMovingForward ? (leftThrottle + rightThrottle) / 2f : (leftThrottle + rightThrottle) / 3f;
        float turnMovement = (rightThrottle - leftThrottle);

        bool canMove = !onlyMoveWhenBothGrounded || ikFootSolver.CanMove;
        if(canMove)
        {
            transform.Translate((moveSpeed / 2) * forwardMovement * Time.deltaTime * Vector3.forward);
            transform.Rotate(Time.deltaTime * turnMovement * turnSpeed * Vector3.up);
        }
    }
}

#region Enums
	public enum RotationAxis
    {
        X, Y, Z
    }
#endregion