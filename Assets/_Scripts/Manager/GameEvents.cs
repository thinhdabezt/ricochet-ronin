using System;
using UnityEngine;

public static class GameEvents
{
    // Pass score value and death world position
    public static Action<int, Vector2> OnEnemyDie;
    public static Action OnPlayerDash;

    // Broadcasting dynamic combo spawner triggers
    public static Action<Vector2, int> OnEnemyKilled;
    public static Action<Vector2, int, float> OnScoreAndTimeGained;

    // HUD and state updates
    public static Action<int> OnScoreChanged;
    public static Action<int> OnComboChanged;
    public static Action<int> OnSurvivalTimeChanged;
}
