#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class SetupIndexSystem : EditorWindow
{
    [MenuItem("Tools/Ricochet Ronin/Setup Index System")]
    public static void Execute()
    {
        Debug.Log("Starting Index System Setup...");

        // 1. Create folders
        string indexFolder = "Assets/ScriptableObjects/Index";
        if (!AssetDatabase.IsValidFolder(indexFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Index");
        }

        // 2. Load QuinqueFive SDF Font & default Sprites
        TMP_FontAsset pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Sprites/Fonts/QuinqueFive SDF.asset");
        Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Lock.png"); // Try loading if exists
        if (lockSprite == null) lockSprite = defaultSprite;

        List<IndexEntryData> allEntriesList = new List<IndexEntryData>();

        // 3. Scan & Populate Enemy Entries
        string[] enemyGUIDs = AssetDatabase.FindAssets("t:EnemyDataSO");
        foreach (string guid in enemyGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyDataSO enemy = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(path);
            if (enemy != null)
            {
                string assetName = "index_enemy_" + enemy.enemyName.Replace(" ", "_");
                string assetPath = $"{indexFolder}/{assetName}.asset";
                
                IndexEntryData entry = AssetDatabase.LoadAssetAtPath<IndexEntryData>(assetPath);
                if (entry == null)
                {
                    entry = ScriptableObject.CreateInstance<IndexEntryData>();
                    entry.Initialize(
                        "monster_" + enemy.enemyName.ToLower().Replace(" ", "_"), 
                        enemy.enemyName, 
                        IndexType.Monster, 
                        $"Max Health: {enemy.maxHealth}\nMovement: {enemy.movementType}\nSpecial: {enemy.specialMechanic}", 
                        "An ancient creature of shadow, returning to test your steel."
                    );
                    
                    AssetDatabase.CreateAsset(entry, assetPath);
                }
                
                // Link
                enemy.indexData = entry;
                EditorUtility.SetDirty(enemy);
                allEntriesList.Add(entry);
            }
        }

        // 4. Create Card Entries programmatically (since cards are instantiated in-memory)
        var cardDataList = new List<(string id, string name, string desc, string lore)>
        {
            ("upgrade_double_dash", "Double Dash", "Allows redirecting mid-dash by left-clicking. Costs 5s of lifetime per redirection.", "A swift redirection of blade and soul."),
            ("upgrade_adrenaline_rush", "Adrenaline Rush", "Kills immediately restore 4 seconds of lifetime.", "Blood pumps, time slows, the blade strikes again."),
            ("upgrade_glass_blade", "Glass Blade", "Increases dash damage by +2 but cuts time penalty in half on hit (penalty becomes +25s instead of +10s).", "A fragile edge, sharp enough to cut time itself."),
            ("upgrade_kinetic_momentum", "Kinetic Momentum", "Dash damage increases by +1 per bounce off walls/obstacles during the current dash.", "Speed translates directly into lethality."),
            ("upgrade_time_harvester", "Time Harvester", "Increases time bonus on kill by +15% per bounce off walls/obstacles.", "Harvesting the essence of the fallen."),
            ("upgrade_trail_of_fire", "Trail Of Fire", "Leaves a blazing trail that damages enemies passing through.", "Sparks ignite, leaving a burning path of destruction."),
            ("upgrade_vacuum_blade", "Vacuum Blade", "Pulls nearby enemies closer when dashing.", "An invisible vortex pulling foes into the blade."),
            ("upgrade_satanic_hourglass", "Satanic Hourglass", "Time flows 3x faster when aiming, but enemies are slowed down by 95%.", "A dark pact that accelerates time for the ultimate strike.")
        };

        foreach (var cardData in cardDataList)
        {
            string assetName = "index_upgrade_" + cardData.name.Replace(" ", "_");
            string assetPath = $"{indexFolder}/{assetName}.asset";
            
            IndexEntryData entry = AssetDatabase.LoadAssetAtPath<IndexEntryData>(assetPath);
            if (entry == null)
            {
                entry = ScriptableObject.CreateInstance<IndexEntryData>();
                entry.Initialize(cardData.id, cardData.name, IndexType.Upgrade, cardData.desc, cardData.lore);
                AssetDatabase.CreateAsset(entry, assetPath);
            }
            allEntriesList.Add(entry);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 5. Create IndexScene
        string indexScenePath = "Assets/_Scenes/IndexScene.unity";
        Scene indexScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Configure Camera
        GameObject cameraGo = GameObject.Find("Main Camera");
        if (cameraGo != null)
        {
            Camera cam = cameraGo.GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f); // Slate dark background
                cam.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        // Destroy 3D Directional Light
        GameObject lightGo = GameObject.Find("Directional Light");
        if (lightGo != null) DestroyImmediate(lightGo);

        // Setup persistent IndexManager in scene
        GameObject indexMgrGo = new GameObject("IndexManager");
        IndexManager indexMgr = indexMgrGo.AddComponent<IndexManager>();
        indexMgr.SetAllEntries(allEntriesList);

        // 6. UI Creation
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Background Panel
        GameObject panelGo = new GameObject("BackgroundPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        Image panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

        // Title Text
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(canvasGo.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.85f);
        titleRect.anchorMax = new Vector2(0.5f, 0.85f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(800, 80);
        titleRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "ENCOUNTERS & PERKS";
        titleText.fontSize = 28;
        titleText.color = new Color(0f, 0.95f, 1f); // Neon Cyan
        titleText.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) titleText.font = pixelFont;

        // UI Setup: ScrollRect (left side)
        GameObject scrollGo = new GameObject("ScrollRect");
        scrollGo.transform.SetParent(canvasGo.transform, false);
        RectTransform scrollRectTransform = scrollGo.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.05f, 0.15f);
        scrollRectTransform.anchorMax = new Vector2(0.55f, 0.75f);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.sizeDelta = Vector2.zero;
        
        ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.4f);
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        
        // Viewport
        GameObject viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        RectTransform viewportRect = viewportGo.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportGo.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewportRect;
        
        // Content
        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        RectTransform contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 300f);
        
        VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.spacing = 20f;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        
        ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        // Encounters Header
        GameObject encountersHeaderGo = new GameObject("EncountersHeader");
        encountersHeaderGo.transform.SetParent(contentGo.transform, false);
        TextMeshProUGUI encountersHeaderTxt = encountersHeaderGo.AddComponent<TextMeshProUGUI>();
        encountersHeaderTxt.text = "— ENCOUNTERS —";
        encountersHeaderTxt.fontSize = 11;
        encountersHeaderTxt.color = new Color(1f, 0.3f, 0.3f); // Neon Red
        encountersHeaderTxt.alignment = TextAlignmentOptions.Left;
        if (pixelFont != null) encountersHeaderTxt.font = pixelFont;

        // Encounters Grid
        GameObject encountersGridGo = new GameObject("EncountersGrid");
        encountersGridGo.transform.SetParent(contentGo.transform, false);
        GridLayoutGroup encountersGrid = encountersGridGo.AddComponent<GridLayoutGroup>();
        encountersGrid.cellSize = new Vector2(90, 90);
        encountersGrid.spacing = new Vector2(15, 15);
        encountersGrid.padding = new RectOffset(5, 5, 5, 5);
        encountersGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        encountersGrid.constraintCount = 4;
        ContentSizeFitter encountersFitter = encountersGridGo.AddComponent<ContentSizeFitter>();
        encountersFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Perks Header
        GameObject perksHeaderGo = new GameObject("PerksHeader");
        perksHeaderGo.transform.SetParent(contentGo.transform, false);
        TextMeshProUGUI perksHeaderTxt = perksHeaderGo.AddComponent<TextMeshProUGUI>();
        perksHeaderTxt.text = "— PERKS & MUTATIONS —";
        perksHeaderTxt.fontSize = 11;
        perksHeaderTxt.color = new Color(0.3f, 0.8f, 1f); // Neon Cyan
        perksHeaderTxt.alignment = TextAlignmentOptions.Left;
        if (pixelFont != null) perksHeaderTxt.font = pixelFont;

        // Perks Grid
        GameObject perksGridGo = new GameObject("PerksGrid");
        perksGridGo.transform.SetParent(contentGo.transform, false);
        GridLayoutGroup perksGrid = perksGridGo.AddComponent<GridLayoutGroup>();
        perksGrid.cellSize = new Vector2(90, 90);
        perksGrid.spacing = new Vector2(15, 15);
        perksGrid.padding = new RectOffset(5, 5, 5, 5);
        perksGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        perksGrid.constraintCount = 4;
        ContentSizeFitter perksFitter = perksGridGo.AddComponent<ContentSizeFitter>();
        perksFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Template Item
        GameObject itemTemplate = new GameObject("GridItemTemplate");
        itemTemplate.transform.SetParent(viewportGo.transform, false);
        itemTemplate.SetActive(false);
        RectTransform itemRect = itemTemplate.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(100, 100);
        
        Image itemImg = itemTemplate.AddComponent<Image>();
        itemImg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
        
        Button itemBtn = itemTemplate.AddComponent<Button>();
        itemBtn.targetGraphic = itemImg;

        ColorBlock itemCb = new ColorBlock
        {
            normalColor = new Color(0.12f, 0.12f, 0.18f, 1f),
            highlightedColor = new Color(0f, 0.95f, 1f, 1f),
            pressedColor = new Color(0f, 0.7f, 0.8f, 1f),
            selectedColor = new Color(0.12f, 0.12f, 0.18f, 1f),
            disabledColor = Color.gray,
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        itemBtn.colors = itemCb;
        
        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(itemTemplate.transform, false);
        RectTransform iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.15f, 0.35f);
        iconRect.anchorMax = new Vector2(0.85f, 0.9f);
        iconRect.sizeDelta = Vector2.zero;
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.sprite = defaultSprite;
        
        GameObject labelGo = new GameObject("Text");
        labelGo.transform.SetParent(itemTemplate.transform, false);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.05f, 0.05f);
        labelRect.anchorMax = new Vector2(0.95f, 0.3f);
        labelRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI labelTxt = labelGo.AddComponent<TextMeshProUGUI>();
        labelTxt.fontSize = 8;
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.color = Color.white;
        if (pixelFont != null) labelTxt.font = pixelFont;

        // UI Setup: Details Panel (right side)
        GameObject detailsGo = new GameObject("DetailsPanel");
        detailsGo.transform.SetParent(canvasGo.transform, false);
        RectTransform detailsRect = detailsGo.AddComponent<RectTransform>();
        detailsRect.anchorMin = new Vector2(0.6f, 0.15f);
        detailsRect.anchorMax = new Vector2(0.95f, 0.75f);
        detailsRect.pivot = new Vector2(0.5f, 0.5f);
        detailsRect.sizeDelta = Vector2.zero;
        detailsGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.8f);
        
        // Details Icon
        GameObject detIconGo = new GameObject("DetailsIcon");
        detIconGo.transform.SetParent(detailsGo.transform, false);
        RectTransform detIconRect = detIconGo.AddComponent<RectTransform>();
        detIconRect.anchorMin = new Vector2(0.5f, 0.75f);
        detIconRect.anchorMax = new Vector2(0.5f, 0.75f);
        detIconRect.pivot = new Vector2(0.5f, 0.5f);
        detIconRect.sizeDelta = new Vector2(100, 100);
        Image detIcon = detIconGo.AddComponent<Image>();
        detIcon.preserveAspect = true;
        
        // Details Name
        GameObject detNameGo = new GameObject("DetailsName");
        detNameGo.transform.SetParent(detailsGo.transform, false);
        RectTransform detNameRect = detNameGo.AddComponent<RectTransform>();
        detNameRect.anchorMin = new Vector2(0.05f, 0.55f);
        detNameRect.anchorMax = new Vector2(0.95f, 0.65f);
        detNameRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI detName = detNameGo.AddComponent<TextMeshProUGUI>();
        detName.fontSize = 18;
        detName.alignment = TextAlignmentOptions.Center;
        detName.color = new Color(0f, 0.95f, 1f); // Neon Cyan
        if (pixelFont != null) detName.font = pixelFont;
        
        // Details Type
        GameObject detTypeGo = new GameObject("DetailsType");
        detTypeGo.transform.SetParent(detailsGo.transform, false);
        RectTransform detTypeRect = detTypeGo.AddComponent<RectTransform>();
        detTypeRect.anchorMin = new Vector2(0.05f, 0.48f);
        detTypeRect.anchorMax = new Vector2(0.95f, 0.54f);
        detTypeRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI detType = detTypeGo.AddComponent<TextMeshProUGUI>();
        detType.fontSize = 10;
        detType.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) detType.font = pixelFont;
        
        // Details Description
        GameObject detDescGo = new GameObject("DetailsDescription");
        detDescGo.transform.SetParent(detailsGo.transform, false);
        RectTransform detDescRect = detDescGo.AddComponent<RectTransform>();
        detDescRect.anchorMin = new Vector2(0.05f, 0.22f);
        detDescRect.anchorMax = new Vector2(0.95f, 0.45f);
        detDescRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI detDesc = detDescGo.AddComponent<TextMeshProUGUI>();
        detDesc.fontSize = 12;
        detDesc.alignment = TextAlignmentOptions.Center;
        detDesc.color = Color.white;
        if (pixelFont != null) detDesc.font = pixelFont;
        
        // Details Lore
        GameObject detLoreGo = new GameObject("DetailsLore");
        detLoreGo.transform.SetParent(detailsGo.transform, false);
        RectTransform detLoreRect = detLoreGo.AddComponent<RectTransform>();
        detLoreRect.anchorMin = new Vector2(0.05f, 0.05f);
        detLoreRect.anchorMax = new Vector2(0.95f, 0.20f);
        detLoreRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI detLore = detLoreGo.AddComponent<TextMeshProUGUI>();
        detLore.fontSize = 9;
        detLore.alignment = TextAlignmentOptions.Center;
        detLore.color = new Color(0.7f, 0.7f, 0.8f);
        detLore.fontStyle = FontStyles.Italic;
        if (pixelFont != null) detLore.font = pixelFont;

        // Back Button
        GameObject backGo = new GameObject("BackButton");
        backGo.transform.SetParent(canvasGo.transform, false);
        RectTransform backRect = backGo.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.05f, 0.05f);
        backRect.anchorMax = new Vector2(0.05f, 0.05f);
        backRect.pivot = new Vector2(0f, 0f);
        backRect.sizeDelta = new Vector2(180, 50);
        backRect.anchoredPosition = Vector2.zero;

        Image backImg = backGo.AddComponent<Image>();
        backImg.color = itemCb.normalColor;

        Button backBtn = backGo.AddComponent<Button>();
        backBtn.targetGraphic = backImg;
        
        ColorBlock backCb = itemCb;
        backCb.highlightedColor = new Color(1f, 0f, 0.33f, 1f); // Glowing neon crimson hover
        backBtn.colors = backCb;

        GameObject backTextGo = new GameObject("Text");
        backTextGo.transform.SetParent(backGo.transform, false);
        RectTransform backTextRect = backTextGo.AddComponent<RectTransform>();
        backTextRect.anchorMin = Vector2.zero;
        backTextRect.anchorMax = Vector2.one;
        backTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI backTextText = backTextGo.AddComponent<TextMeshProUGUI>();
        backTextText.text = "BACK";
        backTextText.fontSize = 14;
        backTextText.color = Color.white;
        backTextText.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) backTextText.font = pixelFont;

        // UI Controller attachment
        IndexUIController uiController = canvasGo.AddComponent<IndexUIController>();
        
        // Link serializations on uiController
        var encountersGridContainerField = typeof(IndexUIController).GetField("encountersGridContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var perksGridContainerField = typeof(IndexUIController).GetField("perksGridContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var gridItemTemplateField = typeof(IndexUIController).GetField("gridItemTemplate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailsIconField = typeof(IndexUIController).GetField("detailsIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailsNameField = typeof(IndexUIController).GetField("detailsName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailsTypeField = typeof(IndexUIController).GetField("detailsType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailsDescriptionField = typeof(IndexUIController).GetField("detailsDescription", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailsLoreField = typeof(IndexUIController).GetField("detailsLore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var backButtonField = typeof(IndexUIController).GetField("backButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lockSpriteField = typeof(IndexUIController).GetField("lockSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        encountersGridContainerField.SetValue(uiController, encountersGridGo.transform);
        perksGridContainerField.SetValue(uiController, perksGridGo.transform);
        gridItemTemplateField.SetValue(uiController, itemTemplate);
        detailsIconField.SetValue(uiController, detIcon);
        detailsNameField.SetValue(uiController, detName);
        detailsTypeField.SetValue(uiController, detType);
        detailsDescriptionField.SetValue(uiController, detDesc);
        detailsLoreField.SetValue(uiController, detLore);
        backButtonField.SetValue(uiController, backBtn);
        lockSpriteField.SetValue(uiController, defaultSprite);

        // Save IndexScene
        EditorSceneManager.SaveScene(indexScene, indexScenePath);
        Debug.Log("IndexScene created and saved at: " + indexScenePath);

        // 7. Open MainMenuScene and link Index button
        string menuScenePath = "Assets/_Scenes/MainMenuScene.unity";
        Scene menuScene = EditorSceneManager.OpenScene(menuScenePath);

        GameObject menuCanvasGo = GameObject.Find("Canvas");
        if (menuCanvasGo != null)
        {
            MainMenu menuController = menuCanvasGo.GetComponent<MainMenu>();
            if (menuController != null)
            {
                // Reposition existing Play and Exit buttons to fit Index button
                GameObject menuPlayBtnGo = GameObject.Find("Canvas/PlayButton");
                GameObject menuExitBtnGo = GameObject.Find("Canvas/ExitButton");

                if (menuPlayBtnGo != null)
                {
                    menuPlayBtnGo.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.55f);
                    menuPlayBtnGo.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.55f);
                    menuPlayBtnGo.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                }

                if (menuExitBtnGo != null)
                {
                    menuExitBtnGo.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.25f);
                    menuExitBtnGo.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.25f);
                    menuExitBtnGo.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                }

                // Add IndexManager persistent singleton to MainMenuScene as well
                GameObject menuMgrGo = new GameObject("IndexManager");
                IndexManager menuMgrInstance = menuMgrGo.AddComponent<IndexManager>();
                menuMgrInstance.SetAllEntries(allEntriesList);

                // Create INDEX button in MainMenu
                GameObject indexBtnGo = new GameObject("IndexButton");
                indexBtnGo.transform.SetParent(menuCanvasGo.transform, false);
                RectTransform indexBtnRect = indexBtnGo.AddComponent<RectTransform>();
                indexBtnRect.anchorMin = new Vector2(0.5f, 0.40f);
                indexBtnRect.anchorMax = new Vector2(0.5f, 0.40f);
                indexBtnRect.pivot = new Vector2(0.5f, 0.5f);
                indexBtnRect.sizeDelta = new Vector2(250, 60);
                indexBtnRect.anchoredPosition = Vector2.zero;

                Image indexImg = indexBtnGo.AddComponent<Image>();
                ColorBlock cb = menuPlayBtnGo.GetComponent<Button>().colors;
                indexImg.color = cb.normalColor;

                Button indexBtn = indexBtnGo.AddComponent<Button>();
                indexBtn.targetGraphic = indexImg;
                indexBtn.colors = cb;
                UnityEventTools.AddPersistentListener(indexBtn.onClick, menuController.LoadIndex);

                GameObject indexTextGo = new GameObject("Text");
                indexTextGo.transform.SetParent(indexBtnGo.transform, false);
                RectTransform indexTextRect = indexTextGo.AddComponent<RectTransform>();
                indexTextRect.anchorMin = Vector2.zero;
                indexTextRect.anchorMax = Vector2.one;
                indexTextRect.sizeDelta = Vector2.zero;

                TextMeshProUGUI indexTextText = indexTextGo.AddComponent<TextMeshProUGUI>();
                indexTextText.text = "INDEX";
                indexTextText.fontSize = 16;
                indexTextText.color = Color.white;
                indexTextText.alignment = TextAlignmentOptions.Center;
                if (pixelFont != null) indexTextText.font = pixelFont;
            }
        }

        EditorSceneManager.SaveScene(menuScene);
        Debug.Log("MainMenuScene updated and saved with Index button.");

        // 8. Update Build Settings to include all 3 scenes
        string gameplayScenePath = "Assets/_Scenes/GameplayScene.unity";
        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[3];
        newScenes[0] = new EditorBuildSettingsScene(menuScenePath, true);
        newScenes[1] = new EditorBuildSettingsScene(gameplayScenePath, true);
        newScenes[2] = new EditorBuildSettingsScene(indexScenePath, true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log("Build Settings updated successfully: Index 0 = MainMenuScene, Index 1 = GameplayScene, Index 2 = IndexScene");

        // Re-open MainMenuScene so that testing starts correctly
        EditorSceneManager.OpenScene(menuScenePath);
        Debug.Log("Index System Setup Completed Successfully!");
    }
}
#endif
