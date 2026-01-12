using UnityEngine;
using UnityEngine.Events;

public class VRPhysicalButton : MonoBehaviour
{
    [Header("Rigging")]
    [SerializeField] private Transform pushTransform;
    [SerializeField] private Collider touchCollider;

    [Header("Button Settings")]
    [SerializeField] private KeyCode keyToSimulate = KeyCode.Space;
    [SerializeField] private bool holdMode = false;

    [Header("Physics Settings")]
    [SerializeField] private float maxDepression = 0.05f;
    [SerializeField] private float returnSpeed = 10f;
    [Range(0f, 1f)]
    [SerializeField] private float triggerThreshold = 0.75f;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color pressedColor = Color.green;

    [Header("Interaction Config")]
    [SerializeField] private string handTag = "PlayerHand";

    [Header("Events")]
    public UnityEvent OnButtonDown;
    public UnityEvent OnButtonUp;
    public UnityEvent OnButtonHeld;

    // State
    private bool isPressed = false;
    private Transform hoveringHand = null;
    private Vector3 initialLocalPos;
    private Material buttonMat;

    private void Start()
    {
        if (pushTransform == null) pushTransform = transform;
        initialLocalPos = pushTransform.localPosition;

        if (buttonRenderer != null)
        {
            buttonMat = buttonRenderer.material;
            SetColor(normalColor);
        }

        if (touchCollider != null) touchCollider.isTrigger = true;
    }

    private void Update()
    {
        // 1. Calculate physical movement
        if (hoveringHand != null)
        {
            FollowHand(hoveringHand);
        }
        else
        {
            ReturnToCenter();
        }

        // 2. Check Logic based on position
        CheckPressState();

        // 3. Continuous Fire Logic
        if (isPressed && holdMode)
        {
            OnButtonHeld?.Invoke();
        }
    }

    private void FollowHand(Transform hand)
    {
        Vector3 localHandPos = transform.InverseTransformPoint(hand.position);

        float targetY = Mathf.Clamp(localHandPos.y, -maxDepression, 0f);

        Vector3 newPos = initialLocalPos;
        newPos.y = targetY;
        pushTransform.localPosition = newPos;
    }

    private void ReturnToCenter()
    {
        pushTransform.localPosition = Vector3.Lerp(pushTransform.localPosition, initialLocalPos, Time.deltaTime * returnSpeed);
    }

    private void CheckPressState()
    {
        float currentDist = Mathf.Abs(pushTransform.localPosition.y - initialLocalPos.y);
        float percentage = Mathf.Clamp01(currentDist / maxDepression);

        if (!isPressed && percentage >= triggerThreshold)
        {
            Press();
        }
        else if (isPressed && percentage < triggerThreshold)
        {
            Release();
        }
    }

    private void Press()
    {
        if (isPressed) return; // Already pressed

        isPressed = true;
        SetColor(pressedColor);

        OnButtonDown?.Invoke(); // Fire once
    }

    private void Release()
    {
        if (!isPressed) return; // Already released

        isPressed = false;
        SetColor(normalColor);

        if (holdMode)
        {
            OnButtonUp?.Invoke(); // Fire once
        }
    }

    private void SetColor(Color c)
    {
        if (buttonMat != null) buttonMat.color = c;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsHand(other))
        {
            hoveringHand = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsHand(other))
        {
            if (other.transform == hoveringHand)
            {
                hoveringHand = null;
            }
        }
    }
    private bool IsHand(Collider col)
    {
        if (!string.IsNullOrEmpty(handTag) && col.CompareTag(handTag)) return true;

        //if (col.GetComponent<VRHandController>()) return true;

        return false;
    }
}