using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainObject : MonoBehaviour
{
    [Header("Terrain Object Settings")]
    public float objectRadius = 2f;

    [Header("Object Destruction")]
    public bool canBeDestroyed = true;
    public float fallSpeed = 1.5f;
    public float fallCheckRayDistance = 1f;
    public float lifetimeAfterFall = 5f;

    private bool isFalling = false;
    private Vector3 targetPosition;
    private Collider objectCollider;

    void Awake()
    {
        objectCollider = GetComponent<Collider>();
    }

    public void FallOver(Vector3 impactDirection)
    {
        if(!canBeDestroyed || isFalling)
            return;

        isFalling = true;
        StartCoroutine(DoFallAnimation(impactDirection));
    }

    private IEnumerator DoFallAnimation(Vector3 impactDirection)
    {
        Vector3 fallDirection = impactDirection;
        fallDirection.y = 0;
        fallDirection.Normalize();

        Vector3 rotationAxis = Vector3.Cross(Vector3.up, fallDirection);

        Quaternion initialRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.AngleAxis(90f, rotationAxis) * initialRotation;

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * fallSpeed;
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, progress);
            yield return null;
        }

        transform.rotation = targetRotation;

        if (objectCollider != null)
        {
            objectCollider.enabled = false;
        }

        StartCoroutine(DestroyAfterDelay());

        /* RaycastHit hit;
         Vector3 rayStart = transform.position + Vector3.up * 2f;

         if (Physics.Raycast(rayStart, Vector3.down, out hit, fallCheckRayDistance))
         {
             transform.position = hit.point;
             Quaternion groundRotation = Quaternion.FromToRotation(transform.up, hit.normal);
             transform.rotation = groundRotation * transform.rotation;
         }*/
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(lifetimeAfterFall);
        Destroy(gameObject);
    }
}
