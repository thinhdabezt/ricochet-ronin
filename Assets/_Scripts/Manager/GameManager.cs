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

    // Upgrades and Roguelike variables
    private float killTimeBonusAddition = 0f;
    private float aimingDrainMultiplierModifier = 1f;
    private int currentFloor = 1;
    private bool isFloorCleared = false;
    private GameObject draftingPanelGo;

    public float KillTimeBonusModifier => killTimeBonusAddition;
    public int CurrentFloor => currentFloor;

    private int killsInCurrentDash = 0;
    private Transform enemiesContainer;
    private Canvas uiCanvas;
    private TextMeshProUGUI waveText; // Will display PlayerLifeTime
    private TextMeshProUGUI dashesText; // Will display LevelSurvivalTime
    private GameObject endGamePanel;
    private int lastSecondsRemaining = -1;

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

        // Determine player aiming state to apply triple drain speed (influenced by upgrades)
        float drainMultiplier = 1f;
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            var player = playerGo.GetComponent<Player>();
            if (player != null && player.StateMachine != null && player.StateMachine.CurrentState is PlayerAimingState)
            {
                drainMultiplier = 3f * aimingDrainMultiplierModifier;
            }
        }

        // Run timers
        PlayerLifeTime -= Time.deltaTime * drainMultiplier;
        
        if (LevelSurvivalTime > 0f)
        {
            LevelSurvivalTime = Mathf.Max(0f, LevelSurvivalTime - Time.deltaTime);
        }

        if (PlayerLifeTime <= 0f)
        {
            PlayerLifeTime = 0f;
            TriggerGameOver();
        }

        // Check if floor is cleared (Timer finished AND all enemies killed)
        if (LevelSurvivalTime <= 0f && EnemiesRemaining <= 0 && !isFloorCleared)
        {
            TriggerFloorClear();
        }

        // Broadcast event only when remaining seconds integer changes (once per second)
        int currentSecs = Mathf.CeilToInt(LevelSurvivalTime);
        if (currentSecs != lastSecondsRemaining)
        {
            lastSecondsRemaining = currentSecs;
            GameEvents.OnSurvivalTimeChanged?.Invoke(currentSecs);
        }
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
        waveText.text = "";
        
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
        dashesText.text = "";

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
    }

    private EnemyDataSO GetRandomEnemyDataForCurrentFloor()
    {
        if (enemyTypes == null || enemyTypes.Length == 0) return null;

        List<EnemyDataSO> allowedTypes = new List<EnemyDataSO>();

        foreach (var enemy in enemyTypes)
        {
            if (enemy == null) continue;

            // Tier unlock conditions based on currentFloor:
            // Floor 1: Only simple enemies (None special mechanic, Static or Patrol movement)
            if (currentFloor == 1)
            {
                if (enemy.specialMechanic == EnemySpecialMechanic.None &&
                    (enemy.movementType == EnemyMovementType.Static || enemy.movementType == EnemyMovementType.Patrol))
                {
                    allowedTypes.Add(enemy);
                }
            }
            // Floor 2: Simple + Shielded Vanguard (FrontShield) + Mitosis Slime (SplitOnDeath)
            else if (currentFloor == 2)
            {
                if (enemy.specialMechanic == EnemySpecialMechanic.None ||
                    enemy.specialMechanic == EnemySpecialMechanic.FrontShield ||
                    enemy.specialMechanic == EnemySpecialMechanic.SplitOnDeath)
                {
                    allowedTypes.Add(enemy);
                }
            }
            // Floor 3+: All types unlock (WeaverDrone, Blink, ExplodeOnDeath)
            else
            {
                allowedTypes.Add(enemy);
            }
        }

        if (allowedTypes.Count == 0)
        {
            return enemyTypes[Random.Range(0, enemyTypes.Length)];
        }

        return allowedTypes[Random.Range(0, allowedTypes.Count)];
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
            EnemyDataSO baseData = GetRandomEnemyDataForCurrentFloor();
            
            // Create a runtime copy to avoid permanent asset file modifications on disk
            EnemyDataSO scaledData = Instantiate(baseData);

            // Apply scaling based on floor level
            float difficultyMultiplier = 1f + (currentFloor - 1) * 0.15f; // +15% move speed & score per floor
            scaledData.moveSpeed *= difficultyMultiplier;
            scaledData.maxHealth += (currentFloor - 1) / 2; // +1 health every 2 floors
            scaledData.scoreValue = Mathf.RoundToInt(scaledData.scoreValue * difficultyMultiplier);

            controller.Initialize(scaledData);
        }
    }

    private void HandlePlayerDash()
    {
        // Dash triggers normally
    }

    private void HandleEnemyKilled(int score, Vector2 position)
    {
        // Add default time based on kill (will be overridden by enemyData.timeBonusOnKill in EnemyController)
        // This is handled in EnemyController calling AddPlayerTime directly.
        
        EnemiesRemaining--;

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
            if (LevelSurvivalTime > 0f)
            {
                StartWave(CurrentWave + 1);
            }
            else
            {
                // Timer is at 0, and player just killed the last remaining enemy
                if (!isFloorCleared)
                {
                    TriggerFloorClear();
                }
            }
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

    private void RemoveLegacyHUDMethodPlaceholder() {}

    public void RegisterSplitEnemy(int count = 1)
    {
        EnemiesRemaining += count;
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

    private void TriggerFloorClear()
    {
        isFloorCleared = true;

        // Clean up remaining entities in the level (like slow zones)
        var slowZones = FindObjectsOfType<SlowZone>();
        foreach (var sz in slowZones)
        {
            Destroy(sz.gameObject);
        }

        // Spawn Portal at center
        SpawnPortal();

        // Trigger Card Selection UI Overlay
        CreateDraftingPanel();
    }

    private void SpawnPortal()
    {
        GameObject portalGo = new GameObject("NextFloorPortal");
        portalGo.transform.position = Vector3.zero;
        portalGo.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        var portalSr = portalGo.AddComponent<SpriteRenderer>();
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            var playerSr = playerGo.GetComponent<SpriteRenderer>();
            if (playerSr != null)
            {
                portalSr.sprite = playerSr.sprite;
            }
        }
        portalSr.color = new Color(0.6f, 0.2f, 1.0f, 0.8f); // Neon violet color
        portalSr.sortingOrder = 5;

        portalGo.AddComponent<Portal>();

        var col = portalGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
    }

    private List<UpgradeCard> GetUpgradeCards()
    {
        var cards = new List<UpgradeCard>();
        
        var playerGo = GameObject.FindWithTag("Player");
        Player player = playerGo != null ? playerGo.GetComponent<Player>() : null;

        // Draft cards as requested, marked with [DRAFT] tag
        cards.Add(new UpgradeCard(
            "[DRAFT] SWIFT BLADE", 
            "+20% LAUNCH POWER\n\nSlices faster and with greater velocity.", 
            () => {
                if (player != null) player.UpgradeWeaponPower(0.20f);
            }
        ));

        cards.Add(new UpgradeCard(
            "[DRAFT] CALM MIND", 
            "HALVES AIM TIME DRAIN\n\nAiming slow-mo drains remaining lifetime 50% slower.", 
            () => {
                aimingDrainMultiplierModifier *= 0.5f;
            }
        ));

        cards.Add(new UpgradeCard(
            "[DRAFT] EXTENDED REACH", 
            "+15% DRAG RANGE\n\nAllows for longer aiming drag slingshots.", 
            () => {
                if (player != null) player.UpgradeWeaponDrag(0.15f);
            }
        ));

        cards.Add(new UpgradeCard(
            "[DRAFT] SOUL HARVEST", 
            "+1S TIME ON KILLS\n\nIncreases the lifetime gained from clearing enemies.", 
            () => {
                killTimeBonusAddition += 1.0f;
            }
        ));

        cards.Add(new UpgradeCard(
            "[DRAFT] REJUVENATION", 
            "+20S LIFE RECOVERY\n\nInstantly restores 20 seconds of lifetime.", 
            () => {
                AddPlayerTime(20f);
            }
        ));

        return cards;
    }

    private List<UpgradeCard> GetRandomCards(int count)
    {
        var allCards = GetUpgradeCards();
        var selected = new List<UpgradeCard>();
        while (selected.Count < count && allCards.Count > 0)
        {
            int idx = Random.Range(0, allCards.Count);
            selected.Add(allCards[idx]);
            allCards.RemoveAt(idx);
        }
        return selected;
    }

    private void CreateDraftingPanel()
    {
        if (uiCanvas == null) return;

        // Pause game actions
        Time.timeScale = 0f;

        // Container panel
        draftingPanelGo = new GameObject("DraftingPanel");
        draftingPanelGo.transform.SetParent(uiCanvas.transform, false);
        
        var rect = draftingPanelGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var img = draftingPanelGo.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.08f, 0.85f); // Semi-translucent dark background

        // Central card selection container
        var boxGo = new GameObject("DraftingBox");
        boxGo.transform.SetParent(draftingPanelGo.transform, false);
        var boxRect = boxGo.AddComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(650, 400);
        var boxImg = boxGo.AddComponent<Image>();
        boxImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f); // Dark slate card holder

        // Title text
        var titleGo = new GameObject("DraftingTitle");
        titleGo.transform.SetParent(boxGo.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "SELECT AN UPGRADE";
        titleText.fontSize = 26;
        titleText.color = new Color(0.2f, 0.9f, 0.5f); // Neon green accent
        titleText.alignment = TextAlignmentOptions.Center;

        // Styling with QuinqueFive pixel font if found
        TMP_FontAsset pixelFont = null;
        var comboGo = uiCanvas.transform.Find("Combo")?.gameObject;
        if (comboGo != null)
        {
            var comboTextComp = comboGo.GetComponent<TextMeshProUGUI>();
            if (comboTextComp != null) pixelFont = comboTextComp.font;
        }
        if (pixelFont != null) titleText.font = pixelFont;

        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(600, 50);
        titleRect.anchoredPosition = new Vector2(0, -30);

        // Get 3 random unique cards
        List<UpgradeCard> chosenCards = GetRandomCards(3);

        float startX = -200f;
        float spacingX = 200f;

        for (int i = 0; i < chosenCards.Count; i++)
        {
            var card = chosenCards[i];

            // Card Panel Button
            var cardBtnGo = new GameObject($"CardButton_{i}");
            cardBtnGo.transform.SetParent(boxGo.transform, false);
            
            var cardBtnRect = cardBtnGo.AddComponent<RectTransform>();
            cardBtnRect.sizeDelta = new Vector2(175, 250);
            cardBtnRect.anchoredPosition = new Vector2(startX + i * spacingX, -30);

            var cardImg = cardBtnGo.AddComponent<Image>();
            cardImg.color = new Color(0.18f, 0.18f, 0.24f, 1f);

            var btn = cardBtnGo.AddComponent<Button>();
            
            // Neon tint transition
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.18f, 0.18f, 0.24f, 1f);
            cb.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
            cb.pressedColor = new Color(0.12f, 0.12f, 0.18f, 1f);
            btn.colors = cb;

            btn.onClick.AddListener(() => {
                card.ApplyUpgrade();
                
                // Resume game timescale
                Time.timeScale = 1f;
                Destroy(draftingPanelGo);
                
                SpawnTimeText("UPGRADE APPLIED!", new Color(0.2f, 0.9f, 0.5f));
            });

            // Card Title text
            var cardTitleGo = new GameObject("CardTitle");
            cardTitleGo.transform.SetParent(cardBtnGo.transform, false);
            var cardTitleText = cardTitleGo.AddComponent<TextMeshProUGUI>();
            cardTitleText.text = card.Name;
            cardTitleText.fontSize = 11;
            cardTitleText.color = Color.white;
            cardTitleText.alignment = TextAlignmentOptions.Center;
            if (pixelFont != null) cardTitleText.font = pixelFont;

            var ctRect = cardTitleGo.AddComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0.5f, 1f);
            ctRect.anchorMax = new Vector2(0.5f, 1f);
            ctRect.pivot = new Vector2(0.5f, 1f);
            ctRect.sizeDelta = new Vector2(160, 45);
            ctRect.anchoredPosition = new Vector2(0, -15);

            // Card Description text
            var cardDescGo = new GameObject("CardDesc");
            cardDescGo.transform.SetParent(cardBtnGo.transform, false);
            var cardDescText = cardDescGo.AddComponent<TextMeshProUGUI>();
            cardDescText.text = card.Description;
            cardDescText.fontSize = 9;
            cardDescText.color = new Color(0.8f, 0.8f, 0.8f);
            cardDescText.alignment = TextAlignmentOptions.Center;
            if (pixelFont != null) cardDescText.font = pixelFont;

            var cdRect = cardDescGo.AddComponent<RectTransform>();
            cdRect.anchorMin = new Vector2(0.5f, 0.5f);
            cdRect.anchorMax = new Vector2(0.5f, 0.5f);
            cdRect.pivot = new Vector2(0.5f, 0.5f);
            cdRect.sizeDelta = new Vector2(160, 140);
            cdRect.anchoredPosition = new Vector2(0, -25);
        }
    }

    public void AdvanceToNextFloor()
    {
        currentFloor++;
        isFloorCleared = false;

        // Reset survival timer to default
        LevelSurvivalTime = initialLevelSurvivalTime;

        // Reset player positions and clear velocities
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            playerGo.transform.position = Vector3.zero;
            var playerRb = playerGo.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
            }

            // Put Player back to Idle state
            var player = playerGo.GetComponent<Player>();
            if (player != null && player.StateMachine != null)
            {
                player.StateMachine.ChangeState(player.IdleState);
            }
        }

        // Clean up remaining slow zones or hazards
        var slowZones = FindObjectsOfType<SlowZone>();
        foreach (var sz in slowZones)
        {
            Destroy(sz.gameObject);
        }

        // Display Floor Announcement above player
        SpawnTimeText($"FLOOR {currentFloor}", new Color(0.2f, 0.9f, 0.5f));

        // Start Wave 1 on the new floor
        StartWave(1);
    }
}

public class UpgradeCard
{
    public string Name;
    public string Description;
    public System.Action ApplyUpgrade;

    public UpgradeCard(string name, string description, System.Action applyUpgrade)
    {
        Name = name;
        Description = description;
        ApplyUpgrade = applyUpgrade;
    }
}
