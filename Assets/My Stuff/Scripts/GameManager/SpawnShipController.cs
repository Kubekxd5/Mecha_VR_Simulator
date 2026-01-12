using System.Collections;
using UnityEngine;

public class SpawnShipController : MonoBehaviour
{
    [Header("Refrences")]
    public Transform playerHolder;
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnPoint;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float clibmStartDistance = 50f;
    [SerializeField] private float climbExponent = 2f;
    [SerializeField] private float climbMultiplier = 5f;
    [SerializeField] private float releaseHeight = 30f;
    [SerializeField] private float travelDelay = 3f;

    [Header("Rotation Settings")]
    [SerializeField] private float turnSpeed = 2f;
    [SerializeField] private float maxClimbPitch = 35f;

    [Header("Debug Info")]
    [SerializeField] private bool hasReleasedPlayer = false;
    [SerializeField] private bool isClimbing = false;

    private float climbProgress = 0f;

    [SerializeField] private bool isInitialized = false;
    private bool canMove = false;


    public void Initialize(Transform playerTransform, Transform targetSpawnPoint)
    {
        this.player = playerTransform;
        this.spawnPoint = targetSpawnPoint;

        if (player == null || spawnPoint == null || playerHolder == null)
        {
            Debug.LogError("SpawnShipController could not be initialized. A reference is missing!", this.gameObject);
            return;
        }

        player.SetParent(playerHolder);
        player.localPosition = Vector3.zero;
        player.localRotation = Quaternion.identity;

        if (player.TryGetComponent<IKFootSolver>(out var footSolver))
            footSolver.enabled = false;

        isInitialized = true;

        StartCoroutine(DelayedTravelStart());
    }

    private IEnumerator DelayedTravelStart()
    {
        yield return new WaitForSeconds(travelDelay);
        canMove = true;
    }
    void Update()
    {
        if (!isInitialized || !canMove || spawnPoint == null)
            return;

        MoveTowardsCenter();

        if (hasReleasedPlayer)
        {
            RotatePlayer();
        }
    }

    private void MoveTowardsCenter()
    {
        Vector3 directionToCenter = (spawnPoint.position - transform.position).normalized;
        Vector3 horizontalDirection = new Vector3(directionToCenter.x, 0f, directionToCenter.z);

        float distance = Vector3.Distance(transform.position, spawnPoint.position);

        if (!isClimbing)
        {
            transform.position += horizontalDirection * moveSpeed * Time.deltaTime;

            if (horizontalDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }

            if (distance <= clibmStartDistance)
            {
                isClimbing = true;
                climbProgress = 0f;
            }
        }
        else
        {
            climbProgress += Time.deltaTime;
            float climbSpeed = Mathf.Pow(climbProgress, climbExponent) * climbMultiplier;

            transform.position += horizontalDirection * moveSpeed * Time.deltaTime;
            transform.position += Vector3.up * climbSpeed * Time.deltaTime;

            float normalizedProgress = Mathf.Clamp01(climbProgress / 2f);
            float targetPitch = Mathf.Lerp(0f, maxClimbPitch, normalizedProgress);

            Quaternion climbRotation = Quaternion.Euler(-targetPitch, transform.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, climbRotation, Time.deltaTime * turnSpeed);

            if (!hasReleasedPlayer && transform.position.y >= releaseHeight)
            {
                ReleasePlayer();
            }
        }
    }

    private void ReleasePlayer()
    {
        if (player == null) return;

        if (player.GetComponentInChildren<MechHudController>())
        {
            player.GetComponentInChildren<MechHudController>().Initialize(player);
        }
        else
        {
            Debug.LogWarning("MechHudController not found on player.");
        }

        if (player.TryGetComponent<IKFootSolver>(out var footSolver))
            footSolver.enabled = true;

        player.SetParent(null, true);

        hasReleasedPlayer = true;
    }

    private void RotatePlayer()
    {
        player.rotation = Quaternion.Slerp(player.rotation, Quaternion.Euler(0, 0, 0), Time.deltaTime * (turnSpeed * 10));
        if (Quaternion.Angle(player.rotation, Quaternion.Euler(0, 0, 0)) < 0.1f)
        {
            player.rotation = Quaternion.Euler(0, 0, 0);
            hasReleasedPlayer = false;
            Destroy(this.gameObject, 1f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, spawnPoint.position);
            Gizmos.DrawWireSphere(spawnPoint.position, 2f);
        }
    }
}