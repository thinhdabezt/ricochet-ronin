using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovementFix
{
    public static string Execute(float moveSpeed = 5f, bool allowVerticalMovement = true)
    {
        // Find all Player objects in the scene
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in players)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            
            if (playerInput == null)
            {
                playerInput = player.AddComponent<PlayerInput>();
            }
            
            InputAction moveAction = playerInput.actions["Player/Move"];
            
            // Add or update movement handler
            PlayerMovementHandler handler = player.GetComponent<PlayerMovementHandler>();
            if (handler == null)
            {
                handler = player.AddComponent<PlayerMovementHandler>();
            }
            
            handler.moveSpeed = moveSpeed;
            handler.allowVerticalMovement = allowVerticalMovement;
            handler.moveAction = moveAction;
            handler.rb = rb;
            
            // Find the PlayerController component
            Player playerController = player.GetComponent<Player>();
            if (playerController != null)
            {
                handler.stateMachine = playerController.StateMachine;
            }
        }
        
        return $"Added movement fix to {players.Length} player(s)";
    }
}

public class PlayerMovementHandler : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool allowVerticalMovement = true;
    public InputAction moveAction;
    public Rigidbody2D rb;
    public PlayerStateMachine stateMachine;

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            
            // Only move if not in aiming state (can move while aiming for positioning)
            if (stateMachine == null || stateMachine.CurrentState == null || 
                stateMachine.CurrentState.GetType().Name != "PlayerAimingState")
            {
                // Apply movement in all four directions (left, right, up, down)
                rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, 
                    allowVerticalMovement ? moveInput.y * moveSpeed : rb.linearVelocity.y);
            }
        }
    }
}