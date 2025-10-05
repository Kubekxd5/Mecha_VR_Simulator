using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class PathGenerator : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private int pathLength = 10;
    [SerializeField] private GameObject pointPrefab;

    [Header("Raycasting Settings")]
    [SerializeField] private int numberOfRays = 12;
    [SerializeField] private float raycastDistance = 15f;
    [Range(0, 360)]
    [SerializeField] private float spreadAngle = 120f;
    [SerializeField] private LayerMask obstacleLayers;

    private LineRenderer pathLineRenderer;
    private List<GameObject> instantiatedPoints = new List<GameObject>();

    private void OnValidate()
    {
        if (pathLineRenderer == null)
        {
            pathLineRenderer = GetComponent<LineRenderer>();
        }

        pathLineRenderer.loop = false;
    }

    [ContextMenu("Generate New Path")]
    public void GeneratePath()
    {
        foreach (GameObject point in instantiatedPoints)
        {
            if (Application.isPlaying) Destroy(point);
            else DestroyImmediate(point);
        }
        instantiatedPoints.Clear();
        pathLineRenderer.positionCount = 0;

        List<Vector3> pathPoints = new List<Vector3>();
        pathPoints.Add(transform.position);

        GameObject startPointObject = Instantiate(pointPrefab, transform.position, Quaternion.identity, transform);
        instantiatedPoints.Add(startPointObject);

        pathLineRenderer.positionCount = 1;
        pathLineRenderer.SetPosition(0, transform.position);

        Vector3 currentDirection = transform.forward;

        for (int i = 0; i < pathLength; i++)
        {
            Vector3 lastPoint = pathPoints[pathPoints.Count - 1];
            Vector3 nextPoint = FindBestNextPoint(lastPoint, currentDirection);

            if (nextPoint == lastPoint)
            {
                Debug.LogWarning("Path generation stopped early because it got stuck.");
                break;
            }

            GameObject newPointObject = Instantiate(pointPrefab, nextPoint, Quaternion.identity, transform);
            instantiatedPoints.Add(newPointObject);

            pathLineRenderer.positionCount++;
            pathLineRenderer.SetPosition(pathLineRenderer.positionCount - 1, nextPoint);

            pathPoints.Add(nextPoint);
            currentDirection = (nextPoint - lastPoint).normalized;
        }
    }

    private Vector3 FindBestNextPoint(Vector3 origin, Vector3 forwardDirection)
    {
        // ... (This method is unchanged and correct)
        float bestDistance = -1f;
        Vector3 bestPoint = origin;
        float startAngle = -spreadAngle / 2f;
        float angleIncrement = spreadAngle / (numberOfRays - 1);

        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = startAngle + i * angleIncrement;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forwardDirection;
            RaycastHit hit;
            float currentDistance;
            Vector3 currentTargetPoint;

            if (Physics.Raycast(origin, direction, out hit, raycastDistance, obstacleLayers))
            {
                currentDistance = hit.distance;
                currentTargetPoint = hit.point;
            }
            else
            {
                currentDistance = raycastDistance;
                currentTargetPoint = origin + direction * raycastDistance;
            }

            if (currentDistance > bestDistance)
            {
                bestDistance = currentDistance;
                bestPoint = currentTargetPoint;
            }
        }
        return bestPoint;
    }
}