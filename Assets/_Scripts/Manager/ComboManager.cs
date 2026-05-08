using TMPro;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI timerText;
    private int currentCombo = 0;
    private float comboResetTime = 2f;

    private void OnEnable()
    {
        GameEvents.OnEnemyDie += UpdateCombo;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyDie -= UpdateCombo;
    }

    private void UpdateCombo(int score)
    {
        currentCombo += score;
        comboResetTime = 2f;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        comboText.text = $"Score: {currentCombo}";
    }

    private void UpdateTimerUI(float timer)
    {
        timerText.text = $"Timer: {timer.ToString("F2")}";
    }

    void Update()
    {
        if (currentCombo > 0)
        {
            comboResetTime -= Time.deltaTime;
            if (comboResetTime <= 0f)
            {
                currentCombo = 0;
                UpdateScoreUI();
                comboResetTime = 2f; // Reset lại thời gian cho combo tiếp theo
            }
            UpdateTimerUI(comboResetTime);
        }

    }
}
