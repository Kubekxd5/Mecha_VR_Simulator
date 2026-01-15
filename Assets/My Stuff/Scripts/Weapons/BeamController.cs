using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class BeamController : MonoBehaviour
{
    private LineRenderer lr;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask targetLayers = -1;
    public LayerMask TargetLayers => targetLayers;

    [Header("Visual Settings")]
    [SerializeField] private bool fadeWidth = true;
    [SerializeField] private float initialWidthMultiplier = 1f;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
    }

    public void SetupBeam(Vector3 start, Vector3 end, float duration = 0.1f)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        initialWidthMultiplier = lr.widthMultiplier;

        StartCoroutine(FadeOut(duration));
    }

    private IEnumerator FadeOut(float duration)
    {
        float elapsed = 0;

        Gradient initialGradient = lr.colorGradient;
        float startWidth = lr.widthMultiplier;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = 1 - (elapsed / duration);

            lr.startColor = new Color(lr.startColor.r, lr.startColor.g, lr.startColor.b, percent);
            lr.endColor = new Color(lr.endColor.r, lr.endColor.g, lr.endColor.b, percent);

            if (fadeWidth)
            {
                lr.widthMultiplier = startWidth * percent;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}