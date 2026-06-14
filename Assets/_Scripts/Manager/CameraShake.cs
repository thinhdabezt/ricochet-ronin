using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 originalPos;
    private Coroutine activeShakeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            originalPos = transform.position; // Cache the default static camera position
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ShakeCamera(float intensity, float duration)
    {
        if (activeShakeCoroutine != null)
        {
            StopCoroutine(activeShakeCoroutine);
            transform.position = originalPos; // Reset camera position before launching a new shake
        }
        activeShakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity * 0.1f;
            float y = Random.Range(-1f, 1f) * intensity * 0.1f;

            transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
        activeShakeCoroutine = null;
    }
}
