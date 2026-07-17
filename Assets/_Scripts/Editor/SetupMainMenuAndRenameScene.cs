#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class SetupMainMenuAndRenameScene : EditorWindow
{
    [MenuItem("Tools/Ricochet Ronin/Setup Main Menu")]
    public static void Execute()
    {
        Debug.Log("Starting Scene and Build Settings configuration...");

        // 1. Ensure _Scenes folder exists
        if (!AssetDatabase.IsValidFolder("Assets/_Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "_Scenes");
        }

        // 2. Rename and move active/sample scene to GameplayScene.unity
        string sampleScenePath = "Assets/SampleScene.unity";
        string gameplayScenePath = "Assets/_Scenes/GameplayScene.unity";

        // Open GameplayScene if it already exists, or rename active one
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(gameplayScenePath) == null)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(sampleScenePath) != null)
            {
                // Close current scene if it's the one we're moving (to avoid lock issues)
                EditorSceneManager.SaveOpenScenes();
                string moveErr = AssetDatabase.MoveAsset(sampleScenePath, gameplayScenePath);
                if (!string.IsNullOrEmpty(moveErr))
                {
                    Debug.LogError("Failed to move active scene to _Scenes folder: " + moveErr);
                }
                else
                {
                    Debug.Log("Successfully moved SampleScene.unity to Assets/_Scenes/GameplayScene.unity");
                }
            }
        }

        // Cleanup old SampleScene inside _Scenes if it exists
        string oldSampleScenePath = "Assets/_Scenes/SampleScene.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(oldSampleScenePath) != null)
        {
            AssetDatabase.DeleteAsset(oldSampleScenePath);
        }

        // 3. Create MainMenuScene
        string menuScenePath = "Assets/_Scenes/MainMenuScene.unity";
        Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Configure Camera
        GameObject cameraGo = GameObject.Find("Main Camera");
        if (cameraGo != null)
        {
            Camera cam = cameraGo.GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f); // Slate dark neon background
                cam.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        // Destroy 3D Directional Light
        GameObject lightGo = GameObject.Find("Directional Light");
        if (lightGo != null)
        {
            DestroyImmediate(lightGo);
        }

        // 4. Create UI Hierarchy
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();

        // Background Panel
        GameObject panelGo = new GameObject("BackgroundPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        Image panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

        // MainMenu Controller
        MainMenu mainMenu = canvasGo.AddComponent<MainMenu>();

        // Load QuinqueFive SDF Font
        TMP_FontAsset pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Sprites/Fonts/QuinqueFive SDF.asset");

        // Title Text
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(canvasGo.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.75f);
        titleRect.anchorMax = new Vector2(0.5f, 0.75f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600, 100);
        titleRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "RICOCHET RONIN";
        titleText.fontSize = 32;
        titleText.color = new Color(0f, 0.95f, 1f); // Neon Cyan
        titleText.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) titleText.font = pixelFont;

        // Button Hover Transitions Setup
        ColorBlock cb = new ColorBlock
        {
            normalColor = new Color(0.12f, 0.12f, 0.16f, 1f),
            highlightedColor = new Color(0f, 0.95f, 1f, 1f), // Glowing neon cyan hover
            pressedColor = new Color(0f, 0.7f, 0.8f, 1f),
            selectedColor = new Color(0.12f, 0.12f, 0.16f, 1f),
            disabledColor = Color.gray,
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        // Play Button
        GameObject playGo = new GameObject("PlayButton");
        playGo.transform.SetParent(canvasGo.transform, false);
        RectTransform playRect = playGo.AddComponent<RectTransform>();
        playRect.anchorMin = new Vector2(0.5f, 0.45f);
        playRect.anchorMax = new Vector2(0.5f, 0.45f);
        playRect.pivot = new Vector2(0.5f, 0.5f);
        playRect.sizeDelta = new Vector2(250, 60);
        playRect.anchoredPosition = Vector2.zero;

        Image playImg = playGo.AddComponent<Image>();
        playImg.color = cb.normalColor;

        Button playBtn = playGo.AddComponent<Button>();
        playBtn.targetGraphic = playImg;
        playBtn.colors = cb;
        UnityEventTools.AddPersistentListener(playBtn.onClick, mainMenu.PlayGame);

        GameObject playTextGo = new GameObject("Text");
        playTextGo.transform.SetParent(playGo.transform, false);
        RectTransform playTextRect = playTextGo.AddComponent<RectTransform>();
        playTextRect.anchorMin = Vector2.zero;
        playTextRect.anchorMax = Vector2.one;
        playTextRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI playTextText = playTextGo.AddComponent<TextMeshProUGUI>();
        playTextText.text = "PLAY";
        playTextText.fontSize = 16;
        playTextText.color = Color.white;
        playTextText.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) playTextText.font = pixelFont;

        // Exit Button
        GameObject exitGo = new GameObject("ExitButton");
        exitGo.transform.SetParent(canvasGo.transform, false);
        RectTransform exitRect = exitGo.AddComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(0.5f, 0.3f);
        exitRect.anchorMax = new Vector2(0.5f, 0.3f);
        exitRect.pivot = new Vector2(0.5f, 0.5f);
        exitRect.sizeDelta = new Vector2(250, 60);
        exitRect.anchoredPosition = Vector2.zero;

        Image exitImg = exitGo.AddComponent<Image>();
        exitImg.color = cb.normalColor;

        Button exitBtn = exitGo.AddComponent<Button>();
        exitBtn.targetGraphic = exitImg;
        exitBtn.colors = cb;
        UnityEventTools.AddPersistentListener(exitBtn.onClick, mainMenu.ExitGame);

        GameObject exitTextGo = new GameObject("Text");
        exitTextGo.transform.SetParent(exitGo.transform, false);
        RectTransform exitTextRect = exitTextGo.AddComponent<RectTransform>();
        exitTextRect.anchorMin = Vector2.zero;
        exitTextRect.anchorMax = Vector2.one;
        exitTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI exitTextText = exitTextGo.AddComponent<TextMeshProUGUI>();
        exitTextText.text = "EXIT";
        exitTextText.fontSize = 16;
        exitTextText.color = Color.white;
        exitTextText.alignment = TextAlignmentOptions.Center;
        if (pixelFont != null) exitTextText.font = pixelFont;

        // Save MainMenuScene
        EditorSceneManager.SaveScene(menuScene, menuScenePath);
        Debug.Log("MainMenuScene created and saved at: " + menuScenePath);

        // 5. Update Build Settings
        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[2];
        newScenes[0] = new EditorBuildSettingsScene(menuScenePath, true);
        newScenes[1] = new EditorBuildSettingsScene(gameplayScenePath, true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log("Build Settings updated successfully: Index 0 = MainMenuScene, Index 1 = GameplayScene");

        // 6. Open the newly created MainMenuScene
        EditorSceneManager.OpenScene(menuScenePath);
    }
}
#endif
