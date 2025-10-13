using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VRInteractable : MonoBehaviour
{
    [Header("Interaction Settings")]
    public bool isGrabbable;
    public bool isHeld;

    public GameObject ghostObject;

    public VRHandController holdingHand;
    public Transform originalParent;
    public VRHandController HoldingHand => holdingHand;

    public virtual void OnPickUp(VRHandController hand)
    {
        isHeld = true;
        holdingHand = hand;
        originalParent = transform.parent;
        transform.SetParent(hand.transform, true);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    public virtual void OnDrop(VRHandController hand)
    {
        if (holdingHand == hand)
        {
            isHeld = false;
            holdingHand = null;

            transform.SetParent(originalParent, true);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
    }

    public virtual void OnPickupUseDown(VRHandController hand)
    {

    }

    public virtual void OnPickupUseUp(VRHandController hand)
    {

    }

    public void EnableGhost(bool enable)
    {
        if (ghostObject != null)
        {
            ghostObject.SetActive(enable);
        }
    }
}
