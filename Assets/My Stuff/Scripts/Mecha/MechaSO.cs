using JetBrains.Annotations;
using UnityEngine;

[CreateAssetMenu(fileName = "New Mecha", menuName = "Game/New Mecha", order = 1)]
public class MechaSO : ScriptableObject
{
    [Header("Mecha Info")]
    public string mechName;
    public string mechDescription;
    public Sprite mechIcon;
    public MechaType mechaType;
    public GameObject mechPrefab;

    [Header("Mecha Structs")]
    public MechaStats mechaStats;
    public MechaCockpit mechaCockpit;
    public MechaLoadout mechaLoadout;
    public MechaVisuals mechaVisuals;
    public MechaAudio mechaAudio;
}

#region structs
[System.Serializable]
public struct MechaStats
{
    public float health;
    public float walkSpeed;
    public float turnSpeed;
    public float throttleSpeed;
}

[System.Serializable]
public struct MechaCockpit
{
    public GameObject cockpitPrefab;
    public Vector3 cockpitOffset;
}

[System.Serializable]
public struct MechaLoadout
{
    public GameObject[] defaultWeapon;
}

[System.Serializable]
public struct MechaVisuals
{
    public Material[] mechaSkins;
}

[System.Serializable]
public struct MechaAudio
{
    public AudioClip spawnSound;
    public AudioClip deathSound;
    public AudioClip damageSound;
    public AudioClip turnSound;
    public AudioClip walkSound;
    public AudioClip[] stompSound;
    public AudioClip idleSound;
    public AudioClip heightAdjustSound;
}
#endregion

#region enums

public enum MechaType
{
    Light,
    Medium,
    Heavy
}

#endregion