using UnityEngine;

public class PlayerDashingState : PlayerState
{
    private float minDashTime = 0.15f; // Wait slightly before checking stopping threshold
    private float stateTimer;

    public PlayerDashingState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) {}

    public override void Enter()
    {
        stateTimer = 0f;
        player.LaunchPlayer();
        GameEvents.OnPlayerDash?.Invoke();
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;
        
        // Return to Idle when speed is near zero after minDashTime
        if (stateTimer >= minDashTime && player.Rb.linearVelocity.magnitude < 0.15f)
        {
            stateMachine.ChangeState(player.IdleState);
            
            // Kiểm tra trạng thái Game Over nếu người chơi đã dừng hẳn
            GameManager.Instance.CheckGameOverCondition();
        }
    }
}
