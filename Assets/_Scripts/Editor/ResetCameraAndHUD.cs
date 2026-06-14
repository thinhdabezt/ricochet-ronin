#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

public class ResetCameraAndHUD : EditorWindow
{
    [MenuItem("Tools/Ricochet Ronin/Reset Camera and HUD")]
    public static void ApplyReset()
    {
        // 1. Reset Main Camera
        var cameraGo = GameObject.FindWithTag("MainCamera");
        if (cameraGo == null) cameraGo = GameObject.Find("Main Camera");

        if (cameraGo != null)
        {
            Undo.RecordObject(cameraGo.transform, "Reset Camera Position");
            cameraGo.transform.localPosition = new Vector3(0f, 0f, -10f);
            Debug.Log("Reset Main Camera position to (0, 0, -10).");
        }
        else
        {
            Debug.LogWarning("Main Camera not found.");
        }

        // 2. Rearrange HUD
        var uiGo = GameObject.Find("UI");
        if (uiGo != null)
        {
            // Find QuinqueFive SDF font
            TMP_FontAsset pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Sprites/Fonts/QuinqueFive SDF.asset");
            if (pixelFont == null)
            {
                Debug.LogWarning("QuinqueFive SDF font not found at path 'Assets/Sprites/Fonts/QuinqueFive SDF.asset'. Trying to find in scene...");
                var searchComboGo = uiGo.transform.Find("Combo")?.gameObject;
                if (searchComboGo != null)
                {
                    var textComp = searchComboGo.GetComponent<TextMeshProUGUI>();
                    if (textComp != null) pixelFont = textComp.font;
                }
            }

            // Wave (TIME)
            var waveGo = uiGo.transform.Find("Wave")?.gameObject;
            if (waveGo != null) ConfigureText(waveGo, pixelFont, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -40), TextAlignmentOptions.Left, 18);

            // Combo (COMBO)
            var comboGo = uiGo.transform.Find("Combo")?.gameObject;
            if (comboGo != null) ConfigureText(comboGo, pixelFont, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -95), TextAlignmentOptions.Left, 18);

            // Timer (TIMER)
            var timerGo = uiGo.transform.Find("Timer")?.gameObject;
            if (timerGo != null) ConfigureText(timerGo, pixelFont, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), TextAlignmentOptions.Center, 18);

            // Dashes (SURVIVE)
            var dashesGo = uiGo.transform.Find("Dashes")?.gameObject;
            if (dashesGo != null) ConfigureText(dashesGo, pixelFont, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), TextAlignmentOptions.Right, 18);

            // Score (SCORE)
            var scoreGo = uiGo.transform.Find("Score")?.gameObject;
            if (scoreGo != null) ConfigureText(scoreGo, pixelFont, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -95), TextAlignmentOptions.Right, 18);

            Debug.Log("Rearranged HUD Elements successfully.");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        else
        {
            Debug.LogWarning("UI Canvas named 'UI' not found in the active scene.");
        }
    }

    private static void ConfigureText(GameObject go, TMP_FontAsset font, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, TextAlignmentOptions alignment, float fontSize)
    {
        var rect = go.GetComponent<RectTransform>();
        var textComp = go.GetComponent<TextMeshProUGUI>();

        if (rect != null)
        {
            Undo.RecordObject(rect, "Configure HUD Position");
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(400, 50);
            rect.anchoredPosition = anchoredPos;
        }

        if (textComp != null)
        {
            Undo.RecordObject(textComp, "Configure HUD Text Styling");
            if (font != null)
            {
                textComp.font = font;
            }
            textComp.fontSize = fontSize;
            textComp.alignment = alignment;
        }
    }
}
#endif
