using UnityEngine;

public class MechaWeaponHardpoint : MonoBehaviour
{
    [Header("Hardpoint Info")]
    public WeaponType hardpointType;
    public Vector3 localOffset;

    public Transform MountPoint => transform;
}