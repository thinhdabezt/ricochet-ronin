using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) {}

    public override void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            stateMachine.ChangeState(player.AimingState);
        }
    }
}
