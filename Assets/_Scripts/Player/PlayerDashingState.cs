using UnityEngine;
using UnityEngine.InputSystem;

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
        GameManager.Instance.ResetDashKills();
    }

    public override void HandleInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            GameManager.Instance.ResolveDashEnd();
            stateMachine.ChangeState(player.AimingState);
        }
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;
        
        // Return to Idle when speed is near zero after minDashTime
        if (stateTimer >= minDashTime && player.Rb.linearVelocity.magnitude < 0.15f)
        {
            GameManager.Instance.ResolveDashEnd();
            stateMachine.ChangeState(player.IdleState);
        }
    }
}
