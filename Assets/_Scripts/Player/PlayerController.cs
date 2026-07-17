using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Header("Equipment")]
    [SerializeField] private WeaponDataSO currentWeapon;

    [Header("Trajectory Prediction")]
    [SerializeField] private LineRenderer lr;
    [SerializeField] private LayerMask collisionLayers;

    [SerializeField] private SpriteRenderer sr;

    private float basePower = 10f;
    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private Camera mainCamera;



    // State Machine
    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerAimingState AimingState { get; private set; }
    public PlayerDashingState DashingState { get; private set; }

    // Properties exposed for states
    public Rigidbody2D Rb => rb;
    public Vector2 StartPosition => startPosition;
    public Vector2 EndPosition => endPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        lr.enabled = false;



        // Initialize state machine & states
        StateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, StateMachine);
        AimingState = new PlayerAimingState(this, StateMachine);
        DashingState = new PlayerDashingState(this, StateMachine);
    }

    private void Start()
    {
        EquipWeapon(currentWeapon);
        StateMachine.Initialize(IdleState);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (StateMachine != null && StateMachine.CurrentState != null)
        {
            StateMachine.CurrentState.HandleInput();
            StateMachine.CurrentState.Update();
        }


    }

    private void FixedUpdate()
    {
        if (StateMachine != null && StateMachine.CurrentState != null)
        {
            StateMachine.CurrentState.FixedUpdate();
        }
    }


    
    public float AimingDrainRateModifier { get; set; } = 1.0f;
    public float AimingTimeScaleModifier { get; set; } = 1.0f;

    // Active Mutation Locks
    public bool HasDoubleDash { get; set; } = false;
    public bool HasVacuumBlade { get; set; } = false;
    public bool HasTrailOfFire { get; set; } = false;

    // Richochet Bounce Tracking
    public int CurrentDashBounces { get; set; } = 0;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only count bounces off walls while actively dashing
        if (StateMachine != null && StateMachine.CurrentState is PlayerDashingState)
        {
            bool isWall = collision.gameObject.name.Contains("Wall") || 
                          (collision.transform.parent != null && collision.transform.parent.name.Contains("Wall"));
            if (isWall)
            {
                CurrentDashBounces++;
            }
        }
    }

    private void EquipWeapon(WeaponDataSO weapon)
    {
        currentWeapon = Instantiate(weapon); // Create a runtime copy to avoid disk asset mutations

        sr.color = currentWeapon.weaponColor;

        lr.startColor = currentWeapon.weaponColor;
        lr.endColor = currentWeapon.weaponColor;
    }

    public void UpgradeWeaponPower(float percentage)
    {
        if (currentWeapon != null)
        {
            currentWeapon.powerMultiplier += currentWeapon.powerMultiplier * percentage;
        }
    }

    public void UpgradeWeaponDrag(float percentage)
    {
        if (currentWeapon != null)
        {
            currentWeapon.maxDragDistance += currentWeapon.maxDragDistance * percentage;
        }
    }

    // Helper methods for states
    public Vector2 GetMouseWorldPosition()
    {
        if (Mouse.current == null) return transform.position;
        return mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    public void SetTrajectoryLineEnabled(bool enabled)
    {
        lr.enabled = enabled;
    }

    public void SetStartPosition(Vector2 pos)
    {
        startPosition = pos;
    }

    public void SetEndPosition(Vector2 pos)
    {
        endPosition = pos;
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * scale;
    }

    public void LaunchPlayer()
    {
        // Tính Vector lực: Lấy điểm đầu TRỪ điểm cuối (Cơ chế kéo ngược súng cao su)
        Vector2 forceVector = startPosition - endPosition;

        // ClampMagnitude: Cắt bớt vector nếu nó dài hơn maxDragLength
        // Giúp lực bắn không vượt quá giới hạn cho phép
        Vector2 clampedForce = Vector2.ClampMagnitude(forceVector, currentWeapon.maxDragDistance);

        // Khi AddForce, nhân thêm hệ số của vũ khí
        float finalPower = basePower * currentWeapon.powerMultiplier;

        // Thêm lực vào Rigidbody
        // ForceMode2D.Impulse: Tác động lực tức thời (như cú đánh, cú nổ) thay vì Force (đẩy từ từ)
        rb.AddForce(clampedForce * finalPower, ForceMode2D.Impulse);
    }

    // --- HÀM TÍNH TOÁN QUỸ ĐẠO ---
    public void DrawTrajectory(Vector2 startPos, Vector2 direction)
    {
        // Cần 3 điểm: Điểm bắt đầu -> Điểm chạm tường 1 -> Điểm chạm tường 2
        lr.positionCount = 3;
        lr.SetPosition(0, startPos);

        // Bắn tia Raycast thứ nhất
        RaycastHit2D hit1 = Physics2D.Raycast(startPos, direction, 50f, collisionLayers);

        if (hit1.collider != null)
        {
            lr.SetPosition(1, hit1.point);

            // TÍNH TOÁN PHẢN XẠ
            Vector2 reflectDirection = Vector2.Reflect(direction, hit1.normal);

            RaycastHit2D hit2 = Physics2D.Raycast(hit1.point + reflectDirection * 0.1f, reflectDirection, 50f, collisionLayers);

            if (hit2.collider != null)
            {
                lr.SetPosition(2, hit2.point);
            }
            else
            {
                lr.SetPosition(2, hit1.point + reflectDirection * 10f);
            }
        }
        else
        {
            lr.positionCount = 2;
            lr.SetPosition(1, startPos + direction * 10f);
        }
    }
}
