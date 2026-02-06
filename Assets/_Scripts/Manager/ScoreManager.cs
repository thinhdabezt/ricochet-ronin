using System;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    private int currentScore = 0;

    private void OnEnable()
    {
        GameEvents.OnEnemyDie += UpdateScore;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyDie -= UpdateScore;
    }

    private void UpdateScore(int score)
    {
        currentScore += score;
        UpdateUI();
    }

    private void UpdateUI()
    {
        scoreText.text = $"Score: {currentScore}";
    }
}
