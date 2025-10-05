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
    [SerializeField] private float maxHeight = 3.5f;
    [SerializeField] private float minHeight = 1.5f;
    [SerializeField] private float heightInputSpeed = 5f;

    [Header("Head Movement")]
    [SerializeField] private float desiredRotation = 0f;
    [Range(0, 180)]
    [SerializeField] private float maxYawAngle = 90f;
    [SerializeField] private float headRotationSmoothTime = 0.1f;
    [SerializeField] private float headRotationSpeed = 5f;
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;
    private enum RotationAxis { X, Y, Z }

    private float leftThrottle;
    private float rightThrottle;
    private Vector3 bodyVelocity, torsoPosition;
    private float _currentForwardMovement;
    private Quaternion headRotation;
    private float currentHeadYaw;
    private float _currentMoveSpeed;
    private Leg[] legs;

    void Start()
    {
        torsoPosition = hipTransform.position;
        headRotation = headTransform.localRotation;

        if (ikFootSolver != null)
        {
            this.legs = ikFootSolver.Legs;
        }
    }

    void Update()
    {
        HandleMovementInput();
        HandleHeightInput();
        HandleCockpitRotation();
        ikFootSolver.DesiredBodyHeight = this.desiredBodyHeight;
        ikFootSolver.CurrentForwardMovement = this._currentForwardMovement;
        ikFootSolver.LegSpeed = this._currentMoveSpeed;
        ikFootSolver.UpdateLegsIK();
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

    void HandleHeightInput()
    {
        if (Input.GetKey(KeyCode.R))
        {
            desiredBodyHeight += Time.deltaTime * (heightInputSpeed / 2f);
        }
        else if (Input.GetKey(KeyCode.F))
        {
            desiredBodyHeight -= Time.deltaTime * (heightInputSpeed / 2f);
        }

        desiredBodyHeight = Mathf.Clamp(desiredBodyHeight, minHeight, maxHeight);
    }

    void HandleMovementInput()
    {
        bool canMove = true;
        if (onlyMoveWhenBothGrounded)
        {
            foreach (var leg in legs)
            {
                if (leg.IsMoving())
                {
                    canMove = false;
                    break;
                }
            }
        }

        float leftTarget = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        float rightTarget = Input.GetKey(KeyCode.I) ? 1f : Input.GetKey(KeyCode.K) ? -1f : 0f;

        if (leftTarget == 0 && rightTarget == 0) return;

        torsoPosition = hipTransform.position;

        /*if (rightTarget != 0)
        {
            angle = Mathf.Abs(angle) * rightTarget * -1f;
        }
        else if (leftTarget != 0)
        {
            angle = Mathf.Abs(angle) * leftTarget * -1f;
        }*/

        foreach (var leg in legs)
        {
            float baseAngle = Mathf.Abs(leg.angle);
            float currentInput = 0f;

            switch (leg.side)
            {
                case Leg.LegSide.Left:
                    currentInput = leftTarget;
                    break;
                case Leg.LegSide.Right:
                    currentInput = rightTarget;
                    break;
                case Leg.LegSide.Center:
                    currentInput = (leftTarget + rightTarget) / 2;
                    break;
            }

            if (currentInput != 0)
            {
                leg.angle = baseAngle * currentInput * -1f;
            }
        }

        leftThrottle = Mathf.Lerp(leftThrottle, leftTarget, Time.deltaTime * throttleSpeed);
        rightThrottle = Mathf.Lerp(rightThrottle, rightTarget, Time.deltaTime * throttleSpeed);

        bool isMovingForward = leftThrottle > 0 || rightThrottle > 0;

        float forwardMovement = isMovingForward ? (leftThrottle + rightThrottle) / 2f : (leftThrottle + rightThrottle) / 3f;
        float turnMovement = (rightThrottle - leftThrottle);

        _currentForwardMovement = forwardMovement;

        // Adaptive leg speed movement
        float currentVelocity = Mathf.Abs(forwardMovement) + Mathf.Abs(turnMovement);
        _currentMoveSpeed = moveSpeed * 1.75f * currentVelocity;

        if (canMove)
        {
            transform.Translate((moveSpeed / 2) * forwardMovement * Time.deltaTime * Vector3.forward);
            transform.Rotate(Time.deltaTime * turnMovement * turnSpeed * Vector3.up);
        }
    }
}
