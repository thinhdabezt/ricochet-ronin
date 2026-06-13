using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Wave Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private EnemyDataSO[] enemyTypes;
    [SerializeField] private int totalWaves = 3;

    // State properties
    public int CurrentWave { get; private set; } = 0;
    public int EnemiesRemaining { get; private set; } = 0;
    public int DashesRemaining { get; private set; } = 0;
    public bool IsGameOver { get; private set; } = false;
    public bool IsVictory { get; private set; } = false;

    private Transform enemiesContainer;
    private Canvas uiCanvas;
    private TextMeshProUGUI waveText;
    private TextMeshProUGUI dashesText;
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
        }
    }

    private void SetupHUD()
    {
        if (uiCanvas == null) return;

        // Find or create UI Text for HUD
        var waveGo = uiCanvas.transform.Find("Wave")?.gameObject;
        if (waveGo == null)
        {
            waveGo = new GameObject("Wave");
            waveGo.transform.SetParent(uiCanvas.transform, false);
            waveText = waveGo.AddComponent<TextMeshProUGUI>();
            waveText.fontSize = 24;
            waveText.alignment = TextAlignmentOptions.TopLeft;
            var rect = waveGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20);
        }
        else
        {
            waveText = waveGo.GetComponent<TextMeshProUGUI>();
        }

        var dashesGo = uiCanvas.transform.Find("Dashes")?.gameObject;
        if (dashesGo == null)
        {
            dashesGo = new GameObject("Dashes");
            dashesGo.transform.SetParent(uiCanvas.transform, false);
            dashesText = dashesGo.AddComponent<TextMeshProUGUI>();
            dashesText.fontSize = 24;
            dashesText.alignment = TextAlignmentOptions.TopRight;
            var rect = dashesGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
        }
        else
        {
            dashesText = dashesGo.GetComponent<TextMeshProUGUI>();
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
        DashesRemaining = waveNumber * 3 + 2; // Wave 1: 5 dashes, Wave 2: 8 dashes, Wave 3: 11 dashes

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
        if (IsGameOver || IsVictory) return;

        DashesRemaining--;
        UpdateHUD();
    }

    private void HandleEnemyKilled(int score)
    {
        if (IsGameOver || IsVictory) return;

        EnemiesRemaining--;
        UpdateHUD();

        if (EnemiesRemaining <= 0)
        {
            if (CurrentWave < totalWaves)
            {
                StartCoroutine(NextWaveRoutine());
            }
            else
            {
                TriggerVictory();
            }
        }
    }

    private IEnumerator NextWaveRoutine()
    {
        yield return new WaitForSeconds(1f);
        StartWave(CurrentWave + 1);
    }

    public void CheckGameOverCondition()
    {
        if (IsGameOver || IsVictory) return;

        // If player has run out of moves and enemies still remain
        if (DashesRemaining <= 0 && EnemiesRemaining > 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        IsGameOver = true;
        Time.timeScale = 0.5f; // Slow down time slightly for dramatic effect
        CreateEndGameScreen("GAME OVER", new Color(0.9f, 0.2f, 0.2f), "R to Retry");
    }

    private void TriggerVictory()
    {
        IsVictory = true;
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
            waveText.text = $"Wave: {CurrentWave} / {totalWaves}";
        }
        if (dashesText != null)
        {
            dashesText.text = $"Dashes: {DashesRemaining}";
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
