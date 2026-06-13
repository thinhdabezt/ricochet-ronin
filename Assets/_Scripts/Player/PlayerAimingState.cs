using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAimingState : PlayerState
{
    public PlayerAimingState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) {}

    public override void Enter()
    {
        player.SetTimeScale(0.1f);
        player.SetTrajectoryLineEnabled(true);
        player.SetStartPosition(player.GetMouseWorldPosition());
    }

    public override void Update()
    {
        Vector2 currentMousePos = player.GetMouseWorldPosition();
        Vector2 shootDirection = (player.StartPosition - currentMousePos).normalized;
        player.DrawTrajectory(player.transform.position, shootDirection);
    }

    public override void HandleInput()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector2 endPos = player.GetMouseWorldPosition();
            if (Vector2.Distance(player.StartPosition, endPos) > 0.5f)
            {
                player.SetEndPosition(endPos);
                stateMachine.ChangeState(player.DashingState);
            }
            else
            {
                stateMachine.ChangeState(player.IdleState);
            }
        }
    }

    public override void Exit()
    {
        player.SetTrajectoryLineEnabled(false);
        player.SetTimeScale(1f);
    }
}
