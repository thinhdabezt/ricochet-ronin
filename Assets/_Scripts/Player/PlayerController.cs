using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float power = 10f;
    [SerializeField] private float maxDragLength = 10f;

    [Header("Trajectory Prediction")]
    [SerializeField] private LineRenderer lr;
    [SerializeField] private LayerMask collisionLayers;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private Camera mainCamera;
    private bool isDragging = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        lr.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isDragging = true;
            startPosition = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            rb.linearVelocity = Vector2.zero;

            lr.enabled = true;
        }

        if (isDragging)
        {
            Vector2 currentMousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 shootDirection = (startPosition - currentMousePos).normalized;

            DrawTrajectory(transform.position, shootDirection);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            endPosition = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            lr.enabled = false;

            if (Vector2.Distance(startPosition, endPosition) > 0.5f)
            {
                LaunchPlayer();
            }
        }
    }

    private void LaunchPlayer()
    {
        // Tính Vector lực: Lấy điểm đầu TRỪ điểm cuối (Cơ chế kéo ngược súng cao su)
        Vector2 forceVector = startPosition - endPosition;

        // ClampMagnitude: Cắt bớt vector nếu nó dài hơn maxDragLength
        // Giúp lực bắn không vượt quá giới hạn cho phép
        Vector2 clampedForce = Vector2.ClampMagnitude(forceVector, maxDragLength);

        // Thêm lực vào Rigidbody
        // ForceMode2D.Impulse: Tác động lực tức thời (như cú đánh, cú nổ) thay vì Force (đẩy từ từ)
        rb.AddForce(clampedForce * power, ForceMode2D.Impulse);
    }

    // --- HÀM MỚI: TÍNH TOÁN QUỸ ĐẠO ---
    private void DrawTrajectory(Vector2 startPos, Vector2 direction)
    {
        // Cần 3 điểm: Điểm bắt đầu -> Điểm chạm tường 1 -> Điểm chạm tường 2
        lr.positionCount = 3;
        lr.SetPosition(0, startPos);

        // Bắn tia Raycast thứ nhất
        RaycastHit2D hit1 = Physics2D.Raycast(startPos, direction, 50f, collisionLayers);

        if(hit1.collider != null)
        {
            lr.SetPosition(1, hit1.point);

            // TÍNH TOÁN PHẢN XẠ
            Vector2 reflectDirection = Vector2.Reflect(direction, hit1.normal);

            RaycastHit2D hit2 = Physics2D.Raycast(hit1.point + reflectDirection * 0.1f, reflectDirection, 50f, collisionLayers);

            if(hit2.collider != null)
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
