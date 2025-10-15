using UnityEngine;

[RequireComponent(typeof(IKFootSolver))]
public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private Transform castingCenter;
    [SerializeField] private float enemyFieldOfView = 80f;
    [SerializeField] private float stoppingFieldOfView = 110f;
    [SerializeField] private float stoppingDistance = 5f;
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float bodyHeight = 0f;
    [SerializeField] private bool onlyMoveWhenBothGrounded = false;

    [Header("AI Logic Settings")]
    [SerializeField] private float throttleSpeed = 5f;
    [SerializeField] private float turnAngleThreshold = 30f;
    [SerializeField] private float moveAngleThreshold = 5f;

    [Header("AI Head Rotation")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private HeadRotationSettings headRotation;

    private IKFootSolver footSolver;

    private Vector3 directionToPlayer;
    private Transform playerTarget;

    private float leftThrottle;
    private float rightThrottle;

    private float avoidanceDirection = 0f;
    private float avoidanceTimer = 0f;

    private bool playerDetected;
    private Collider[] detectedColliders = new Collider[20];
    private Collider[] detectedPlayers = new Collider[2];

    void Start()
    {
        footSolver = GetComponent<IKFootSolver>();
        playerDetected = false;
    }

    void Update()
    {
        if (footSolver == null) return;

        LookForPlayer();

        if (playerTarget == null) return;

        FacePlayer();
        MoveTowardsPlayer();
    }

    private void LookForPlayer()
    {
        bool foundPlayer = false;

        int hitCount = Physics.OverlapSphereNonAlloc(castingCenter.position, detectionRadius, detectedPlayers, playerMask);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = detectedPlayers[i];
            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(castingCenter.forward, directionToTarget);

            if (col.CompareTag("Player") && angleToTarget < enemyFieldOfView / 2)
            {
                if (!Physics.Raycast(castingCenter.position, directionToTarget, Vector3.Distance(castingCenter.position, col.transform.position), obstacleMask))
                {
                    playerDetected = true;
                    playerTarget = col.transform;
                    foundPlayer = true;

                    break;
                }
            }
        }

        if (!foundPlayer)
        {
            playerDetected = false;
            playerTarget = null;
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector3 heading = playerTarget.position - transform.position;
        float distanceToPlayer = heading.magnitude;
        directionToPlayer = heading.normalized;

        float targetLeftThrottle = 0f;
        float targetRightThrottle = 0f;

        bool isAvoiding = false;

        int hitCount = Physics.OverlapSphereNonAlloc(castingCenter.position, stoppingDistance, detectedColliders, enemyMask);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = detectedColliders[i];

            if (col.transform == this.transform)
            {
                continue;
            }

            Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(castingCenter.forward, directionToTarget);

            if (col.CompareTag("Enemy") && angleToTarget < stoppingFieldOfView / 2)
            {
                if (!Physics.Raycast(castingCenter.position, directionToTarget, Vector3.Distance(castingCenter.position, col.transform.position), obstacleMask))
                {
                    if (avoidanceTimer <= 0f)
                    {
                        avoidanceDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
                        avoidanceTimer = Random.Range(2f, 5f);
                    }

                    targetLeftThrottle = avoidanceDirection;
                    targetRightThrottle = -avoidanceDirection;

                    isAvoiding = true;
                    break;
                }
            }
        }

        if (avoidanceTimer > 0f)
            avoidanceTimer -= Time.deltaTime;

        if (!isAvoiding && distanceToPlayer > stoppingDistance)
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

        bool canMove = !onlyMoveWhenBothGrounded || footSolver.CanMove;
        if (canMove)
        {
            transform.Translate(moveSpeed * forwardMovement * Time.deltaTime * Vector3.forward);
            transform.Rotate(Time.deltaTime * turnMovement * turnSpeed * Vector3.up);
        }
    }

    private void FacePlayer()
    {
        if (headTransform == null || playerTarget == null)
        {
            return;
        }

        Vector3 worldDirectionToTarget = playerTarget.position - headTransform.position;
        Vector3 directionOnBodyPlane = Vector3.ProjectOnPlane(worldDirectionToTarget, transform.up);

        float yawAngle = Vector3.SignedAngle(transform.forward, directionOnBodyPlane, transform.up);
        float pitchAngle = Vector3.SignedAngle(directionOnBodyPlane, worldDirectionToTarget, transform.right);
        float rollAngle = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);

        float finalYaw = headRotation.rotateOnY ? Mathf.Clamp(yawAngle, -headRotation.leftLimit, headRotation.rightLimit) : 0f;
        float finalPitch = headRotation.rotateOnX ? Mathf.Clamp(pitchAngle, -headRotation.upLimit, headRotation.downLimit) : 0f;
        float finalRoll = headRotation.rotateOnZ ? Mathf.Clamp(rollAngle, -headRotation.zLeftLimit, headRotation.zRightLimit) : 0f;



        Quaternion finalLocalRotation = Quaternion.Euler(finalPitch, finalYaw, finalRoll);

        headTransform.localRotation = Quaternion.Slerp(
        headTransform.localRotation,
        finalLocalRotation,
        Time.deltaTime * headRotation.rotationSpeed
    );
    }

    private void OnDrawGizmos()
    {
        Vector3 leftBoundaryS = Quaternion.Euler(0, -stoppingFieldOfView / 2, 0) * castingCenter.forward;
        Vector3 rightBoundaryS = Quaternion.Euler(0, stoppingFieldOfView / 2, 0) * castingCenter.forward;
        Vector3 leftBoundary = Quaternion.Euler(0, -enemyFieldOfView / 2, 0) * castingCenter.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, enemyFieldOfView / 2, 0) * castingCenter.forward;

        // STOPPING DISTANCE
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(castingCenter.position, stoppingDistance);
        Gizmos.DrawLine(castingCenter.position, castingCenter.position + leftBoundaryS * stoppingDistance);
        Gizmos.DrawLine(castingCenter.position, castingCenter.position + rightBoundaryS * stoppingDistance);

        // DETECTION RADIUS
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(castingCenter.position, detectionRadius);
        Gizmos.DrawLine(castingCenter.position, castingCenter.position + leftBoundary * detectionRadius);
        Gizmos.DrawLine(castingCenter.position, castingCenter.position + rightBoundary * detectionRadius);

        // DIRECTION TO PLAYER

        if (playerTarget == null) return;

        Gizmos.color = playerDetected ? Color.green : Color.yellow;

        Gizmos.DrawLine(castingCenter.position, playerTarget.position);

    }
}

#region struct
[System.Serializable]
public struct HeadRotationSettings
{
    public bool rotateOnX;
    public bool rotateOnY;
    public bool rotateOnZ;


    [Header("Rotation Limits")]
    [Range(0, 180)] public float upLimit;
    [Range(0, 180)] public float downLimit;
    [Range(0, 180)] public float leftLimit;
    [Range(0, 180)] public float rightLimit;
    [Range(0, 180)] public float zLeftLimit;
    [Range(0, 180)] public float zRightLimit;

    [Header("Rotation Speed")]
    public float rotationSpeed;
}
#endregion