using System.Collections;
using UnityEngine;

public class TerrainObject : MonoBehaviour, IDamageableEntity
{
    [Header("Classification")]
    [SerializeField] private MissionTargetCategory myCategory = MissionTargetCategory.Nature;

    [Header("Terrain Object Settings")]
    public float objectRadius = 2f;

    [Header("Health Settings")]
    [SerializeField] private float health = 30f;

    [Header("Object Destruction")]
    public bool canBeDestroyed = true;
    public float fallSpeed = 1.5f;
    public float lifetimeAfterFall = 5f;
    public float minImpactForce = 1f;

    [Header("Effects")]
    [Tooltip("Plays immediately when health reaches 0 or object is struck.")]
    [SerializeField] private GameObject breakEffect;
    [SerializeField] private float breakEffectLifetime = 3f;

    [Tooltip("Optional: Plays when the object finishes falling and hits the ground.")]
    [SerializeField] private GameObject landingEffect;
    [SerializeField] private float landingEffectLifetime = 3f;

    [Header("Pivot Settings")]
    [SerializeField] private Transform pivotTransform;

    private bool isFalling = false;
    private Collider objectCollider;

    void Awake()
    {
        objectCollider = GetComponent<Collider>();
    }

    public MissionTargetCategory GetCategory() => myCategory;

    public void TakeDamage(float amount)
    {
        if (isFalling || !canBeDestroyed) return;
        health -= amount;
        if (health <= 0)
        {
            Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            FallOver(randomDir);
        }
    }

    public void FallOver(Vector3 impactDirection)
    {
        if (isFalling) return;
        isFalling = true;

        if (breakEffect != null)
        {
            GameObject effect = Instantiate(breakEffect, transform.position, transform.rotation);
            Destroy(effect, breakEffectLifetime);
        }

        if (MissionManager.Instance != null)
            MissionManager.Instance.ReportProgress(MissionType.Elimination, myCategory, 1);

        if (myCategory == MissionTargetCategory.Nature)
            StartCoroutine(DoFallAnimation(impactDirection));
        else
            Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isFalling || myCategory != MissionTargetCategory.Nature) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("PlayerShip") || collision.gameObject.layer == LayerMask.NameToLayer("Mech_Body"))
        {
            if (collision.relativeVelocity.magnitude >= minImpactForce)
            {
                Vector3 impactDir = transform.position - collision.transform.position;
                FallOver(impactDir);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFalling || !canBeDestroyed || myCategory != MissionTargetCategory.Nature) return;

        bool isPlayer = other.CompareTag("Player") || other.CompareTag("PlayerShip");
        bool isMechPart = other.gameObject.layer == LayerMask.NameToLayer("Mech_Body");

        if (isPlayer || isMechPart)
        {
            Vector3 impactDir = transform.position - other.transform.position;
            FallOver(impactDir);
        }
    }

    private IEnumerator DoFallAnimation(Vector3 impactDirection)
    {
        if (objectCollider != null) objectCollider.enabled = false;

        Vector3 pivotPos = GetPivotPosition();

        Vector3 fallDirection = impactDirection;
        fallDirection.y = 0;
        fallDirection.Normalize();
        if (fallDirection == Vector3.zero) fallDirection = transform.forward;

        Vector3 rotationAxis = Vector3.Cross(Vector3.up, fallDirection);

        float totalRotation = 90f;
        float currentRotation = 0f;

        while (currentRotation < totalRotation)
        {
            float step = Time.deltaTime * fallSpeed * 60f;
            if (currentRotation + step > totalRotation) step = totalRotation - currentRotation;

            transform.RotateAround(pivotPos, rotationAxis, step);
            currentRotation += step;
            yield return null;
        }

        if (landingEffect != null)
        {
            GameObject effect = Instantiate(landingEffect, transform.position, Quaternion.identity);
            Destroy(effect, landingEffectLifetime);
        }

        StartCoroutine(DestroyAfterDelay());
    }

    private Vector3 GetPivotPosition()
    {
        if (pivotTransform != null) return pivotTransform.position;
        if (objectCollider != null) return new Vector3(objectCollider.bounds.center.x, objectCollider.bounds.min.y, objectCollider.bounds.center.z);
        return transform.position;
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(lifetimeAfterFall);
        Destroy(gameObject);
    }
}