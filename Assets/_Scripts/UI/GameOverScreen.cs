using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameOverScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dishonoredText;
    [SerializeField] private Button resheatheButton;
    [SerializeField] private CanvasGroup transitionOverlay;

    // Registered event for resetting the scene
    public static event Action OnRetryTriggered;

    private bool isRetrying = false;

    private void OnEnable()
    {
        GameUIManager.OnStateChanged += HandleStateChanged;
        if (resheatheButton != null)
        {
            resheatheButton.onClick.AddListener(OnResheatheButtonClicked);
        }
    }

    private void OnDisable()
    {
        GameUIManager.OnStateChanged -= HandleStateChanged;
        if (resheatheButton != null)
        {
            resheatheButton.onClick.RemoveListener(OnResheatheButtonClicked);
        }
    }

    private void Start()
    {
        if (transitionOverlay != null)
        {
            transitionOverlay.alpha = 0f;
            transitionOverlay.blocksRaycasts = false;
        }
        isRetrying = false;
    }

    private void Update()
    {
        // Listen to Keyboard input R to trigger retry
        if (GameUIManager.Instance != null && GameUIManager.Instance.CurrentState == GameState.GameOver && !isRetrying)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                TriggerRetry();
            }
        }
    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver)
        {
            isRetrying = false;
            TriggerGameOverAnimation();
        }
    }

    private void TriggerGameOverAnimation()
    {
        if (dishonoredText == null) return;

        // Setup initial scale of 3x for the slam effect
        dishonoredText.transform.localScale = new Vector3(3f, 3f, 3f);
        dishonoredText.alpha = 0f;

        // Slam down animation using OutBounce easing
        dishonoredText.DOFade(1f, 0.3f).SetUpdate(true);
        dishonoredText.transform.DOScale(1f, 0.6f)
            .SetEase(Ease.OutBounce)
            .SetUpdate(true); // Animate independently of slow timescales
    }

    private void OnResheatheButtonClicked()
    {
        if (!isRetrying)
        {
            TriggerRetry();
        }
    }

    private void TriggerRetry()
    {
        isRetrying = true;

        if (transitionOverlay != null)
        {
            transitionOverlay.blocksRaycasts = true;

            // Fullscreen transition overlay fade over 0.3s
            transitionOverlay.DOFade(1f, 0.3f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    OnRetryTriggered?.Invoke();
                });
        }
        else
        {
            OnRetryTriggered?.Invoke();
        }
    }
}
