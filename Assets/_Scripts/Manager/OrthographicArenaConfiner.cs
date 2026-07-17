using UnityEngine;
using System;
using DG.Tweening;

/// <summary>
/// Procedurally generates 4 indestructible boundary walls exactly matching the Main Camera's viewport edges.
/// Prevents high-velocity player tunneling using thick colliders, continuous collision detection, and static rigidbodies.
/// </summary>
public class OrthographicArenaConfiner : MonoBehaviour
{
    // Event triggered when the player hits a boundary wall
    public event Action<Vector2, Vector2> OnWallHit;

    [Header("Physics Configuration")]
    [Tooltip("Thickness of the boundaries extending OUTWARDS from the viewport edges (5-10 recommended).")]
    [SerializeField] private float wallThickness = 10f;
    [SerializeField] private PhysicsMaterial2D customPhysicsMaterial;

    [Header("Juiciness & Feedback")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeStrength = 0.3f;
    [SerializeField] private Color sparksColor = new Color(0f, 0.95f, 1f, 1f); // Neon Cyan

    private Camera mainCamera;
    private GameObject wallsParent;
    private GameObject topWall, bottomWall, leftWall, rightWall;

    private float lastOrthographicSize;
    private float lastAspect;
    private Vector3 lastCameraPosition;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[OrthographicArenaConfiner] Main Camera not found in the scene.");
            return;
        }

        if (!mainCamera.orthographic)
        {
            Debug.LogWarning("[OrthographicArenaConfiner] Main Camera is not orthographic. Confiner may not match viewport correctly.");
        }

        // Initialize parent container
        wallsParent = GameObject.Find("ArenaBoundaries");
        if (wallsParent == null)
        {
            wallsParent = new GameObject("ArenaBoundaries");
        }

        CreatePhysicsMaterial();
        GenerateWalls();
        ConfigurePlayerPhysics();

        // Register initial camera metrics
        lastOrthographicSize = mainCamera.orthographicSize;
        lastAspect = mainCamera.aspect;
        lastCameraPosition = mainCamera.transform.position;
    }

    private void Start()
    {
        // Re-configure player physics in Start to ensure the Player object is fully initialized
        ConfigurePlayerPhysics();
    }

    private void Update()
    {
        if (mainCamera == null) return;

        // Dynamically adjust walls if camera size, aspect ratio, or position changes (e.g. resolution changes or screen shake offsets)
        // We check camera position ignoring the screen shake displacements by checking only relative variations, 
        // but to be safe we track orthographic size and aspect changes.
        if (!Mathf.Approximately(mainCamera.orthographicSize, lastOrthographicSize) ||
            !Mathf.Approximately(mainCamera.aspect, lastAspect))
        {
            UpdateWallPositionsAndSizes();
            lastOrthographicSize = mainCamera.orthographicSize;
            lastAspect = mainCamera.aspect;
        }
    }

    /// <summary>
    /// Forces the Player's Rigidbody2D to use Continuous collision detection.
    /// </summary>
    private void ConfigurePlayerPhysics()
    {
        GameObject playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            Rigidbody2D playerRb = playerGo.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }
    }

    /// <summary>
    /// Gets or creates a frictionless, maximum bounciness physics material.
    /// </summary>
    private void CreatePhysicsMaterial()
    {
        if (customPhysicsMaterial == null)
        {
            customPhysicsMaterial = new PhysicsMaterial2D("indestructibleFrictionlessBouncy")
            {
                friction = 0f,
                bounciness = 1f
            };
        }
    }

    /// <summary>
    /// Instantiates and configures the 4 wall GameObjects.
    /// </summary>
    private void GenerateWalls()
    {
        topWall = CreateWallSegment("TopWall");
        bottomWall = CreateWallSegment("BottomWall");
        leftWall = CreateWallSegment("LeftWall");
        rightWall = CreateWallSegment("RightWall");

        UpdateWallPositionsAndSizes();
    }

    private GameObject CreateWallSegment(string name)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(wallsParent.transform);
        
        // Mark as Wall layer
        int wallLayerId = LayerMask.NameToLayer("Wall");
        if (wallLayerId != -1) wall.layer = wallLayerId;

        // Static Rigidbody to prevent any physical movement/displacement
        Rigidbody2D rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        // BoxCollider2D Setup
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.sharedMaterial = customPhysicsMaterial;

        // Attach collision detector to forward events back to this script
        WallCollisionDetector detector = wall.AddComponent<WallCollisionDetector>();
        detector.onCollisionEnter = (collision) => HandleWallCollision(collision, name);

        return wall;
    }

    /// <summary>
    /// Recalculates wall positions and collider boundaries based on current camera viewport metrics.
    /// </summary>
    private void UpdateWallPositionsAndSizes()
    {
        if (mainCamera == null) return;

        float screenHeight = mainCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCamera.aspect;
        Vector3 camPos = mainCamera.transform.position;

        // Horizontal boundaries (Top & Bottom)
        // Set width with padding (wallThickness * 2f) to fully overlap at corners and eliminate gaps
        float horizontalWidth = screenWidth + (wallThickness * 2f);

        // Top Wall
        topWall.transform.position = new Vector3(camPos.x, camPos.y + (screenHeight / 2f) + (wallThickness / 2f), 0f);
        topWall.GetComponent<BoxCollider2D>().size = new Vector2(horizontalWidth, wallThickness);

        // Bottom Wall
        bottomWall.transform.position = new Vector3(camPos.x, camPos.y - (screenHeight / 2f) - (wallThickness / 2f), 0f);
        bottomWall.GetComponent<BoxCollider2D>().size = new Vector2(horizontalWidth, wallThickness);

        // Vertical boundaries (Left & Right)
        float verticalHeight = screenHeight;

        // Left Wall
        leftWall.transform.position = new Vector3(camPos.x - (screenWidth / 2f) - (wallThickness / 2f), camPos.y, 0f);
        leftWall.GetComponent<BoxCollider2D>().size = new Vector2(wallThickness, verticalHeight);

        // Right Wall
        rightWall.transform.position = new Vector3(camPos.x + (screenWidth / 2f) + (wallThickness / 2f), camPos.y, 0f);
        rightWall.GetComponent<BoxCollider2D>().size = new Vector2(wallThickness, verticalHeight);
    }

    /// <summary>
    /// Triggered whenever the player collides with any boundary wall.
    /// </summary>
    private void HandleWallCollision(Collision2D collision, string wallName)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ContactPoint2D contact = collision.GetContact(0);
            Vector2 hitPoint = contact.point;
            Vector2 hitNormal = contact.normal;

            // Trigger screen shake (uses modular CameraJuice if available, otherwise direct DOTween fallback)
            if (CameraJuice.Instance != null)
            {
                CameraJuice.Instance.TriggerScreenShake(shakeDuration, shakeStrength);
            }
            else
            {
                mainCamera.transform.DOShakePosition(shakeDuration, new Vector3(shakeStrength, shakeStrength, 0f), 15, 90f, false, true);
            }

            // Spawn dynamic neon sparks particle system aligned with hit normal
            SpawnHitSparks(hitPoint, hitNormal);

            // Invoke delegates
            OnWallHit?.Invoke(hitPoint, hitNormal);
        }
    }

    /// <summary>
    /// Procedurally creates a glowing spark burst on impact.
    /// </summary>
    private void SpawnHitSparks(Vector2 position, Vector2 normal)
    {
        // Clean up any existing particle systems to prevent "Setting the duration while system is still playing" error
        ParticleSystem[] existingSparks = GameObject.FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem existingPs in existingSparks)
        {
            if (existingPs.gameObject.name == "WallHitSparks")
            {
                existingPs.Stop(true);
                Destroy(existingPs.gameObject, 0.1f); // Give time for stop to complete
            }
        }

        GameObject sparksGo = new GameObject("WallHitSparks");
        sparksGo.transform.position = position;

        ParticleSystem particleSystem = sparksGo.AddComponent<ParticleSystem>();
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = particleSystem.main;
        main.duration = 0.4f;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.startColor = sparksColor;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);

        var emission = particleSystem.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.05f;

        // Rotate particle system to shoot sparks outward matching the bounce vector reflection normal
        float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
        sparksGo.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        var psr = sparksGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.material = new Material(Shader.Find("Sprites/Default"));
        }

        particleSystem.Play();
    }
}