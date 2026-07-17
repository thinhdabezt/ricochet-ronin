using UnityEngine;
using TMPro;
using DG.Tweening;

public class InGameHUDController : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI timeText;        // Displays PlayerLifeTime
    [SerializeField] private TextMeshProUGUI scoreText;       // Displays Score
    [SerializeField] private TextMeshProUGUI comboText;       // Displays Combo
    [SerializeField] private TextMeshProUGUI objectiveText;   // Displays LevelSurvivalTime (Timer)
    [SerializeField] private TextMeshProUGUI floorText;       // Displays Floor number

    [Header("UI Containers for Punch Scaling")]
    [SerializeField] private RectTransform timeContainer;
    [SerializeField] private RectTransform scoreContainer;
    [SerializeField] private RectTransform comboContainer;
    [SerializeField] private RectTransform objectiveContainer;
    [SerializeField] private RectTransform floorContainer;

    [Header("HUD Settings")]
    [SerializeField] private string timePrefix = "TIME: ";
    [SerializeField] private string scorePrefix = "SCORE: ";
    [SerializeField] private string comboPrefix = "COMBO: ";
    [SerializeField] private string objectivePrefix = "Survive: ";
    [SerializeField] private string objectiveSuffix = " seconds remaining";
    [SerializeField] private string floorPrefix = "FLOOR: ";

    private int displayedScore = 0;
    private int currentScore = 0;
    private Tween scoreTween;
    private Tween scorePunchTween;
    private Tween comboPunchTween;
    private Tween timePunchTween;

    private float lastTimeValue = -1f;

    private void OnEnable()
    {
        GameEvents.OnSurvivalTimeChanged += UpdateObjectiveUI;
        GameEvents.OnScoreChanged += HandleScoreChanged;
        GameEvents.OnComboChanged += HandleComboChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnSurvivalTimeChanged -= UpdateObjectiveUI;
        GameEvents.OnScoreChanged -= HandleScoreChanged;
        GameEvents.OnComboChanged -= HandleComboChanged;

        if (scoreTween != null && scoreTween.IsActive()) scoreTween.Kill();
        if (scorePunchTween != null && scorePunchTween.IsActive()) scorePunchTween.Kill();
        if (comboPunchTween != null && comboPunchTween.IsActive()) comboPunchTween.Kill();
        if (timePunchTween != null && timePunchTween.IsActive()) timePunchTween.Kill();
    }

    private void Start()
    {
        // Find or create Wave (displays PlayerLifeTime) at runtime if timeText is not assigned
        if (timeText == null)
        {
            var waveGo = transform.Find("Wave")?.gameObject;
            if (waveGo == null)
            {
                waveGo = new GameObject("Wave");
                waveGo.transform.SetParent(transform, false);
                timeText = waveGo.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                timeText = waveGo.GetComponent<TextMeshProUGUI>();
            }
        }

        // Find other text components if not assigned
        if (scoreText == null) scoreText = transform.Find("Score")?.GetComponent<TextMeshProUGUI>();
        if (comboText == null) comboText = transform.Find("Combo")?.GetComponent<TextMeshProUGUI>();
        if (objectiveText == null) objectiveText = transform.Find("Timer")?.GetComponent<TextMeshProUGUI>();

        // Find or create Floor UI at runtime if floorText is not assigned
        if (floorText == null)
        {
            var floorGo = transform.Find("Floor")?.gameObject;
            if (floorGo == null)
            {
                floorGo = new GameObject("Floor");
                floorGo.transform.SetParent(transform, false);
                floorText = floorGo.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                floorText = floorGo.GetComponent<TextMeshProUGUI>();
            }
        }

        // Always enforce QuinqueFive pixel font and correct sizes/alignments at runtime
        TMP_FontAsset pixelFont = null;
        if (scoreText != null && scoreText.font != null && scoreText.font.name.Contains("Quinque")) pixelFont = scoreText.font;
        else if (comboText != null && comboText.font != null && comboText.font.name.Contains("Quinque")) pixelFont = comboText.font;
        else if (objectiveText != null && objectiveText.font != null && objectiveText.font.name.Contains("Quinque")) pixelFont = objectiveText.font;

        if (pixelFont != null)
        {
            if (timeText != null) timeText.font = pixelFont;
            if (scoreText != null) scoreText.font = pixelFont;
            if (comboText != null) comboText.font = pixelFont;
            if (objectiveText != null) objectiveText.font = pixelFont;
            if (floorText != null) floorText.font = pixelFont;
        }

        if (timeText != null)
        {
            timeText.fontSize = 18;
            timeText.alignment = TextAlignmentOptions.Left;
        }
        if (scoreText != null)
        {
            scoreText.fontSize = 18;
            scoreText.alignment = TextAlignmentOptions.Left;
        }
        if (comboText != null)
        {
            comboText.fontSize = 18;
            comboText.alignment = TextAlignmentOptions.Right;
        }
        if (floorText != null)
        {
            floorText.fontSize = 18;
            floorText.alignment = TextAlignmentOptions.Right;
        }
        if (objectiveText != null)
        {
            objectiveText.fontSize = 18;
            objectiveText.alignment = TextAlignmentOptions.Center;
        }

        ConfigureLayouts();

        UpdateScoreText(0);
        UpdateComboText(0);
    }

    private void Update()
    {
        // Update Player Life Time (Time Loop) in real-time
        if (GameManager.Instance != null && timeText != null)
        {
            float playerLifeTime = GameManager.Instance.PlayerLifeTime;
            
            // Format to 1 decimal place for precision (e.g. TIME: 55.4s)
            timeText.text = $"{timePrefix}{playerLifeTime:F1}s";

            // Trigger a subtle punch scale if life time is increased (e.g. time gained on kill)
            if (playerLifeTime > lastTimeValue + 0.5f && lastTimeValue > 0f)
            {
                PunchTimeContainer();
            }
            lastTimeValue = playerLifeTime;

            // Change color dynamically based on time remaining
            if (playerLifeTime > 30f)
            {
                timeText.color = new Color(0f, 0.95f, 1f); // Neon Cyan
            }
            else if (playerLifeTime > 15f)
            {
                timeText.color = new Color(1f, 0.84f, 0f); // Neon Yellow
            }
            else
            {
                timeText.color = new Color(1f, 0f, 0.33f); // Neon Crimson
            }
        }
        
        // Update Floor display in real-time
        if (GameManager.Instance != null && floorText != null)
        {
            floorText.text = $"{floorPrefix}{GameManager.Instance.CurrentFloor}";
            floorText.color = new Color(0.2f, 0.9f, 0.5f); // Neon Green
        }
    }

    private void ConfigureLayouts()
    {
        // Programmatically enforce matching positions to look unified and clean:
        // Increased width from 400f to 800f to prevent text wrapping on long messages
        Vector2 defaultSizeDelta = new Vector2(800f, 50f);

        // Top-Left corner: TIME (at y: -40), SCORE (at y: -95)
        if (timeContainer != null)
        {
            timeContainer.anchorMin = new Vector2(0f, 1f);
            timeContainer.anchorMax = new Vector2(0f, 1f);
            timeContainer.pivot = new Vector2(0f, 1f);
            timeContainer.anchoredPosition = new Vector2(40f, -40f);
        }
        else if (timeText != null)
        {
            var rect = timeText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = defaultSizeDelta;
            rect.anchoredPosition = new Vector2(40f, -40f);
        }

        if (scoreContainer != null)
        {
            scoreContainer.anchorMin = new Vector2(0f, 1f);
            scoreContainer.anchorMax = new Vector2(0f, 1f);
            scoreContainer.pivot = new Vector2(0f, 1f);
            scoreContainer.anchoredPosition = new Vector2(40f, -95f);
        }
        else if (scoreText != null)
        {
            var rect = scoreText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = defaultSizeDelta;
            rect.anchoredPosition = new Vector2(40f, -95f);
        }

        // Top-Right corner: COMBO (at y: -40)
        if (comboContainer != null)
        {
            comboContainer.anchorMin = new Vector2(1f, 1f);
            comboContainer.anchorMax = new Vector2(1f, 1f);
            comboContainer.pivot = new Vector2(1f, 1f);
            comboContainer.anchoredPosition = new Vector2(-40f, -40f);
        }
        else if (comboText != null)
        {
            var rect = comboText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = defaultSizeDelta;
            rect.anchoredPosition = new Vector2(-40f, -40f);
        }

        // Top-Center: OBJECTIVE (at y: -40)
        if (objectiveContainer != null)
        {
            objectiveContainer.anchorMin = new Vector2(0.5f, 1f);
            objectiveContainer.anchorMax = new Vector2(0.5f, 1f);
            objectiveContainer.pivot = new Vector2(0.5f, 1f);
            objectiveContainer.anchoredPosition = new Vector2(0f, -40f);
        }
        else if (objectiveText != null)
        {
            var rect = objectiveText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = defaultSizeDelta;
            rect.anchoredPosition = new Vector2(0f, -40f);
        }
 
        // Top-Right corner: FLOOR (at y: -95) - Symmetrical to SCORE
        if (floorContainer != null)
        {
            floorContainer.anchorMin = new Vector2(1f, 1f);
            floorContainer.anchorMax = new Vector2(1f, 1f);
            floorContainer.pivot = new Vector2(1f, 1f);
            floorContainer.anchoredPosition = new Vector2(-40f, -95f);
        }
        else if (floorText != null)
        {
            var rect = floorText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = defaultSizeDelta;
            rect.anchoredPosition = new Vector2(-40f, -95f);
        }
    }

    private void UpdateObjectiveUI(int secondsRemaining)
    {
        if (objectiveText != null)
        {
            if (secondsRemaining > 0)
            {
                objectiveText.text = $"{objectivePrefix}{secondsRemaining}{objectiveSuffix}";
            }
            else
            {
                objectiveText.text = "PORTAL OPEN! ENTER TO ADVANCE";
            }
        }
    }

    private void HandleScoreChanged(int newScore)
    {
        currentScore = newScore;

        if (scoreTween != null && scoreTween.IsActive()) scoreTween.Kill();

        scoreTween = DOTween.To(() => displayedScore, x => {
            displayedScore = x;
            UpdateScoreText(displayedScore);
        }, currentScore, 0.4f).SetEase(Ease.OutQuad);

        PunchScoreContainer();
    }

    private void HandleComboChanged(int currentCombo)
    {
        UpdateComboText(currentCombo);

        if (currentCombo > 0)
        {
            Transform targetTransform = comboContainer != null ? comboContainer.transform : (comboText != null ? comboText.transform : null);
            if (targetTransform != null)
            {
                if (comboPunchTween != null && comboPunchTween.IsActive())
                {
                    comboPunchTween.Kill();
                    targetTransform.localScale = Vector3.one;
                }
                comboPunchTween = targetTransform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.15f, 10, 1f);
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

    private void PunchScoreContainer()
    {
        Transform targetTransform = scoreContainer != null ? scoreContainer.transform : (scoreText != null ? scoreText.transform : null);
        if (targetTransform != null)
        {
            if (scorePunchTween != null && scorePunchTween.IsActive())
            {
                scorePunchTween.Kill();
                targetTransform.localScale = Vector3.one;
            }
            scorePunchTween = targetTransform.DOPunchScale(new Vector3(0.15f, 0.15f, 0.15f), 0.2f, 10, 1f);
        }
    }

    private void PunchTimeContainer()
    {
        Transform targetTransform = timeContainer != null ? timeContainer.transform : (timeText != null ? timeText.transform : null);
        if (targetTransform != null)
        {
            if (timePunchTween != null && timePunchTween.IsActive())
            {
                timePunchTween.Kill();
                targetTransform.localScale = Vector3.one;
            }
            timePunchTween = targetTransform.DOPunchScale(new Vector3(0.15f, 0.15f, 0.15f), 0.2f, 10, 1f);
        }
    }
}
