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
    public Leg[] Legs { get { return legs; } }

    [Header("Leg IK Settings")]
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask groundMask;

    [Header("Forward Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float throttleSpeed = 5f;

    [Header("Height Movement")]
    [SerializeField] private float desiredBodyHeight = 2.5f;
    [SerializeField] private float heightAdjustSpeed = 5f;
    [Space]
    [SerializeField] private float feetHeightFixAdjustement = 0f;
    [SerializeField] private float safetyThreshold = 1.2f;
    [SerializeField] private float safetyRaiseHeight = 2.0f;
    public float DesiredBodyHeight { get; set; }
	public float CurrentForwardMovement {  get; set; }
    public float LegSpeed { get; set; }

    [Header("Body Side-to-side")]
	//[SerializeField] [Range(0f,2f)] private float bodyTiltIntensity  = 1.5f;
	[SerializeField] private AnimationCurve tiltAngleToIntensity = AnimationCurve.EaseInOut(0f,0f,45f,1f);
    [SerializeField] private AnimationCurve stepTiltCurve = AnimationCurve.Linear(0, 0, 1, 0);
    [SerializeField] private float tiltSmoothSpeed = 10f;
	
	[Header("Body Bob")]
	[SerializeField] private Transform bodyPivot;
	[SerializeField] private float bodyBobAmount = 0.1f;
	[SerializeField] private float bodyBobSpeed = 10f;

    private enum RotationAxis
    {
        X, Y, Z
    }

    private Vector3 torsoPosition;

    private Quaternion targetBodyRotation = Quaternion.identity;
    private Quaternion currentBodyRotation = Quaternion.identity;

    private Vector3 targetBodyOffset;
    private Vector3 currentBodyOffset;
    private Vector3 bobVelocity, bodyVelocity;


    void Start()
    {
		torsoPosition = hipTransform.position;
				
        foreach (var leg in legs)
        {
            if (leg.ikTarget != null)
            {
                leg.currentPosition = leg.ikTarget.position;
                leg.newPosition = leg.ikTarget.position;
                leg.oldPosition = leg.ikTarget.position;
            }
            leg.lerp = 1;
        }
    }

    void Update()
    {
        AutomatedHeightAdjustment();
        UpdateLegsIK();
		ApplyBodyTilt();
		ApplyBodyBob();
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
                leg.lerp += Time.deltaTime * LegSpeed;
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

    public bool IsMoving()
    {
        return lerp < 1;
    }
}

#endregion