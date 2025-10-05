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

    [Header("Body Side-to-side")]
	//[SerializeField] [Range(0f,2f)] private float bodyTiltIntensity  = 1.5f;
	[SerializeField] private AnimationCurve tiltAngleToIntensity = AnimationCurve.EaseInOut(0f,0f,45f,1f);
    [SerializeField] private AnimationCurve stepTiltCurve = AnimationCurve.Linear(0, 0, 1, 0);
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
        float effectiveHeight = this.DesiredBodyHeight;

        Vector3 averageFootPosition = Vector3.zero;
        foreach (var leg in legs) { averageFootPosition += leg.newPosition; }
        averageFootPosition /= legs.Length + feetHeightFixAdjustement;

        if (transform.position.y - averageFootPosition.y < safetyThreshold)
        {
            effectiveHeight = safetyRaiseHeight;
        }

        Vector3 targetPosition = transform.position;
        Vector3 torsoPosition = hipTransform.position;
        Debug.DrawRay(torsoPosition, 2 * raycastDistance * Vector3.down, Color.yellow);

        if (Physics.Raycast(torsoPosition, Vector3.down, out RaycastHit hit, raycastDistance * 2))
        {
            targetPosition.y = hit.point.y + effectiveHeight;
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref bodyVelocity, 1 / heightAdjustSpeed);
    }

    public void UpdateLegsIK()
    {
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

                if (leg.stepLeanRotation != Quaternion.identity)
                {
                    float curveValue = stepTiltCurve.Evaluate(leg.lerp);

                    targetBodyRotation = Quaternion.Slerp(Quaternion.identity, leg.stepLeanRotation, curveValue);
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
                Leg otherLeg = legs[leg.otherFootIndex];
				
                // Check is other leg moving
                if (otherLeg.IsMoving() == false && Vector3.Distance(leg.newPosition, hitInfo.point) > leg.stepDistance)
                {
                    leg.lerp = 0;
                    leg.newPosition = hitInfo.point + leg.targetOffset;

                    /* if (Mathf.Abs(leg.footSpacing) < centerLegThreshold) targetBodyTiltEuler = new Vector3(forwardTiltPower,0,0);
					else 
					{
						float tiltDirection = -Mathf.Sign(leg.footSpacing);
						targetBodyTiltEuler = new Vector3(0, 0, tiltDirection * sideTiltPower);
					}*/

                    if (Mathf.Abs(CurrentForwardMovement) > 0.1f)
                    {
                        Vector3 leanVectorWorld = (bodyTransform.position - otherLeg.newPosition).normalized;
                        Vector3 leanVectorLocal = transform.InverseTransformDirection(leanVectorWorld);

                        Quaternion leanRotation = Quaternion.LookRotation(Vector3.forward, leanVectorLocal);

                        float leanAngle = Vector3.Angle(Vector3.up, leanVectorWorld);
                        float currentIntensity = tiltAngleToIntensity.Evaluate(leanAngle);

                        targetBodyRotation = Quaternion.Slerp(Quaternion.identity, leanRotation, currentIntensity);
                        leg.stepLeanRotation = targetBodyRotation;
                    }
                    else
                    {
                        leg.stepLeanRotation = Quaternion.identity;
                    }
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
		
		//currentBodyTiltEuler = Vector3.Lerp(currentBodyTiltEuler, targetBodyTiltEuler, tiltSmoothSpeed * Time.deltaTime);
		currentBodyRotation = Quaternion.Slerp(currentBodyRotation, targetBodyRotation, Time.deltaTime * (tiltSmoothSpeed/2));
		
		bodyTransform.rotation = transform.rotation * currentBodyRotation;
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
    public int otherFootIndex;

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