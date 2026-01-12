using UnityEngine;
using UnityEngine.InputSystem;

public class VRJoystickController : VRInteractable
{
    [Header("Joystick Rigging")]
    [SerializeField] private Transform handleBone;
    [SerializeField] private Transform grabProxy;
    [SerializeField] private Transform pivotBone;

    [Header("Input Settings")]
    [SerializeField] private InputActionProperty triggerAction;

    [Header("Joystick Settings")]
    [SerializeField] private float maxTiltAngle = 45f;
    [SerializeField] private float impactStrength = 1f;
    [SerializeField] private float returnSpeed = 10f;

    public bool isLeft = false;
    public Vector2 Output { get; private set; }
    public bool IsTriggerPressed { get; private set; }

    private Quaternion initialPivotLocalRotation;

    private void Start()
    {
        initialPivotLocalRotation = pivotBone.localRotation;

        MechaControls controls = GetComponentInParent<MechaControls>();
        if (controls != null)
        {
            controls.RegisterJoystick(this);
        }
    }

    private void Update()
    {
        if (isHeld && HoldingHand != null)
        {
            FollowHand();
            ReadTriggerInput();

        }
        else
        {
            ReturnToCenterWithRotation();
            Output = Vector2.zero;
            IsTriggerPressed = false;
        }

        CalculateOutputFromRotation();
    }

    private void ReadTriggerInput()
    {
        float triggerValue = triggerAction.action != null ? triggerAction.action.ReadValue<float>() : 0f;

        IsTriggerPressed = triggerValue > 0.5f;
    }

    private void FollowHand()
    {
        Vector3 targetDirection = HoldingHand.transform.position - pivotBone.position;
        Vector3 localTargetDirection = pivotBone.parent.InverseTransformDirection(targetDirection);

        /*localTargetDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.up, localTargetDirection);
        // Was causing gimbal lock! (Quaternion targetRotation = Quaternion.LookRotation(localTargetDirection, Vector3.up);)*/

        Quaternion deltaRotation = Quaternion.FromToRotation(Vector3.up, localTargetDirection);

        Quaternion targetRotation = initialPivotLocalRotation * deltaRotation;

        pivotBone.localRotation = Quaternion.RotateTowards(pivotBone.localRotation, targetRotation, 360f);
        pivotBone.localRotation = Quaternion.RotateTowards(initialPivotLocalRotation, pivotBone.localRotation, maxTiltAngle);
    }

    /*private void ReturnToCenter()
    {
        handleBone.localPosition = Vector3.Lerp(handleBone.localPosition, initialHandleLocalPosition, Time.deltaTime * returnSpeed);
    }*/

    private void ReturnToCenterWithRotation()
    {
        pivotBone.localRotation = Quaternion.Slerp(pivotBone.localRotation, initialPivotLocalRotation, Time.deltaTime * returnSpeed);
    }

    private void CalculateOutputFromRotation()
    {
        float currentAngle = Quaternion.Angle(pivotBone.localRotation, initialPivotLocalRotation);

        float magnitude = Mathf.Clamp01(currentAngle / impactStrength);

        if (magnitude < 0.01f)
        {
            Output = Vector2.zero;
            return;
        }

        Vector3 localUp = pivotBone.transform.localRotation * Vector3.up;

        Output = new Vector2(localUp.x, localUp.z).normalized * magnitude;
    }

    public override void OnPickUp(VRHandController hand)
    {
        isHeld = true;
        holdingHand = hand;
    }

    public override void OnDrop(VRHandController hand)
    {
        isHeld = false;
        holdingHand = null;
    }
}
