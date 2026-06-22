using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class CameraJuice : MonoBehaviour
{
    public static CameraJuice Instance { get; private set; }

    private Camera mainCamera;
    private Vector3 originalPosition;
    private Tween activeShakeTween;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            originalPosition = transform.position;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    /// <summary>
    /// Triggers a clean screen shake using DOTween.
    /// </summary>
    /// <param name="duration">Duration of the shake in seconds.</param>
    /// <param name="strength">Strength/Intensity of the shake.</param>
    public void TriggerScreenShake(float duration, float strength)
    {
        if (activeShakeTween != null && activeShakeTween.IsActive())
        {
            activeShakeTween.Kill();
            transform.position = originalPosition;
        }

        // Shake only in X and Y planes, preserving the Z depth of the camera
        activeShakeTween = transform.DOShakePosition(duration, new Vector3(strength, strength, 0f), 15, 90f, false, true)
            .OnComplete(() => transform.position = originalPosition);
    }

    private void OnDestroy()
    {
        if (activeShakeTween != null && activeShakeTween.IsActive())
        {
            activeShakeTween.Kill();
        }
    }
}
