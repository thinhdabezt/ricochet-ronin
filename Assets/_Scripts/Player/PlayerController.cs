using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float power = 10f;
    [SerializeField] private float maxDragLength = 10f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private Camera mainCamera;
    private bool isDragging = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
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
        }

        if (isDragging)
        {

        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            endPosition = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            LaunchPlayer();
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
}
