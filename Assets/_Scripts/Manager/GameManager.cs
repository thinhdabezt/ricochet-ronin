using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Assets._Scripts.Manager;
using DG.Tweening;

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

    // Upgrade modifiers
    public float TimeHarvesterBonusPercent { get; set; } = 0f;
    public int KineticMomentumBonusDamage { get; set; } = 0;
    public bool IsGlassBladeActive { get; set; } = false;
    public bool IsSatanicHourglassActive { get; set; } = false;

    private int activeMutationsCount = 0;
    private List<UpgradeCardSO> allUpgradeCards;

    private int killsInCurrentDash = 0;
    private Transform enemiesContainer;
    private Canvas uiCanvas;
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

        InitializeCards(); // Populates allUpgradeCards programmatically

        // Find UI components
        var uiGo = GameObject.Find("UI") ?? GameObject.Find("Canvas");
        if (uiGo != null) uiCanvas = uiGo.GetComponent<Canvas>();
        if (uiCanvas == null) uiCanvas = FindFirstObjectByType<Canvas>();

        enemiesContainer = GameObject.Find("Enemies")?.transform;
        if (enemiesContainer == null)
        {
            enemiesContainer = new GameObject("Enemies").transform;
        }

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
            if (UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame)
            {
                ReturnToMainMenu();
            }
            return;
        }

        // Determine player aiming state to apply triple drain speed (influenced by upgrades)
        float drainMultiplier = 1f;
        bool playerAiming = false;
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            var player = playerGo.GetComponent<Player>();
            if (player != null && player.StateMachine != null && player.StateMachine.CurrentState is PlayerAimingState)
            {
                drainMultiplier = 3f * aimingDrainMultiplierModifier;
                playerAiming = true;
            }
        }

        // Run timers
        float idleDrainRate = IsSatanicHourglassActive ? 2f : 1f;
        PlayerLifeTime -= Time.deltaTime * (playerAiming ? drainMultiplier : idleDrainRate);
        
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

        // Find player to calculate spawn proximity safety check
        GameObject playerGo = GameObject.FindWithTag("Player");
        Vector3 spawnPos = Vector3.zero;
        int attempts = 0;
        bool validPosFound = false;

        while (!validPosFound && attempts < 30)
        {
            spawnPos = new Vector3(Random.Range(-11f, 11f), Random.Range(-5f, 5f), 0f);
            attempts++;

            // Ensure not spawning inside a Wall collider (Layer 6)
            int wallLayerMask = 1 << LayerMask.NameToLayer("Wall");
            if (Physics2D.OverlapCircle(spawnPos, 0.4f, wallLayerMask) != null)
            {
                continue;
            }

            if (playerGo != null)
            {
                if (Vector3.Distance(spawnPos, playerGo.transform.position) >= 3.0f)
                {
                    validPosFound = true;
                }
            }
            else
            {
                validPosFound = true;
            }
        }

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
            scaledData.scoreValue = Mathf.RoundToInt(scaledData.scoreValue * difficultyMultiplier);

            // Capped health scaling: max +1 health on Floor 3-5, max +2 health on Floor 6+
            int healthBonus = 0;
            if (currentFloor >= 6)
            {
                healthBonus = 2;
            }
            else if (currentFloor >= 3)
            {
                healthBonus = 1;
            }
            scaledData.maxHealth += healthBonus;

            // Action cooldown scaling: Cooldown Mới = Cooldown Gốc * [1 - (F - 1) * 0.05] (max 50% reduction)
            float cooldownFactor = 1f - (currentFloor - 1) * 0.05f;
            cooldownFactor = Mathf.Max(0.5f, cooldownFactor);
            scaledData.actionCooldown *= cooldownFactor;

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

        // Satanic Hourglass corrupted card effect: Freeze all other active enemies for 1s
        if (IsSatanicHourglassActive)
        {
            FreezeAllEnemies(1.0f);
        }

        if (EnemiesRemaining <= 0)
        {
            StartCoroutine(NextWaveRoutine());
        }
    }

    public void FreezeAllEnemies(float duration)
    {
        var activeEnemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.Freeze(duration);
            }
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

    public void PenalizePlayerTimeSilent(float amount)
    {
        if (IsGameOver || IsVictory) return;
        PlayerLifeTime = Mathf.Max(0f, PlayerLifeTime - amount);
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
        CreateEndGameScreen("TIME LOOP FAILED", new Color(1f, 0.15f, 0.25f), "PRESS R OR CLICK TO RETRY");
    }

    private void TriggerVictory()
    {
        IsVictory = true;
        Time.timeScale = 0.5f;
        CreateEndGameScreen("VICTORY", new Color(0.2f, 0.9f, 0.5f), "PRESS R OR CLICK TO ADVENTURE AGAIN");
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
        img.color = new Color(0.02f, 0.02f, 0.05f, 0.92f); // Cinematic dark blue-black overlay

        var cg = endGamePanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.DOFade(1f, 0.4f).SetUpdate(true);

        // Load the QuinqueFive pixel font from the HUD
        TMP_FontAsset pixelFont = null;
        var scoreGo = GameObject.Find("UI/Score") ?? GameObject.Find("Score");
        if (scoreGo != null)
        {
            var tmp = scoreGo.GetComponent<TextMeshProUGUI>();
            if (tmp != null) pixelFont = tmp.font;
        }

        // Title text (e.g. DISHONORED or VICTORY)
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(endGamePanel.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 46;
        titleText.color = titleColor;
        titleText.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) titleText.font = pixelFont;

        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(1200, 150);
        titleRect.anchoredPosition = new Vector2(0, 100);

        // Slam down animation for the title
        titleGo.transform.localScale = new Vector3(3f, 3f, 3f);
        titleText.alpha = 0f;
        titleText.DOFade(1f, 0.25f).SetUpdate(true);
        titleGo.transform.DOScale(1f, 0.55f)
            .SetEase(Ease.OutBounce)
            .SetUpdate(true);

        // Subtitle text (e.g. PRESS R TO RETRY / CLICK TO RESTART)
        var subGo = new GameObject("Subtitle");
        subGo.transform.SetParent(endGamePanel.transform, false);
        var subText = subGo.AddComponent<TextMeshProUGUI>();
        subText.text = subtitleText;
        subText.fontSize = 14;
        subText.color = new Color(0.7f, 0.7f, 0.8f);
        subText.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) subText.font = pixelFont;

        var subRect = subGo.GetComponent<RectTransform>();
        subRect.sizeDelta = new Vector2(1200, 100);
        subRect.anchoredPosition = new Vector2(0, 15);

        // Subtle pulsing animation for subtitle after delay
        subText.alpha = 0f;
        DOTween.Sequence()
            .AppendInterval(0.4f)
            .Append(subText.DOFade(1f, 0.3f))
            .AppendCallback(() => {
                subGo.transform.DOScale(1.08f, 0.8f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true);
            })
            .SetUpdate(true);

        // Retry Button
        var retryBtn = new GameObject("RetryButton");
        retryBtn.transform.SetParent(endGamePanel.transform, false);
        var rRect = retryBtn.AddComponent<RectTransform>();
        rRect.sizeDelta = new Vector2(240, 50);
        rRect.anchoredPosition = new Vector2(-140, -70);
        var rImg = retryBtn.AddComponent<Image>();
        rImg.color = new Color(0.07f, 0.07f, 0.1f, 0.9f);
        var rBtn = retryBtn.AddComponent<Button>();
        rBtn.targetGraphic = rImg;
        ColorBlock rColors = rBtn.colors;
        rColors.normalColor = new Color(0.07f, 0.07f, 0.1f, 0.9f);
        rColors.highlightedColor = new Color(0f, 0.6f, 0.8f, 1f); // Neon Cyan
        rColors.pressedColor = new Color(0f, 0.4f, 0.6f, 1f);
        rColors.selectedColor = rColors.normalColor;
        rBtn.colors = rColors;
        rBtn.onClick.AddListener(RestartGame);

        var rTextGo = new GameObject("Text");
        rTextGo.transform.SetParent(retryBtn.transform, false);
        var rText = rTextGo.AddComponent<TextMeshProUGUI>();
        rText.text = "RETRY (R)";
        rText.fontSize = 12;
        rText.color = Color.white;
        rText.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) rText.font = pixelFont;
        var rTextRect = rTextGo.GetComponent<RectTransform>();
        rTextRect.anchorMin = Vector2.zero;
        rTextRect.anchorMax = Vector2.one;
        rTextRect.sizeDelta = Vector2.zero;

        // Main Menu Button
        var menuBtn = new GameObject("MenuButton");
        menuBtn.transform.SetParent(endGamePanel.transform, false);
        var mRect = menuBtn.AddComponent<RectTransform>();
        mRect.sizeDelta = new Vector2(240, 50);
        mRect.anchoredPosition = new Vector2(140, -70);
        var mImg = menuBtn.AddComponent<Image>();
        mImg.color = new Color(0.07f, 0.07f, 0.1f, 0.9f);
        var mBtn = menuBtn.AddComponent<Button>();
        mBtn.targetGraphic = mImg;
        ColorBlock mColors = mBtn.colors;
        mColors.normalColor = new Color(0.07f, 0.07f, 0.1f, 0.9f);
        mColors.highlightedColor = new Color(0.5f, 0.2f, 0.6f, 1f); // Neon Purple
        mColors.pressedColor = new Color(0.35f, 0.15f, 0.45f, 1f);
        mColors.selectedColor = mColors.normalColor;
        mBtn.colors = mColors;
        mBtn.onClick.AddListener(ReturnToMainMenu);

        var mTextGo = new GameObject("Text");
        mTextGo.transform.SetParent(menuBtn.transform, false);
        var mText = mTextGo.AddComponent<TextMeshProUGUI>();
        mText.text = "MENU (M)";
        mText.fontSize = 12;
        mText.color = Color.white;
        mText.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) mText.font = pixelFont;
        var mTextRect = mTextGo.GetComponent<RectTransform>();
        mTextRect.anchorMin = Vector2.zero;
        mTextRect.anchorMax = Vector2.one;
        mTextRect.sizeDelta = Vector2.zero;
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

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void TriggerFloorClear()
    {
        isFloorCleared = true;

        // Clean up remaining entities in the level (like slow zones)
        var slowZones = FindObjectsByType<SlowZone>(FindObjectsSortMode.None);
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

    private void InitializeCards()
    {
        allUpgradeCards = new List<UpgradeCardSO>
        {
            ScriptableObject.CreateInstance<DoubleDashCard>(),
            ScriptableObject.CreateInstance<VacuumBladeCard>(),
            ScriptableObject.CreateInstance<TrailOfFireCard>(),
            ScriptableObject.CreateInstance<TimeHarvesterCard>(),
            ScriptableObject.CreateInstance<AdrenalineRushCard>(),
            ScriptableObject.CreateInstance<KineticMomentumCard>(),
            ScriptableObject.CreateInstance<GlassBladeCard>(),
            ScriptableObject.CreateInstance<SatanicHourglassCard>()
        };

        // Link with IndexEntryData dynamically at runtime
        foreach (var card in allUpgradeCards)
        {
            if (IndexManager.Instance != null)
            {
                card.indexData = IndexManager.Instance.AllEntries.Find(e => e.EntryName == card.cardName);
            }
        }
    }

    private List<UpgradeCardSO> GetRandomCards(int count)
    {
        var pool = new List<UpgradeCardSO>();
        foreach (var card in allUpgradeCards)
        {
            if (card == null) continue;
            // Mutation cards are limited to 1 active mutation
            if (card.cardType == CardType.Mutation && activeMutationsCount >= 1)
            {
                continue;
            }
            pool.Add(card);
        }

        var selected = new List<UpgradeCardSO>();
        while (selected.Count < count && pool.Count > 0)
        {
            int idx = Random.Range(0, pool.Count);
            selected.Add(pool[idx]);
            pool.RemoveAt(idx);
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

        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(600, 50);
        titleRect.anchoredPosition = new Vector2(0, -30);

        // Get 3 random unique cards
        List<UpgradeCardSO> chosenCards = GetRandomCards(3);

        float startX = -200f;
        float spacingX = 200f;

        var playerGo = GameObject.FindWithTag("Player");
        Player player = playerGo != null ? playerGo.GetComponent<Player>() : null;

        for (int i = 0; i < chosenCards.Count; i++)
        {
            var card = chosenCards[i];

            // Card Panel Button - Outer border container
            var cardBtnGo = new GameObject($"CardButton_{i}");
            cardBtnGo.transform.SetParent(boxGo.transform, false);
            
            var cardBtnRect = cardBtnGo.AddComponent<RectTransform>();
            cardBtnRect.sizeDelta = new Vector2(175, 250);
            cardBtnRect.anchoredPosition = new Vector2(startX + i * spacingX, -30);

            var borderImg = cardBtnGo.AddComponent<Image>();

            // Setup border colors based on Category type
            Color borderColor = Color.white;
            switch (card.cardType)
            {
                case CardType.Mutation:
                    borderColor = new Color(1.0f, 0.84f, 0.0f); // Gold/Yellow
                    break;
                case CardType.Passive:
                    borderColor = new Color(0.0f, 0.95f, 1.0f); // Cyan
                    break;
                case CardType.Corrupted:
                    borderColor = new Color(1.0f, 0.0f, 0.33f); // Crimson Red
                    break;
            }
            borderImg.color = borderColor;

            var btn = cardBtnGo.AddComponent<Button>();
            
            // Inner Card panel
            var innerGo = new GameObject("InnerPanel");
            innerGo.transform.SetParent(cardBtnGo.transform, false);
            var innerRect = innerGo.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.pivot = new Vector2(0.5f, 0.5f);
            innerRect.sizeDelta = new Vector2(-8, -8); // 4-pixel border thickness

            var innerImg = innerGo.AddComponent<Image>();
            innerImg.color = new Color(0.18f, 0.18f, 0.24f, 1f);

            btn.targetGraphic = innerImg;

            // Hover/Press transitions
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.18f, 0.18f, 0.24f, 1f);
            cb.highlightedColor = new Color(0.24f, 0.24f, 0.32f, 1f);
            cb.pressedColor = new Color(0.12f, 0.12f, 0.18f, 1f);
            btn.colors = cb;

            btn.onClick.AddListener(() => {
                if (player != null)
                {
                    card.ApplyUpgrade(player);
                    if (card.indexData != null && IndexManager.Instance != null)
                    {
                        IndexManager.Instance.UnlockEntry(card.indexData.EntryID);
                    }
                }

                if (card.cardType == CardType.Mutation)
                {
                    activeMutationsCount++;
                }
                
                // Resume game timescale
                Time.timeScale = 1f;
                Destroy(draftingPanelGo);
                
                SpawnTimeText("UPGRADE APPLIED!", new Color(0.2f, 0.9f, 0.5f));
            });

            // Card Title text
            var cardTitleGo = new GameObject("CardTitle");
            cardTitleGo.transform.SetParent(innerGo.transform, false);
            var cardTitleText = cardTitleGo.AddComponent<TextMeshProUGUI>();
            cardTitleText.text = card.cardName;
            cardTitleText.fontSize = 11;
            cardTitleText.color = Color.white;
            cardTitleText.alignment = TextAlignmentOptions.Center;
            if (pixelFont != null) cardTitleText.font = pixelFont;

            var ctRect = cardTitleGo.GetComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0.5f, 1f);
            ctRect.anchorMax = new Vector2(0.5f, 1f);
            ctRect.pivot = new Vector2(0.5f, 1f);
            ctRect.sizeDelta = new Vector2(160, 45);
            ctRect.anchoredPosition = new Vector2(0, -15);

            // Card Description text
            var cardDescGo = new GameObject("CardDesc");
            cardDescGo.transform.SetParent(innerGo.transform, false);
            var cardDescText = cardDescGo.AddComponent<TextMeshProUGUI>();
            cardDescText.text = card.cardDescription;
            cardDescText.fontSize = 9;
            cardDescText.color = new Color(0.8f, 0.8f, 0.8f);
            cardDescText.alignment = TextAlignmentOptions.Center;
            if (pixelFont != null) cardDescText.font = pixelFont;

            var cdRect = cardDescGo.GetComponent<RectTransform>();
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
        var slowZones = FindObjectsByType<SlowZone>(FindObjectsSortMode.None);
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
