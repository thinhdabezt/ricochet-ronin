using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDashingState : PlayerState
{
    private float minDashTime = 0.15f; // Wait slightly before checking stopping threshold
    private float stateTimer;
    private float fireDropTimer = 0f;
    private float fireDropInterval = 0.08f; // Interval between spawning fire segments

    public PlayerDashingState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) {}

    public override void Enter()
    {
        stateTimer = 0f;
        fireDropTimer = 0f;
        player.CurrentDashBounces = 0; // Reset ricochet bounce count for this dash
        player.LaunchPlayer();
        GameEvents.OnPlayerDash?.Invoke();
        GameManager.Instance.ResetDashKills();
    }

    public override void HandleInput()
    {
        // Double Dash redirection: allowed mid-dash only if HasDoubleDash mutation is active
        if (player.HasDoubleDash && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (GameManager.Instance != null)
            {
                // Consumes 5 seconds of player lifetime
                GameManager.Instance.PenalizePlayerTime(5f);
                GameManager.Instance.ResolveDashEnd();
            }
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

    public override void FixedUpdate()
    {
        // 1. Vacuum Blade: pull nearby enemies toward the player's path
        if (player.HasVacuumBlade)
        {
            float vacuumRadius = 3.0f;
            float pullSpeed = 8.0f;

            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(player.transform.position, vacuumRadius);
            foreach (var col in hitColliders)
            {
                if (col.CompareTag("Enemy"))
                {
                    Vector2 pullDirection = ((Vector2)player.transform.position - (Vector2)col.transform.position).normalized;
                    col.transform.position += (Vector3)pullDirection * pullSpeed * Time.fixedDeltaTime;
                }
            }
        }

        // 2. Trail of Fire: periodically drop flame triggers
        if (player.HasTrailOfFire)
        {
            fireDropTimer += Time.fixedDeltaTime;
            if (fireDropTimer >= fireDropInterval)
            {
                fireDropTimer = 0f;
                SpawnFireTrailSegment();
            }
        }
    }

    private void SpawnFireTrailSegment()
    {
        GameObject fire = new GameObject("FireTrailSegment");
        fire.transform.position = player.transform.position;

        var sr = fire.AddComponent<SpriteRenderer>();
        var playerSr = player.GetComponent<SpriteRenderer>();
        if (playerSr != null)
        {
            sr.sprite = playerSr.sprite;
        }
        sr.color = new Color(1f, 0.35f, 0f, 0.75f); // Orange fire color
        sr.sortingOrder = 3;
        fire.transform.localScale = Vector3.one * 0.6f;

        var col = fire.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        fire.AddComponent<FireTrailSegment>();
    }
}
