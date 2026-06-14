using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPredictor : MonoBehaviour
{
    [Header("Line Renderer Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float maxDistance = 25f;

    [Header("Visual Feedback")]
    [SerializeField, Range(0.1f, 1f)] private float fadeDecayFactor = 0.5f;
    [SerializeField] private Color trajectoryColor = Color.cyan;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        // Configure basic LineRenderer parameters programmatically
        lineRenderer.useWorldSpace = true;
    }

    /// <summary>
    /// Computes and draws the bounce trajectory path and updates the LineRenderer gradient.
    /// </summary>
    /// <param name="startPos">Initial starting position in world space.</param>
    /// <param name="direction">Normalized direction vector of the dash aim.</param>
    /// <param name="maxBounces">Maximum number of bounces allowed.</param>
    public void UpdateTrajectory(Vector2 startPos, Vector2 direction, int maxBounces)
    {
        if (lineRenderer == null) return;

        List<Vector3> points = new List<Vector3> { startPos };
        List<float> cumulativeDistances = new List<float> { 0f };

        Vector2 currentOrigin = startPos;
        Vector2 currentDir = direction.normalized;
        float remainingDistance = maxDistance;
        float totalDistance = 0f;

        // Trace trajectory using physics raycasts
        for (int bounce = 0; bounce <= maxBounces; bounce++)
        {
            RaycastHit2D hit = Physics2D.Raycast(currentOrigin, currentDir, remainingDistance, collisionLayers);

            if (hit.collider != null)
            {
                float stepDist = hit.distance;
                totalDistance += stepDist;
                cumulativeDistances.Add(totalDistance);
                points.Add(hit.point);

                remainingDistance -= stepDist;
                if (remainingDistance <= 0.05f) break;

                // Reflect and calculate new segment origin
                currentDir = Vector2.Reflect(currentDir, hit.normal).normalized;
                currentOrigin = hit.point + currentDir * 0.01f; // Offset to avoid self-collision
            }
            else
            {
                // Trace ending segment to max distance
                Vector2 endPoint = currentOrigin + currentDir * remainingDistance;
                totalDistance += remainingDistance;
                cumulativeDistances.Add(totalDistance);
                points.Add(endPoint);
                break;
            }
        }

        // Apply path positions
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());

        // Apply dynamic alpha gradient based on bounces
        ApplyBounceGradient(points.Count, cumulativeDistances, totalDistance);
    }

    private void ApplyBounceGradient(int pointCount, List<float> cumulativeDistances, float totalDistance)
    {
        if (totalDistance <= 0f || pointCount < 2) return;

        // Unity's Gradient supports up to 8 alpha keys. We distribute keys evenly across points.
        int keyCount = Mathf.Min(pointCount, 8);
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[keyCount];
        GradientColorKey[] colorKeys = new GradientColorKey[2];

        // Set base colors
        colorKeys[0] = new GradientColorKey(trajectoryColor, 0f);
        colorKeys[1] = new GradientColorKey(trajectoryColor, 1f);

        for (int i = 0; i < keyCount; i++)
        {
            // Distribute keys relative to points list index mapping
            int ptIndex = (i == keyCount - 1) ? pointCount - 1 : i * (pointCount - 1) / (keyCount - 1);
            float timeRatio = cumulativeDistances[ptIndex] / totalDistance;

            // Exponential decay per bounce to fade extended path segments smoothly
            float alphaValue = Mathf.Pow(fadeDecayFactor, ptIndex);
            alphaKeys[i] = new GradientAlphaKey(alphaValue, Mathf.Clamp01(timeRatio));
        }

        // Apply new Gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        lineRenderer.colorGradient = gradient;
    }
}
