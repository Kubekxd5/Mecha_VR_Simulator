using UnityEngine;

[RequireComponent(typeof(VRPhysicalButton))]
public class MechaButtonLinker : MonoBehaviour
{
    private enum ButtonFunction { HeightUp, HeightDown, None }

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

    public void TriggerMissionFailure()
    {
        if (MissionManager.Instance != null)
        {
            Debug.Log("Cockpit: Emergency failure sequence initiated via physical button.");
            MissionManager.Instance.ReportPlayerDeath();
        }
        else
        {
            Debug.LogWarning("Cockpit: Tried to trigger failure, but MissionManager is not present in this scene.");
        }
    }
}