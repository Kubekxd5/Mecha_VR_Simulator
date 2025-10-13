using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SphereCollider))]
public class VRHandController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference grabAction;
    [SerializeField] private InputActionReference useAction;
    [SerializeField] private InputActionReference joystickAction;
    [SerializeField] private InputActionReference buttonAAction;
    [SerializeField] private InputActionReference buttonBAction;

    [Header("Interaction Settings")]
    [SerializeField] private float detectionRadius = 0.1f;

    // Publiczne w³aœciwoœci do odczytu przez inne skrypty
    public Vector2 JoystickValue { get; private set; }
    public bool IsButtonAPressed { get; private set; }
    public bool IsButtonBPressed { get; private set; }

    private VRInteractable heldObject = null;
    private List<VRInteractable> nearbyInteractables = new List<VRInteractable>();
    private VRInteractable closestInteractable;

    private void OnEnable()
    {
        grabAction.action.Enable();
        useAction.action.Enable();
        joystickAction.action.Enable();
        buttonAAction.action.Enable();
        buttonBAction.action.Enable();
    }

    private void OnDisable()
    {
        grabAction.action.Disable();
        useAction.action.Disable();
        joystickAction.action.Disable();
        buttonAAction.action.Disable();
        buttonBAction.action.Disable();
    }

    private void Awake()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = detectionRadius;

    }

    void Update()
    {
        ReadInputValues();

        if (heldObject == null)
        {
            DetectNearbyObject();

            if (grabAction.action.WasPressedThisFrame())
            {
                PickupNearbyObject();
            }
        }
        else
        {
            if (grabAction.action.WasReleasedThisFrame())
            {
                DropHeldObject();
            }
            if (useAction.action.WasPressedThisFrame())
            {
                heldObject.OnPickupUseDown(this);
            }
            if (useAction.action.WasReleasedThisFrame())
            {
                heldObject.OnPickupUseUp(this);
            }
        }
    }
    private void ReadInputValues()
    {
        JoystickValue = joystickAction.action.ReadValue<Vector2>();

        IsButtonAPressed = buttonAAction.action.IsPressed();
        IsButtonBPressed = buttonBAction.action.IsPressed();
    }

    void OnTriggerEnter(Collider other)
    {
        VRInteractable interactable = other.GetComponent<VRInteractable>();
        if (interactable != null && !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        VRInteractable interactable = other.GetComponent<VRInteractable>();
        if(interactable != null && nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Remove(interactable);
        }
    }

    private void DetectNearbyObject()
    {
        nearbyInteractables.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);

        foreach (var i in nearbyInteractables) i.EnableGhost(false);

        closestInteractable = nearbyInteractables
            .Where(i => i.isGrabbable && !i.isHeld)
            .OrderBy(i => Vector3.Distance(transform.position, i.transform.position))
            .FirstOrDefault();


        if (closestInteractable != null)
        {
            closestInteractable.EnableGhost(true);
        }
    }
    private void PickupNearbyObject()
    {
        if (closestInteractable == null) return;

        heldObject = closestInteractable;
        heldObject.OnPickUp(this);

        closestInteractable.EnableGhost(false);
        closestInteractable = null;
    }

    private void DropHeldObject()
    {
        if(heldObject != null)
        {
            heldObject.OnDrop(this);
            heldObject = null;
        }
    }
}
