using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Assets._Scripts.Manager;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Wave Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private EnemyDataSO[] enemyTypes;

    [Header("Time Loop Settings")]
    [SerializeField] private float initialPlayerLifeTime = 60f;
    [SerializeField] private float initialLevelSurvivalTime = 120f;

    // State properties
    public int CurrentWave { get; private set; } = 0;
    public int EnemiesRemaining { get; private set; } = 0;
    public float PlayerLifeTime { get; private set; }
    public float LevelSurvivalTime { get; private set; }
    public bool IsGameOver { get; private set; } = false;
    public bool IsVictory { get; private set; } = false;

    private int killsInCurrentDash = 0;
    private Transform enemiesContainer;
    private Canvas uiCanvas;
    private TextMeshProUGUI waveText; // Will display PlayerLifeTime
    private TextMeshProUGUI dashesText; // Will display LevelSurvivalTime
    private GameObject endGamePanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        PlayerLifeTime = initialPlayerLifeTime;
        LevelSurvivalTime = initialLevelSurvivalTime;

        // Find UI components
        var uiGo = GameObject.Find("UI");
        if (uiGo != null) uiCanvas = uiGo.GetComponent<Canvas>();

        enemiesContainer = GameObject.Find("Enemies")?.transform;
        if (enemiesContainer == null)
        {
            enemiesContainer = new GameObject("Enemies").transform;
        }

        SetupHUD();
        StartWave(1);
    }

    private void OnEnable()
    {
        GameEvents.OnEnemyDie += HandleEnemyKilled;
        GameEvents.OnPlayerDash += HandlePlayerDash;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyDie -= HandleEnemyKilled;
        GameEvents.OnPlayerDash -= HandlePlayerDash;
    }

    private void Update()
    {
        if (IsGameOver || IsVictory)
        {
            if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
            return;
        }

        // Run timers
        PlayerLifeTime -= Time.deltaTime;
        LevelSurvivalTime -= Time.deltaTime;

        if (PlayerLifeTime <= 0f)
        {
            PlayerLifeTime = 0f;
            TriggerGameOver();
        }

        if (LevelSurvivalTime <= 0f)
        {
            LevelSurvivalTime = 0f;
            TriggerVictory();
        }

        UpdateHUD();
    }

    private void SetupHUD()
    {
        if (uiCanvas == null) return;

        // Find or create UI Text for Wave (displays TIME)
        var waveGo = uiCanvas.transform.Find("Wave")?.gameObject;
        if (waveGo == null)
        {
            waveGo = new GameObject("Wave");
            waveGo.transform.SetParent(uiCanvas.transform, false);
            waveText = waveGo.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            waveText = waveGo.GetComponent<TextMeshProUGUI>();
        }

        // Find or create UI Text for Dashes (displays SURVIVE)
        var dashesGo = uiCanvas.transform.Find("Dashes")?.gameObject;
        if (dashesGo == null)
        {
            dashesGo = new GameObject("Dashes");
            dashesGo.transform.SetParent(uiCanvas.transform, false);
            dashesText = dashesGo.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            dashesText = dashesGo.GetComponent<TextMeshProUGUI>();
        }

        // Find others to rearrange
        var scoreGo = uiCanvas.transform.Find("Score")?.gameObject;
        var comboGo = uiCanvas.transform.Find("Combo")?.gameObject;
        var timerGo = uiCanvas.transform.Find("Timer")?.gameObject;

        // Try to get QuinqueFive SDF font from Combo
        TMP_FontAsset pixelFont = null;
        if (comboGo != null)
        {
            var comboTextComp = comboGo.GetComponent<TextMeshProUGUI>();
            if (comboTextComp != null)
            {
                pixelFont = comboTextComp.font;
            }
        }

        // Programmatically configure and position all 5 HUD elements cleanly
        ConfigureHUDText(waveGo, waveText, pixelFont, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -40), TextAlignmentOptions.Left, 18);
        
        if (comboGo != null)
        {
            var comboTextComp = comboGo.GetComponent<TextMeshProUGUI>();
            ConfigureHUDText(comboGo, comboTextComp, pixelFont, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -95), TextAlignmentOptions.Left, 18);
        }

        if (timerGo != null)
        {
            var timerTextComp = timerGo.GetComponent<TextMeshProUGUI>();
            ConfigureHUDText(timerGo, timerTextComp, pixelFont, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), TextAlignmentOptions.Center, 18);
        }

        ConfigureHUDText(dashesGo, dashesText, pixelFont, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), TextAlignmentOptions.Right, 18);

        if (scoreGo != null)
        {
            var scoreTextComp = scoreGo.GetComponent<TextMeshProUGUI>();
            ConfigureHUDText(scoreGo, scoreTextComp, pixelFont, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -95), TextAlignmentOptions.Right, 18);
        }
    }

    private void ConfigureHUDText(GameObject go, TextMeshProUGUI textComp, TMP_FontAsset font, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, TextAlignmentOptions alignment, float fontSize)
    {
        if (go == null || textComp == null) return;

        if (font != null)
        {
            textComp.font = font;
        }
        textComp.fontSize = fontSize;
        textComp.alignment = alignment;

        var rect = go.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(400, 50); // Set a wide size to prevent wrapping
            rect.anchoredPosition = anchoredPos;
        }
    }

    public void StartWave(int waveNumber)
    {
        CurrentWave = waveNumber;
        
        // Clean up leftover active enemies
        foreach (Transform child in enemiesContainer)
        {
            Destroy(child.gameObject);
        }

        int spawnCount = waveNumber * 2 + 1; // Wave 1: 3, Wave 2: 5, Wave 3: 7
        EnemiesRemaining = spawnCount;

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnEnemy();
        }

        UpdateHUD();
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || enemyTypes.Length == 0) return;

        // Bounds X: [-11, 11], Y: [-5, 5] to prevent spawning inside walls
        Vector3 spawnPos = new Vector3(Random.Range(-11f, 11f), Random.Range(-5f, 5f), 0f);

        GameObject enemyGo = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, enemiesContainer);
        EnemyController controller = enemyGo.GetComponent<EnemyController>();
        if (controller != null)
        {
            EnemyDataSO selectedData = enemyTypes[Random.Range(0, enemyTypes.Length)];
            controller.Initialize(selectedData);
        }
    }

    private void HandlePlayerDash()
    {
        // Dash triggers normally
    }

    private void HandleEnemyKilled(int score)
    {
        // Add default time based on kill (will be overridden by enemyData.timeBonusOnKill in EnemyController)
        // This is handled in EnemyController calling AddPlayerTime directly.
        
        EnemiesRemaining--;
        UpdateHUD();

        if (EnemiesRemaining <= 0)
        {
            StartCoroutine(NextWaveRoutine());
        }
    }

    private IEnumerator NextWaveRoutine()
    {
        yield return new WaitForSeconds(1f);
        if (!IsGameOver && !IsVictory)
        {
            StartWave(CurrentWave + 1);
        }
    }

    public void AddPlayerTime(float amount)
    {
        if (IsGameOver || IsVictory) return;
        PlayerLifeTime += amount;
        SpawnTimeText($"+{amount:F0}s", Color.green);
    }

    public void PenalizePlayerTime(float amount)
    {
        if (IsGameOver || IsVictory) return;
        PlayerLifeTime = Mathf.Max(0f, PlayerLifeTime - amount);
        SpawnTimeText($"-{amount:F0}s", Color.red);
        if (PlayerLifeTime <= 0f)
        {
            TriggerGameOver();
        }
    }

    private void SpawnTimeText(string value, Color color)
    {
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(playerGo.transform.position) + new Vector3(0, 80, 0);
        GameObject textObj = ObjectPooler.Instance.Spawn(PoolType.DamageText.ToString(), screenPos, Quaternion.identity, 0.8f);
        if (textObj != null)
        {
            textObj.transform.position = screenPos;
            textObj.SetActive(true);
            DamageText dmgText = textObj.GetComponent<DamageText>();
            if (dmgText != null)
            {
                dmgText.Setup(value, color);
            }
        }
    }

    public void ResetDashKills()
    {
        killsInCurrentDash = 0;
    }

    public void RegisterDashKill()
    {
        killsInCurrentDash++;
    }

    public void ResolveDashEnd()
    {
        if (killsInCurrentDash > 1)
        {
            // Combo time rewards:
            // 2 kills: +4s
            // 3 kills: +10s
            // 4+ kills: +18s
            float bonus = 0f;
            if (killsInCurrentDash == 2) bonus = 4f;
            else if (killsInCurrentDash == 3) bonus = 10f;
            else if (killsInCurrentDash >= 4) bonus = 18f;

            AddPlayerTime(bonus);

            // Spawn floating combo text above player
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(playerGo.transform.position) + new Vector3(0, 140, 0);
                GameObject textObj = ObjectPooler.Instance.Spawn(PoolType.DamageText.ToString(), screenPos, Quaternion.identity, 0.8f);
                if (textObj != null)
                {
                    textObj.transform.position = screenPos;
                    textObj.SetActive(true);
                    DamageText dmgText = textObj.GetComponent<DamageText>();
                    if (dmgText != null)
                    {
                        dmgText.Setup($"COMBO x{killsInCurrentDash}! +{bonus}s", Color.red);
                    }
                }
            }
        }
        killsInCurrentDash = 0;
    }

    private void TriggerGameOver()
    {
        IsGameOver = true;
        Time.timeScale = 0.5f;
        CreateEndGameScreen("GAME OVER", new Color(0.9f, 0.2f, 0.2f), "R to Retry");
    }

    private void TriggerVictory()
    {
        IsVictory = true;
        Time.timeScale = 0.5f;
        CreateEndGameScreen("VICTORY", new Color(0.2f, 0.9f, 0.5f), "R to Play Again");
    }

    private void CreateEndGameScreen(string title, Color titleColor, string subtitleText)
    {
        if (uiCanvas == null) return;

        // Create Panel
        endGamePanel = new GameObject("EndGamePanel");
        endGamePanel.transform.SetParent(uiCanvas.transform, false);
        
        var rect = endGamePanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var img = endGamePanel.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.08f, 0.85f); // Premium semi-translucent dark background

        // Central Box
        var boxGo = new GameObject("ContentBox");
        boxGo.transform.SetParent(endGamePanel.transform, false);
        var boxRect = boxGo.AddComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(400, 250);
        var boxImg = boxGo.AddComponent<Image>();
        boxImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f); // Slate dark panel

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(boxGo.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 42;
        titleText.color = titleColor;
        titleText.alignment = TextAlignmentOptions.Center;
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 50);

        // Subtitle
        var subGo = new GameObject("Subtitle");
        subGo.transform.SetParent(boxGo.transform, false);
        var subText = subGo.AddComponent<TextMeshProUGUI>();
        subText.text = subtitleText;
        subText.fontSize = 20;
        subText.color = Color.white;
        subText.alignment = TextAlignmentOptions.Center;
        var subRect = subGo.GetComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, -50);
    }

    private void UpdateHUD()
    {
        if (waveText != null)
        {
            waveText.text = $"TIME: {PlayerLifeTime:F1}s";
            waveText.color = PlayerLifeTime < 15f ? Color.red : Color.cyan;
        }
        if (dashesText != null)
        {
            dashesText.text = $"SURVIVE: {LevelSurvivalTime:F0}s";
        }
    }

    public void RegisterSplitEnemy(int count = 1)
    {
        EnemiesRemaining += count;
        UpdateHUD();
    }

    public void DeductDashes(int amount)
    {
        // Dashes are removed from core loop, replaced by PenalizePlayerTime.
        PenalizePlayerTime(10f); // Bomber explosion penalizes 10 seconds!
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
