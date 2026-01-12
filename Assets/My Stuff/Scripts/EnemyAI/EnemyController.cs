using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageableEntity
{
    [Header("Classification")]
    [SerializeField] private MissionTargetCategory myCategory = MissionTargetCategory.EnemyUnit;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Death Settings")]
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private GameObject lootDropPrefab;
    [SerializeField] private float destroyDelay = 0.5f;

    private AIController aiMovement;
    private SpawnerModule mySpawner;
    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;

        aiMovement = GetComponent<AIController>();
        mySpawner = GetComponent<SpawnerModule>();
    }

    public MissionTargetCategory GetCategory()
    {
        return myCategory;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        isDead = true;

        if (aiMovement != null)
        {
            aiMovement.enabled = false;
        }

        if (mySpawner != null)
        {
            mySpawner.DisableSpawning();
        }

        if (MissionManager.Instance != null)
        {
            // Reports progress to any active Elimination missions
            MissionManager.Instance.ReportProgress(MissionType.Elimination, myCategory, 1);
        }

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, transform.rotation);
        }

        if (lootDropPrefab != null)
        {
            Instantiate(lootDropPrefab, transform.position + Vector3.up, Quaternion.identity);
        }

        Destroy(gameObject, destroyDelay);
    }
}