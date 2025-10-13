using UnityEngine;

public class MechaRuntimeData : MonoBehaviour
{
    [Header("Mecha Data")]
    [SerializeField] private MechaSO mechaData;

    [Header("Runtime Stats")]
    private float currentHealth;

    public MechaSO MechaData => mechaData;
    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        if (mechaData == null)
        {
            Debug.LogError("Critical error: MechaSO is unavailable" + gameObject.name);
            return;
        }
        currentHealth = mechaData.mechaStats.health;
    }

    public void Initialize(MechaSO data)
    {
        this.mechaData = data;
        currentHealth = data.mechaStats.health;
        gameObject.name = $"Mecha_{data.mechName}";
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        Debug.Log($"{mechaData.mechName} Took {damageAmount} damage. Remaining health: {currentHealth}");

        if (mechaData.mechaAudio.damageSound != null)
        {
            AudioSource.PlayClipAtPoint(mechaData.mechaAudio.damageSound, transform.position);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        currentHealth = 0;
        Debug.Log($"{mechaData.mechName} zosta³ zniszczony!");

        if (mechaData.mechaAudio.deathSound != null)
        {
            AudioSource.PlayClipAtPoint(mechaData.mechaAudio.deathSound, transform.position);
        }

        Destroy(gameObject, 2f);
    }
}