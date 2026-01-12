using UnityEngine;
using System.Collections.Generic;

public class ProjectileController : MonoBehaviour
{
    private WeaponSO weaponData;
    private ParticleSystem _particleSystem;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    public void Initialize(WeaponSO data)
    {
        weaponData = data;
        _particleSystem = GetComponent<ParticleSystem>();

        if (_particleSystem != null)
        {
            var mainModule = _particleSystem.main;

            float calculatedLifetime = weaponData.projectileSpeed > 0
                ? weaponData.range / weaponData.projectileSpeed
                : 2f;

            mainModule.startLifetime = calculatedLifetime;
            mainModule.startSpeed = weaponData.projectileSpeed;

            var colModule = _particleSystem.collision;
            colModule.enabled = true;
            colModule.sendCollisionMessages = true;
        }
        else
        {
            float calculatedLifetime = weaponData.projectileSpeed > 0
                ? weaponData.range / weaponData.projectileSpeed
                : 2f;

            Destroy(gameObject, calculatedLifetime);
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (weaponData == null || _particleSystem == null) return;

        int numCollisionEvents = _particleSystem.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            HandleCollision(other, collisionEvents[i].intersection);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (weaponData == null) return;

        HandleCollision(collision.gameObject, collision.contacts[0].point);
        Destroy(gameObject);
    }

    private void HandleCollision(GameObject hitObject, Vector3 hitPosition)
    {
        Damageable damageable = hitObject.GetComponent<Damageable>();

        if (damageable != null)
        {
            damageable.ReceiveDamage(weaponData.damage);

            Debug.Log($"Hit {hitObject.name} for {weaponData.damage} damage.");
        }
        else
        {
            // Look for raw interface if no hitbox script exists
            IDamageableEntity entity = hitObject.GetComponentInParent<IDamageableEntity>();
            if (entity != null) entity.TakeDamage(weaponData.damage);
        }

        if (weaponData.ImpactVFX != null)
        {
            Instantiate(weaponData.ImpactVFX, hitPosition, Quaternion.LookRotation(Vector3.up));
        }
    }
}