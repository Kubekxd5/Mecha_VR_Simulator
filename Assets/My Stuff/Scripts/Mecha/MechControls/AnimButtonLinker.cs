using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(VRPhysicalButton))]
public class AnimButtonLinker : MonoBehaviour
{
    private enum Direction { Increase, Decrease }

    [Header("Configuration")]
    [SerializeField] private string parameterName;
    [SerializeField] private Direction direction;

    private void Start()
    {
        SimpleAnimationController animCtrl = GetComponentInParent<SimpleAnimationController>();

        if (animCtrl == null)
        {
            return;
        }

        VRPhysicalButton btn = GetComponent<VRPhysicalButton>();

        if (direction == Direction.Increase)
        {
            btn.OnButtonHeld.AddListener(() => animCtrl.IncreaseParameter(parameterName));
        }
        else
        {
            btn.OnButtonHeld.AddListener(() => animCtrl.DecreaseParameter(parameterName));
        }
        }
}