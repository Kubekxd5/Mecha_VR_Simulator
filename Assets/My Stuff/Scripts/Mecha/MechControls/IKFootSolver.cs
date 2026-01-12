using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class IKFootSolver : MonoBehaviour
{
    [Header("Core transforms")]
    [SerializeField] private Transform hipTransform;
	[SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Leg[] legs;

    [Header("Leg IK Settings")]
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private bool useCurrentVelocityForLegs, resetAngleOnPause;

	[Header("Automated Height Adjustment")]
    [SerializeField] private float initialBodyHeight = 0f;
    [SerializeField] private float heightAdjustSpeed = 5f;
    [SerializeField] private float feetHeightFixAdjustement = 0f;
    [SerializeField] private float safetyThreshold = 1.2f;
    [SerializeField] private float safetyRaiseHeight = 2.0f;

    [Header("Gravity Settings")]
    [SerializeField] private bool useGravity = true;
    [SerializeField] private float gravityStrength = 9.81f;
    [SerializeField] private float maxFallSpeed = 25f;
    [SerializeField] private TrailRenderer[] fallTrails;
    [SerializeField] private float fallTrailDelay = 0.3f;

    [Header("Body Side-to-side")]
    //[SerializeField] [Range(0f,2f)] private float bodyTiltIntensity  = 1.5f;
    [SerializeField] private float maxTiltAngle = 5f;
    [SerializeField] private float tiltSmoothSpeed = 10f;
	
	[Header("Body Bob")]
	[SerializeField] private Transform bodyPivot;
	[SerializeField] private float bodyBobAmount = 0.1f;
	[SerializeField] private float bodyBobSpeed = 10f;

	public float DesiredBodyHeight { get; set; }
    public float LeftThrottle { get; set; }
    public float RightThrottle { get; set; }
    public float MoveSpeed { get; set; }
    public float CurrentForwardMovement { get; private set; }
    public bool CanMove { get; private set; } = true;

    private Vector3 torsoPosition;
    private Quaternion targetBodyRotation = Quaternion.identity;
    private Quaternion currentBodyRotation = Quaternion.identity;
    private Vector3 targetBodyOffset, currentBodyOffset, bobVelocity, bodyVelocity;
    private float legSpeed;
    private float verticalVelocity = 0f;
    private float airTime = 0f;
    private bool isGrounded;

    void Start()
    {
		torsoPosition = hipTransform.position;
		DesiredBodyHeight = initialBodyHeight;
		
        foreach (var leg in legs)
        {
            if (leg.ikTarget != null)
            {
                leg.currentPosition = leg.ikTarget.position;
                leg.newPosition = leg.ikTarget.position;
                leg.oldPosition = leg.ikTarget.position;
            }
            leg.lerp = 1;
            leg.defaultAngle = leg.angle;
        }
    }

    void Update()
    {
		torsoPosition = hipTransform.position;

        bool allLegsGrounded = true;
        foreach (var leg in legs)
        {
            if (leg.IsMoving())
            {
                allLegsGrounded = false;
                break;
            }
        }
        CanMove = allLegsGrounded;

        ProcessMovementInputs();
		AutomatedHeightAdjustment();
        UpdateLegsIK();
        ApplyBodyTilt();
        ApplyBodyBob();
    }
	
	private void ProcessMovementInputs()
    {
        foreach (var leg in legs)
        {
            float baseAngle = Mathf.Abs(leg.defaultAngle);
            float currentInput = 0f;

            switch (leg.side)
            {
                case Leg.LegSide.Left:
                    currentInput = LeftThrottle;
                    break;
                case Leg.LegSide.Right:
                    currentInput = RightThrottle;
                    break;
                case Leg.LegSide.Center:
                    currentInput = (LeftThrottle + RightThrottle) / 2;
                    break;
            }

            if (Mathf.Abs(currentInput) > 0.01f)
            {
                leg.angle = baseAngle * Mathf.Sign(currentInput) * -1f;
            }
            else if (resetAngleOnPause) leg.angle = 0;
        }

        bool isMovingForward = LeftThrottle > 0 || RightThrottle > 0;
        float forwardMovement = isMovingForward ? (LeftThrottle + RightThrottle) / 2f : (LeftThrottle + RightThrottle) / 3f;
        float turnMovement = (RightThrottle - LeftThrottle);

        CurrentForwardMovement = forwardMovement;

        float currentVelocity = Mathf.Abs(forwardMovement) + Mathf.Abs(turnMovement);
        legSpeed = useCurrentVelocityForLegs ? MoveSpeed * 1.75f * currentVelocity : MoveSpeed * 1.75f;
    }

    private void AutomatedHeightAdjustment()
    {
        torsoPosition = hipTransform.position;
        Vector3 targetPosition = transform.position;
        Vector3 averageFootPosition = Vector3.zero;
        
        Debug.DrawRay(torsoPosition, raycastDistance * Vector3.down, Color.yellow);

        if (Physics.Raycast(torsoPosition, Vector3.down, out RaycastHit hit, raycastDistance * 2, groundMask))
        {
            isGrounded = true;
            airTime = 0;

            float effectiveHeight = this.DesiredBodyHeight;

            if (transform.position.y - averageFootPosition.y < safetyThreshold)
            {
                effectiveHeight = safetyRaiseHeight;
            }

            foreach (var leg in legs) { averageFootPosition += leg.newPosition; }
            averageFootPosition /= legs.Length + feetHeightFixAdjustement;

            if (useGravity)
            {
                verticalVelocity = Mathf.Min(0f, verticalVelocity);
            }

            targetPosition.y = hit.point.y + effectiveHeight;

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref bodyVelocity, 1 / heightAdjustSpeed);
        }
        else if (useGravity)
        {
            isGrounded = false;
            airTime += Time.deltaTime;

            verticalVelocity -= gravityStrength * Time.deltaTime;
            verticalVelocity = Mathf.Clamp(verticalVelocity, -maxFallSpeed, maxFallSpeed);

            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
        }

        UpdateFallingTrails(isGrounded);
    }

    private void UpdateFallingTrails(bool isGrounded)
    {
        if (fallTrails == null) return;

        if (isGrounded)
        {
            airTime = 0f;
            foreach (var trail in fallTrails)
            {
                trail.emitting = false;
            }
        }
        else
        {
            foreach (var trail in fallTrails) 
            { 
                trail.emitting = airTime >= fallTrailDelay; 
            }
        }
    }

    public void UpdateLegsIK()
    {
        if (isGrounded == false && useGravity) return;

        torsoPosition = hipTransform.position;

        targetBodyRotation = Quaternion.identity;
        targetBodyOffset = Vector3.zero;
		
        for (int i = 0; i < legs.Length; i++)
        {
            Leg leg = legs[i];
            if (leg.ikTarget == null) continue;

            leg.ikTarget.position = leg.currentPosition;
			
			if(leg.IsMoving())
			{
				float bobCurve = Mathf.Sin(leg.lerp * Mathf.PI);
				targetBodyOffset.y = bobCurve * bodyBobAmount;

                float tiltDirection = 0;

                if (leg.side == Leg.LegSide.Left) tiltDirection = 1;
                else if (leg.side == Leg.LegSide.Right) tiltDirection = -1;

                if (tiltDirection != 0)
                {
                    float tiltStrength = Mathf.Sin(leg.lerp * Mathf.PI);
                    float currentTiltAngle = maxTiltAngle * tiltDirection * tiltStrength;

                    targetBodyRotation = Quaternion.Euler(0, 0, currentTiltAngle);
                }
            }
			
			Vector3 rayOrigin = torsoPosition + transform.TransformDirection(leg.footSpacing);
            Vector3 rayDirection = Quaternion.AngleAxis(leg.angle, transform.right) * -transform.up;
			
			// Update leg Raycast for each leg
			leg.raycast.origin = rayOrigin;
			leg.raycast.direction = rayDirection;
		
			Debug.DrawRay(leg.raycast.origin, leg.raycast.direction * raycastDistance, Color.cyan);

            if (Physics.Raycast(leg.raycast, out RaycastHit hitInfo, raycastDistance, groundMask))
            {
                bool allOtherLegsStable = true;

                if(leg.otherFootIndex != null && leg.otherFootIndex.Length > 0)
                {
                    foreach (int otherLeg in leg.otherFootIndex)
                    {
                        if (otherLeg < 0 || otherLeg >= legs.Length) continue;
                        if (legs[otherLeg].IsMoving())
                        {
                            allOtherLegsStable = false;
                            break;
                        }
                    }
                }

                // Check is other leg moving
                if (allOtherLegsStable && Vector3.Distance(leg.newPosition, hitInfo.point) > leg.stepDistance)
                {
                    leg.lerp = 0;
                    leg.newPosition = hitInfo.point + leg.targetOffset;
                }
            }
            else
            {
                Vector3 airbornePosition = rayOrigin - transform.up * DesiredBodyHeight;
                if (Vector3.Distance(leg.newPosition, airbornePosition) > 0.1f)
                {
                    leg.lerp = 0;
                    leg.newPosition = airbornePosition;
                    leg.stepLeanRotation = Quaternion.identity;
                }
            }

            // Arcing motion
            if (leg.lerp < 1)
            {
                Vector3 footPos = Vector3.Lerp(leg.oldPosition, leg.newPosition, leg.lerp);
                footPos.y += Mathf.Sin(leg.lerp * Mathf.PI) * leg.stepHeight;

                leg.currentPosition = footPos;
                leg.lerp += Time.deltaTime * legSpeed;
            }
            else
            {
                leg.oldPosition = leg.newPosition;
            }
        }
    }
	
	void ApplyBodyBob()
	{
		if (bodyPivot == null) return;
		
		currentBodyOffset = Vector3.SmoothDamp(currentBodyOffset,targetBodyOffset, ref bobVelocity, 1/bodyBobSpeed);
		
		bodyPivot.localPosition = currentBodyOffset;
	}
	
	void ApplyBodyTilt()
	{
		if (bodyTransform == null) return;

        currentBodyRotation = Quaternion.Slerp(currentBodyRotation, targetBodyRotation, Time.deltaTime * tiltSmoothSpeed);

        bodyTransform.localRotation = currentBodyRotation;
    }
	
    private void OnDrawGizmos()
    {
        if (legs == null) return;

        foreach (var leg in legs)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(leg.newPosition, 0.1f);
        }
    }
}

#region Leg Class

[System.Serializable]
public class Leg
{
    public enum LegSide { Left, Right, Center }
    public LegSide side;
   
    [HideInInspector] public Ray raycast;
	
    public Transform ikTarget;
    public int[] otherFootIndex;

    [Header("Footstep Settings")]
    public Vector3 footSpacing;
    public float stepDistance;
    public float stepHeight;
    public float angle;

    public Vector3 targetOffset;

    [HideInInspector] public Vector3 currentPosition;
    [HideInInspector] public Vector3 newPosition;
    [HideInInspector] public Vector3 oldPosition;
    [HideInInspector] public Quaternion stepLeanRotation;
    [HideInInspector] public float lerp;
    [HideInInspector] public float defaultAngle;

    public bool IsMoving()
    {
        return lerp < 1;
    }
}

#endregion