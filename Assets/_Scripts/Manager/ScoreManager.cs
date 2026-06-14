using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private int currentScore = 0;

    private void OnEnable()
    {
        GameEvents.OnEnemyDie += UpdateScore;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyDie -= UpdateScore;
    }

    private void UpdateScore(int score, Vector2 position)
    {
        currentScore += score;
        GameEvents.OnScoreChanged?.Invoke(currentScore);
    }
}
