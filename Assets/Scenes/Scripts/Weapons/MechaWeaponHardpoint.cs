using UnityEngine;

public class MechaWeaponHardpoint : MonoBehaviour
{
    [Header("Hardpoint Info")]
    public HardpointType hardpointType;
    public Vector3 localOffset;

    public Transform MountPoint => transform;
}

#region enums
public enum HardpointType
{
    Light,
    Medium,
    Heavy
}

#endregion