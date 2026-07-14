using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIDiegeticTimer : MonoBehaviour
{
    [Header("Target Tracking")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);

    [Header("UI References")]
    [SerializeField] private Image radialImage;

    [Header("Timer Settings")]
    [SerializeField] private float maxTime = 100f;
    [SerializeField] private float remainingTime = 100f;

    // Neon colors based on specifications
    private readonly Color neonCyan = new Color(0f, 0.95f, 1f, 1f);       // #00F3FF
    private readonly Color neonYellow = new Color(1f, 0.84f, 0f, 1f);     // #FFD700
    private readonly Color neonCrimson = new Color(1f, 0f, 0.33f, 1f);    // #FF0055

    private bool isUrgent = false;
    private Tween pulseTween;
    private Tween flashTween;

    // Expose property for isAiming state
    public bool isAiming { get; set; } = false;

    public float RemainingTime
    {
        get => remainingTime;
        set => remainingTime = Mathf.Clamp(value, 0f, maxTime);
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                playerTransform = playerGo.transform;
            }
        }

        remainingTime = maxTime;
        UpdateUI();
    }

    private void LateUpdate()
    {
        if (playerTransform != null)
        {
            transform.position = playerTransform.position + offset;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null)
        {
            remainingTime = GameManager.Instance.PlayerLifeTime;

            // Automatically detect player aiming state and set local property
            if (playerTransform != null)
            {
                var player = playerTransform.GetComponent<Player>();
                isAiming = player != null && player.StateMachine != null && player.StateMachine.CurrentState is PlayerAimingState;
            }
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (radialImage == null) return;

        float fillAmount = remainingTime / maxTime;
        radialImage.fillAmount = fillAmount;

        // Color logic based on fill threshold
        if (fillAmount > 0.5f)
        {
            radialImage.color = neonCyan;
            StopUrgentEffects();
        }
        else if (fillAmount > 0.25f)
        {
            radialImage.color = neonYellow;
            StopUrgentEffects();
        }
        else
        {
            radialImage.color = neonCrimson;
            StartUrgentEffects();
        }
    }

    private void StartUrgentEffects()
    {
        if (isUrgent) return;
        isUrgent = true;

        // Scale loop
        pulseTween = radialImage.transform.DOScale(1.2f, 0.4f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(radialImage.gameObject);

        // Alpha flashing loop
        flashTween = radialImage.DOFade(0.3f, 0.25f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(radialImage.gameObject);
    }

    private void StopUrgentEffects()
    {
        if (!isUrgent) return;
        isUrgent = false;

        if (pulseTween != null && pulseTween.IsActive()) pulseTween.Kill();
        if (flashTween != null && flashTween.IsActive()) flashTween.Kill();

        if (radialImage != null)
        {
            radialImage.transform.localScale = Vector3.one;
            Color col = radialImage.color;
            col.a = 1f;
            radialImage.color = col;
        }
    }

    private void OnDestroy()
    {
        StopUrgentEffects();
    }
}
