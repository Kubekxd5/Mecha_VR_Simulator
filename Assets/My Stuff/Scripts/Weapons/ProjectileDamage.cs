using UnityEngine;
using System.Collections.Generic;

public class ProjectileController : MonoBehaviour
{
    private WeaponSO weaponData;
    private ParticleSystem _particleSystem;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    [Header("Collision Settings")]
    [SerializeField] private LayerMask targetLayers = -1;

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
        if (((1 << collision.gameObject.layer) & targetLayers) != 0)
        {
            HandleCollision(collision.gameObject, collision.contacts[0].point);
            Destroy(gameObject);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (weaponData == null) return;

        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            Vector3 hitPos = other.ClosestPoint(transform.position);

            HandleCollision(other.gameObject, hitPos);
            Destroy(gameObject);
        }
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
            IDamageableEntity entity = hitObject.GetComponentInParent<IDamageableEntity>();
            if (entity != null) entity.TakeDamage(weaponData.damage);
        }

        if (weaponData.ImpactVFX != null)
        {
            Instantiate(weaponData.ImpactVFX, hitPosition, Quaternion.LookRotation(Vector3.up));
        }
    }
}