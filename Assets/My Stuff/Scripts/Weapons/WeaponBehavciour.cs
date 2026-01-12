using UnityEngine;

public class WeaponBehaviour : MonoBehaviour
{
    [Header("Runtime Info")]
    [SerializeField] private WeaponSO weaponData;
    [SerializeField] private bool isLeft;

    [Header("References")]
    [SerializeField] private Transform firePoint;

    private float fireCooldown;
    public WeaponSO WeaponData => weaponData;
    public bool IsLeft => isLeft;

    public void Initialize(WeaponSO data, bool isLeftSide)
    {
        weaponData = data;
        isLeft = isLeftSide;
        Debug.Log($"Weapon {data.WeaponName} initialized on {(isLeft ? "Left" : "Right")} side.");
    }

    void Update()
    {
        if (fireCooldown > 0)
            fireCooldown -= Time.deltaTime;
    }

    public void Fire()
    {
        if (weaponData == null) return;
        if (fireCooldown > 0) return;

        fireCooldown = 1f / weaponData.fireRate;

        // Firing Logic
        switch (weaponData.ProjectileType)
        {
            case ProjectileType.RigidBody:
                FireProjectile();
                break;
            case ProjectileType.Particle:
                FireParticle();
                break;
            case ProjectileType.LineRenderer:
                FireLaser();
                break;
        }

        // VFX
        if (weaponData.MuzzleFlashVFX)
            Instantiate(weaponData.MuzzleFlashVFX, firePoint.position, firePoint.rotation, firePoint);

        // Audio
        if (weaponData.FireSound != null)
        {
            int index = Random.Range(0, weaponData.FireSound.Length);
            AudioSource.PlayClipAtPoint(weaponData.FireSound[index], firePoint.position);
        }
    }

    private void FireProjectile()
    {
        if (!weaponData.ProjectilePrefab) return;
        GameObject proj = Instantiate(weaponData.ProjectilePrefab, firePoint.position, firePoint.rotation);

        var controller = proj.GetComponent<ProjectileController>();
        if (controller != null) controller.Initialize(weaponData);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = firePoint.forward * weaponData.projectileSpeed;
    }

    private void FireParticle()
    {
        if (!weaponData.ProjectilePrefab) return;
        GameObject pfx = Instantiate(weaponData.ProjectilePrefab, firePoint.position, firePoint.rotation);

        var controller = pfx.GetComponent<ProjectileController>();
        if (controller != null) controller.Initialize(weaponData);

        var ps = pfx.GetComponent<ParticleSystem>();
        if (ps) ps.Play();
    }

    private void FireLaser()
    {
        if (!weaponData.ProjectilePrefab) return;
        GameObject beam = Instantiate(weaponData.ProjectilePrefab, firePoint.position, firePoint.rotation);
        var lr = beam.GetComponent<LineRenderer>();
        if (lr)
        {
            lr.SetPosition(0, firePoint.position);
            lr.SetPosition(1, firePoint.position + firePoint.forward * weaponData.range);
        }
    }
}