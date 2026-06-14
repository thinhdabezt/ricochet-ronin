using UnityEngine;
using TMPro;

public class InGameHUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI objectiveText;

    [Header("HUD Settings")]
    [SerializeField] private string objectivePrefix = "Survive: ";
    [SerializeField] private string objectiveSuffix = " seconds remaining";

    private void OnEnable()
    {
        GameEvents.OnSurvivalTimeChanged += UpdateObjectiveUI;
    }

    private void OnDisable()
    {
        GameEvents.OnSurvivalTimeChanged -= UpdateObjectiveUI;
    }

    private void UpdateObjectiveUI(int secondsRemaining)
    {
        if (objectiveText != null)
        {
            // Fully optimized: only run once per second when event is triggered
            objectiveText.text = $"{objectivePrefix}{secondsRemaining}{objectiveSuffix}";
        }
    }
}
