using UnityEngine;

public class ComboManager : MonoBehaviour
{
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

    private void UpdateCombo(int score, Vector2 position)
    {
        currentCombo++;
        comboResetTime = 2f;

        // Broadcast event-driven updates for observers (HUD and Combo Spawner)
        GameEvents.OnEnemyKilled?.Invoke(position, currentCombo);
        GameEvents.OnComboChanged?.Invoke(currentCombo);
    }

    private void Update()
    {
        if (currentCombo > 0)
        {
            comboResetTime -= Time.deltaTime;
            if (comboResetTime <= 0f)
            {
                currentCombo = 0;
                comboResetTime = 2f;
                GameEvents.OnComboChanged?.Invoke(0);
            }
        }
    }
}
