using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(VRPhysicalButton))]
public class MechaButtonLinker : MonoBehaviour
{
    private enum ButtonFunction { HeightUp, HeightDown }

    [SerializeField] private ButtonFunction functionType;

    private void Start()
    {
        MechaControls controls = GetComponentInParent<MechaControls>();

        if (controls == null)
        {
            return;
        }

        VRPhysicalButton btn = GetComponent<VRPhysicalButton>();

        switch (functionType)
        {
            case ButtonFunction.HeightUp:
                btn.OnButtonHeld.AddListener(controls.MoveHeightUp);
                break;
            case ButtonFunction.HeightDown:
                btn.OnButtonHeld.AddListener(controls.MoveHeightDown);
                break;
        }
    }
}