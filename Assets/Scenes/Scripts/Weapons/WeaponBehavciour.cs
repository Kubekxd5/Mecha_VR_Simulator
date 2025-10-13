using UnityEngine;

public class WeaponBehavciour : MonoBehaviour
{
    [SerializeField] private WeaponSO weaponData;
    [SerializeField] private Transform firePoint;

    private float fireCooldown;

    void Update()
    {
        if (fireCooldown > 0)
            fireCooldown -= Time.deltaTime;
    }

    public void Fire()
    {
        if (fireCooldown > 0) return;
        fireCooldown = 1f / weaponData.fireRate;

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

        if (weaponData.MuzzleFlashVFX)
            Instantiate(weaponData.MuzzleFlashVFX, firePoint.position, firePoint.rotation, firePoint);

        if (weaponData.FireSound == null) return;

        int index = Random.Range(0, weaponData.FireSound.Length);

        AudioSource.PlayClipAtPoint(weaponData.FireSound[index], firePoint.position);
    }

    private void FireProjectile()
    {
        GameObject proj = Instantiate(weaponData.ProjectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb)
            rb.linearVelocity = firePoint.forward * weaponData.projectileSpeed;
    }

    private void FireParticle()
    {
        GameObject pfx = Instantiate(weaponData.ProjectilePrefab, firePoint.position, firePoint.rotation);
        var ps = pfx.GetComponent<ParticleSystem>();
        if (ps) ps.Play();
    }

    private void FireLaser()
    {
        GameObject beam = Instantiate(weaponData.ProjectilePrefab, firePoint.position, firePoint.rotation);
        var lr = beam.GetComponent<LineRenderer>();
        if (lr)
        {
            lr.SetPosition(0, firePoint.position);
            lr.SetPosition(1, firePoint.position + firePoint.forward * weaponData.range);
        }
    }
}
