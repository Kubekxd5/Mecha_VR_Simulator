using Unity.VisualScripting;
using UnityEngine;

public class MechaControls : MonoBehaviour
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

    [Header("Collision Handling")]
    [SerializeField] private float collisionCheckDistance = 1.5f;
    [SerializeField] private float collisionRadius = 1f;
    [SerializeField] private LayerMask collisionMask;

    [Header("Component References")]
    [SerializeField] private VRJoystickController movementJoystick;
    [SerializeField] private VRJoystickController aimingJoystick;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private float leftThrottle;
    private float rightThrottle;
    private Quaternion headRotation;
    private float currentHeadYaw;
	private bool hasBeenInitialized = false;
    void Start()
    {
        headRotation = headTransform.localRotation;
    }

    void Update()
    {
        if (!debugMode && hasBeenInitialized)
        {
            Vector2 moveInput = movementJoystick.Output;
            float leftTarget = moveInput.y - moveInput.x;
            float rightTarget = moveInput.y + moveInput.x;
            leftThrottle = Mathf.Clamp(leftTarget, -1f, 1f);
            rightThrottle = Mathf.Clamp(rightTarget, -1f, 1f);

            Vector2 aimInput = aimingJoystick.Output;
            HandleCockpitRotation(aimInput.x);
        }
        else
        {
            HandleMovementInput();
            HandleCockpitRotation(0);
        }

        HandleHeightInput();

        ikFootSolver.DesiredBodyHeight = this.desiredBodyHeight;
        ikFootSolver.LeftThrottle = this.leftThrottle;
        ikFootSolver.RightThrottle = this.rightThrottle;
        ikFootSolver.MoveSpeed = this.moveSpeed;
		
		ApplyMovement();
    }

    public void InitializeControls()
    {
        foreach (var joystick in GetComponentsInChildren<VRJoystickController>(true))
        {
            if (joystick.isLeft)
                movementJoystick = joystick;
            else
                aimingJoystick = joystick;
        }

        if (movementJoystick != null && aimingJoystick != null)
        {
            hasBeenInitialized = true;
        }
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
	
    void HandleCockpitRotation(float horizontalInput)
    {
        if (debugMode)
        {
            if (Input.GetKey(KeyCode.Q))
            {
                desiredRotation -= Time.deltaTime * headRotationSpeed;
            }

            if (Input.GetKey(KeyCode.E))
            {
                desiredRotation += Time.deltaTime * headRotationSpeed;
            }
        }
        else desiredRotation += horizontalInput * Time.deltaTime * headRotationSpeed;

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
            CheckCollision(forwardMovement);

            transform.Translate((moveSpeed / 2) * forwardMovement * Time.deltaTime * Vector3.forward);
            transform.Rotate(Time.deltaTime * turnMovement * turnSpeed * Vector3.up);
        }
    }
    
    private void CheckCollision(float forwardMovement)
    {
        Vector3 movementVector = (moveSpeed/2) * forwardMovement * Time.deltaTime * Vector3.forward;

        if (forwardMovement <= 0.001f)
        {
            return;
        }

        Vector3 worldSpaceMovementDir = transform.TransformDirection(Vector3.forward);

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, collisionRadius, worldSpaceMovementDir,
            collisionCheckDistance, collisionMask);

        if (hits.Length > 0)
        {
            foreach (RaycastHit hit in hits)
            {
                TerrainObject terrainObject = hit.collider.gameObject.GetComponent<TerrainObject>();
                if (terrainObject != null)
                {
                    Vector3 impactDirection = worldSpaceMovementDir;
                    terrainObject.FallOver(impactDirection);
                }
            }
        }
    }
}

#region Enums
	public enum RotationAxis
    {
        X, Y, Z
    }
#endregion