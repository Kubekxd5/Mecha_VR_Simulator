using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Game/New Weapon", order = 2)]
public class WeaponSO : ScriptableObject
{
    [Header("General Info")]
    [SerializeField] private string weaponName;
    [SerializeField] private Sprite weaponIcon;
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private WeaponClass weaponClass;
    [SerializeField] private GameObject weaponPrefab;

    [Header("Stats")]
    public float damage = 25f;
    public float fireRate = 0.2f;
    public float range = 100f;
    public float projectileSpeed = 60f;
    public float energyCost = 5f;
    public float overheatThreshold = 10f;
    public float cooldownTime = 2f;

    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private ProjectileType projectileType;

    [Header("FX / Audio")]
    [SerializeField] private AudioClip[] fireSounds;
    [SerializeField] private GameObject muzzleFlashVFX;
    [SerializeField] private GameObject impactVFX;

    public string WeaponName => weaponName;
    public GameObject ProjectilePrefab => projectilePrefab;
    public GameObject MuzzleFlashVFX => muzzleFlashVFX;
    public GameObject ImpactVFX => impactVFX;
    public AudioClip[] FireSound => fireSounds.Length > 0 ? fireSounds : null;
    public ProjectileType ProjectileType => projectileType;
}

#region enums
public enum WeaponType
{
    Ballistic,
    Energy,
    Explosive,
    Kinetic
}

public enum WeaponClass
{
    Light,
    Medium,
    Heavy,
}
public enum ProjectileType
{
    RigidBody,
    Particle,
    LineRenderer
}
#endregion