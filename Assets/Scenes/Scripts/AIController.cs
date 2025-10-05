using UnityEngine;

[RequireComponent(typeof(IKFootSolver))]
public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float stoppingDistance = 5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float bodyHeight = 0f;

    [Header("AI Logic Settings")]
    [SerializeField] private float throttleSpeed = 5f;
    [SerializeField] private float turnAngleThreshold = 30f;
    [SerializeField] private float moveAngleThreshold = 5f;

    [Header("AI Head Rotation")]
    [SerializeField] private Transform headTransform;
    [Range(0, 180)][SerializeField] private float maxYawAngle = 90f;
    [SerializeField] private float headRotationSmoothTime = 0f;
    [SerializeField] private float headRotationSpeed = 0f;
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;

    private IKFootSolver footSolver;

    private Vector3 directionToPlayer;
    private float leftThrottle;
    private float rightThrottle;
    private float currentHeadYawVelocity;
    private float currentHeadYaw;

    void Start()
    {
        footSolver = GetComponent<IKFootSolver>();

        if(playerTarget == null)
        {
            Debug.LogError("Player is null");
            enabled = false;
        }
    }

    void Update()
    {
        if (playerTarget == null || footSolver == null) return;

        MoveTowardsPlayer();
    }

    private void MoveTowardsPlayer()
    {
        Vector3 heading = playerTarget.position - transform.position;
        float distanceToPlayer = heading.magnitude;
        directionToPlayer = heading.normalized;

        float targetLeftThrottle = 0f;
        float targetRightThrottle = 0f;

        if (distanceToPlayer > stoppingDistance)
        {
            Vector3 forward = transform.forward;
            Vector3 directionOnPlane = Vector3.ProjectOnPlane(directionToPlayer, Vector3.up);
            float angleToPlayer = Vector3.SignedAngle(forward, directionOnPlane, Vector3.up);

            if (Mathf.Abs(angleToPlayer) > turnAngleThreshold)
            {
                targetLeftThrottle = -Mathf.Sign(angleToPlayer);
                targetRightThrottle = Mathf.Sign(angleToPlayer);
            }
            else
            {
                targetLeftThrottle = 1f;
                targetRightThrottle = 1f;

                if (Mathf.Abs(angleToPlayer) > moveAngleThreshold)
                {
                    float turnStrength = Mathf.Clamp01(Mathf.Abs(angleToPlayer) / turnAngleThreshold);

                    if (angleToPlayer > 0)
                    {
                        targetLeftThrottle = 1f;
                        targetRightThrottle = 1f - turnStrength;
                    }
                    else
                    {
                        targetLeftThrottle = 1f - turnStrength;
                        targetRightThrottle = 1f;
                    }
                }
            }
        }

        leftThrottle = Mathf.Lerp(leftThrottle, targetLeftThrottle, Time.deltaTime * throttleSpeed);
        rightThrottle = Mathf.Lerp(rightThrottle, targetRightThrottle, Time.deltaTime * throttleSpeed);

        ApplyMovement();

        footSolver.LeftThrottle = leftThrottle;
        footSolver.RightThrottle = rightThrottle;
        footSolver.MoveSpeed = moveSpeed;
        footSolver.DesiredBodyHeight = 0;
    }

    private void ApplyMovement()
    {
        bool isMovingForward = leftThrottle > 0 || rightThrottle > 0;

        float forwardMovement = isMovingForward ? (leftThrottle + rightThrottle) / 2f : (leftThrottle + rightThrottle) / 3f;
        float turnMovement = (rightThrottle - leftThrottle);

        transform.Translate(moveSpeed * forwardMovement * Time.deltaTime * Vector3.forward);
        transform.Rotate(Time.deltaTime * turnMovement * turnSpeed * Vector3.up);
    }

    private void LateUpdate()
    {
        FacePlayer();
    }

    private void FacePlayer()
    {
        if (headTransform == null || playerTarget == null)
        {
            return;
        }

        Vector3 headLookDirection = playerTarget.position - headTransform.position;
        Vector3 targetDirectionOnPlane = Vector3.ProjectOnPlane(headLookDirection, transform.up);
        headLookDirection.y = 0;

        float targetHeadYaw = Vector3.SignedAngle(transform.forward, targetDirectionOnPlane, transform.up);

        targetHeadYaw = Mathf.Clamp(targetHeadYaw, -maxYawAngle, maxYawAngle);

        currentHeadYaw = Mathf.SmoothDampAngle(currentHeadYaw, targetHeadYaw, ref currentHeadYawVelocity, headRotationSmoothTime);


        switch (rotationAxis)
        {
            case RotationAxis.X:
                headTransform.localRotation = Quaternion.Euler(currentHeadYaw, 0f, 0f);
                break;
            case RotationAxis.Y:
                headTransform.localRotation = Quaternion.Euler(0f, currentHeadYaw, 0f);
                break;
            case RotationAxis.Z:
                headTransform.localRotation = Quaternion.Euler(0f, 0f, currentHeadYaw);
                break;
        }
    }
}
