using UnityEngine;
using System.Collections.Generic;

public class Damageable : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("Drag the Root Object here (The one with EnemyController or TerrainObject)")]
    [SerializeField] private GameObject mainControllerObject;

    [Header("Settings")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float defaultParticleDamage = 10f;

    private IDamageableEntity targetEntity;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    private void Awake()
    {
        if (mainControllerObject != null)
        {
            targetEntity = mainControllerObject.GetComponent<IDamageableEntity>();
        }

        if (targetEntity == null)
        {
            targetEntity = GetComponent<IDamageableEntity>();
        }
    }

    public void ReceiveDamage(float baseDamage)
    {
        if (targetEntity != null)
        {
            targetEntity.TakeDamage(baseDamage * damageMultiplier);
        }
    }
}