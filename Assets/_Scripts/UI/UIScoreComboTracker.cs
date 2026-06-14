using UnityEngine;
using TMPro;
using DG.Tweening;

public class UIScoreComboTracker : MonoBehaviour
{
    [Header("Score Configuration")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private RectTransform scoreContainer;
    [SerializeField] private string scorePrefix = "SCORE: ";

    [Header("Combo Configuration")]
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private RectTransform comboContainer;
    [SerializeField] private string comboPrefix = "COMBO: ";

    private int displayedScore = 0;
    private int currentScore = 0;
    private Tween scoreTween;
    private Tween punchTween;

    private void OnEnable()
    {
        GameEvents.OnScoreChanged += HandleScoreChanged;
        GameEvents.OnComboChanged += HandleComboChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= HandleScoreChanged;
        GameEvents.OnComboChanged -= HandleComboChanged;
        
        if (scoreTween != null && scoreTween.IsActive()) scoreTween.Kill();
        if (punchTween != null && punchTween.IsActive()) punchTween.Kill();
    }

    private void Start()
    {
        // Programmatically enforce clean layout anchors to guarantee UI safety
        if (scoreContainer != null)
        {
            scoreContainer.anchorMin = new Vector2(0f, 1f);
            scoreContainer.anchorMax = new Vector2(0f, 1f);
            scoreContainer.pivot = new Vector2(0f, 1f);
            scoreContainer.anchoredPosition = new Vector2(40f, -40f);
        }

        if (comboContainer != null)
        {
            comboContainer.anchorMin = new Vector2(1f, 1f);
            comboContainer.anchorMax = new Vector2(1f, 1f);
            comboContainer.pivot = new Vector2(1f, 1f);
            comboContainer.anchoredPosition = new Vector2(-40f, -40f);
        }

        UpdateScoreText(0);
        UpdateComboText(0);
    }

    private void HandleScoreChanged(int newScore)
    {
        currentScore = newScore;

        if (scoreTween != null && scoreTween.IsActive()) scoreTween.Kill();

        // Smoothly interpolate score text value
        scoreTween = DOTween.To(() => displayedScore, x => {
            displayedScore = x;
            UpdateScoreText(displayedScore);
        }, currentScore, 0.4f).SetEase(Ease.OutQuad);

        // Subtle punch scale animation for that juicy responsive feel
        Transform targetTransform = scoreContainer != null ? scoreContainer.transform : scoreText.transform;
        if (targetTransform != null)
        {
            if (punchTween != null && punchTween.IsActive())
            {
                punchTween.Kill();
                targetTransform.localScale = Vector3.one;
            }
            punchTween = targetTransform.DOPunchScale(new Vector3(0.15f, 0.15f, 0.15f), 0.2f, 10, 1f);
        }
    }

    private void HandleComboChanged(int currentCombo)
    {
        UpdateComboText(currentCombo);

        if (currentCombo > 0)
        {
            Transform targetTransform = comboContainer != null ? comboContainer.transform : comboText.transform;
            if (targetTransform != null)
            {
                targetTransform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.15f, 10, 1f);
            }
        }
    }

    private void UpdateScoreText(int val)
    {
        if (scoreText != null)
        {
            scoreText.text = $"{scorePrefix}{val}";
        }
    }

    private void UpdateComboText(int val)
    {
        if (comboText != null)
        {
            if (val > 0)
            {
                comboText.text = $"{comboPrefix}{val}";
            }
            else
            {
                comboText.text = "";
            }
        }
    }
}
