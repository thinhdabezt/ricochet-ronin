using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NeonSceneGenerator : MonoBehaviour
{
    [Header("Boundary Configuration")]
    [SerializeField] private float wallThickness = 1.0f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private PhysicsMaterial2D customPhysicsMaterial;

    [Header("Grid Configuration")]
    [SerializeField] private float gridSpacing = 2.0f;
    [SerializeField, Range(0.01f, 0.2f)] private float gridOpacity = 0.08f;
    [SerializeField] private Color gridColor = new Color(0f, 0.5f, 1f); // Neon blue base

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    private Sprite whiteSprite;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        // Perform setup at runtime initialization
        GenerateArena();
    }

    /// <summary>
    /// Generates or adjusts the arena boundaries, grid, post-processing volume, and materials.
    /// </summary>
    public void GenerateArena()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null)
        {
            Debug.LogError("NeonSceneGenerator: No camera found to calculate aspect ratio boundaries!");
            return;
        }

        // 1. Create a 1x1 white sprite procedurally if not set, to avoid package asset dependencies
        CreateProceduralSprite();

        // 2. Set Up Physics Material
        PhysicsMaterial2D wallPhysMat = GetOrCreatePhysicsMaterial();

        // 3. Generate Boundaries
        SetupBoundaries(wallPhysMat);

        // 4. Generate Background Grid
        SetupBackgroundGrid();

        // 5. Setup Post Processing Volume
        SetupPostProcessingVolume();

        // 6. Apply Glow to Player and Scene Objects if present
        ApplyGlowToEntities();

        // 7. Ensure CameraJuice is on the Camera
        EnsureCameraJuiceComponent();
    }

    private void CreateProceduralSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
    }

    private PhysicsMaterial2D GetOrCreatePhysicsMaterial()
    {
        if (customPhysicsMaterial != null) return customPhysicsMaterial;

        // Try to load default from project
#if UNITY_EDITOR
        string path = "Assets/PhysicsMaterials/SuperBouncy.physicsMaterial2D";
        PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat != null) return mat;

        // Create directory if missing
        if (!System.IO.Directory.Exists("Assets/PhysicsMaterials"))
        {
            System.IO.Directory.CreateDirectory("Assets/PhysicsMaterials");
        }

        mat = new PhysicsMaterial2D("SuperBouncy")
        {
            friction = 0f,
            bounciness = 1f
        };
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
#else
        PhysicsMaterial2D mat = new PhysicsMaterial2D("SuperBouncy")
        {
            friction = 0f,
            bounciness = 1f
        };
        return mat;
#endif
    }

    private void SetupBoundaries(PhysicsMaterial2D physMat)
    {
        // Find or create Walls parent
        GameObject wallsParent = GameObject.Find("Walls");
        if (wallsParent == null)
        {
            wallsParent = new GameObject("Walls");
        }

        // Calculate viewport sizing
        float orthographicSize = targetCamera.orthographicSize;
        float aspect = targetCamera.aspect;
        float height = orthographicSize * 2f;
        float width = height * aspect;

        int wallLayerId = LayerMask.NameToLayer("Wall");
        if (wallLayerId == -1) wallLayerId = 6; // Default fallback to Wall layer

        float visualThickness = 0.15f;
        float colliderThickness = 50.0f;
        float overlap = 0.3f; // Small overlap to ensure visual corners look perfect

        // For Right Wall:
        Vector2 rightColliderSize = new Vector2(colliderThickness, height + overlap * 2f);
        Vector2 rightColliderOffset = new Vector2(colliderThickness / 2f - visualThickness / 2f, 0f);
        ConfigureWall(wallsParent.transform, "Right", new Vector3(width / 2f, 0, 0), new Vector3(visualThickness, height + overlap * 2f, 1), physMat, wallLayerId, rightColliderSize, rightColliderOffset);

        // For Left Wall:
        Vector2 leftColliderSize = new Vector2(colliderThickness, height + overlap * 2f);
        Vector2 leftColliderOffset = new Vector2(visualThickness / 2f - colliderThickness / 2f, 0f);
        ConfigureWall(wallsParent.transform, "Left", new Vector3(-width / 2f, 0, 0), new Vector3(visualThickness, height + overlap * 2f, 1), physMat, wallLayerId, leftColliderSize, leftColliderOffset);

        // For Top Wall:
        Vector2 topColliderSize = new Vector2(width + overlap * 2f, colliderThickness);
        Vector2 topColliderOffset = new Vector2(0f, colliderThickness / 2f - visualThickness / 2f);
        ConfigureWall(wallsParent.transform, "Top", new Vector3(0, orthographicSize, 0), new Vector3(width + overlap * 2f, visualThickness, 1), physMat, wallLayerId, topColliderSize, topColliderOffset);

        // For Bottom Wall:
        Vector2 bottomColliderSize = new Vector2(width + overlap * 2f, colliderThickness);
        Vector2 bottomColliderOffset = new Vector2(0f, visualThickness / 2f - colliderThickness / 2f);
        ConfigureWall(wallsParent.transform, "Bottom", new Vector3(0, -orthographicSize, 0), new Vector3(width + overlap * 2f, visualThickness, 1), physMat, wallLayerId, bottomColliderSize, bottomColliderOffset);
    }

    private void ConfigureWall(Transform parent, string wallName, Vector3 position, Vector3 scale, PhysicsMaterial2D physMat, int layer, Vector2 colliderSize, Vector2 colliderOffset)
    {
        Transform wallTrans = parent.Find(wallName);
        GameObject wallGo;
        if (wallTrans != null)
        {
            wallGo = wallTrans.gameObject;
        }
        else
        {
            wallGo = new GameObject(wallName);
            wallGo.transform.SetParent(parent);
        }

        wallGo.transform.localPosition = position;
        wallGo.transform.localRotation = Quaternion.identity;
        wallGo.transform.localScale = scale;
        wallGo.layer = layer;

        // Setup BoxCollider2D
        BoxCollider2D collider = wallGo.GetComponent<BoxCollider2D>();
        if (collider == null) collider = wallGo.AddComponent<BoxCollider2D>();
        collider.sharedMaterial = physMat;
        collider.size = colliderSize;
        collider.offset = colliderOffset;

        // Setup SpriteRenderer for high glow minimalist neon border styling
        SpriteRenderer sr = wallGo.GetComponent<SpriteRenderer>();
        if (sr == null) sr = wallGo.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.sortingOrder = 1;

        // Assign URP Lit Neon Material
        Material neonMat = GetOrCreateNeonMaterial();
        sr.sharedMaterial = neonMat;

        // Set color of the boundary wall (Neon Cyan with high intensity)
        Color wallColor = new Color(0f, 0.95f, 1f, 1f); // Neon Cyan
        if (neonMat != null)
        {
            Material instanceMat = new Material(neonMat);
            instanceMat.SetColor("_BaseColor", wallColor);
            instanceMat.SetColor("_EmissionColor", wallColor * 2.5f);
            sr.sharedMaterial = instanceMat;
        }
    }

    private void SetupBackgroundGrid()
    {
        // Find or create BackgroundGrid parent
        GameObject gridParent = GameObject.Find("BackgroundGrid");
        if (gridParent != null)
        {
            DestroyImmediate(gridParent);
        }
        gridParent = new GameObject("BackgroundGrid");

        float orthographicSize = targetCamera.orthographicSize;
        float aspect = targetCamera.aspect;
        float height = orthographicSize * 2f;
        float width = height * aspect;

        Material neonMat = GetOrCreateNeonMaterial();
        Color gridColorWithAlpha = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        // Generate Vertical Grid Lines
        float xStart = -width / 2f;
        float xEnd = width / 2f;
        for (float x = xStart; x <= xEnd; x += gridSpacing)
        {
            CreateGridLine(gridParent.transform, $"VLine_{x}", new Vector3(x, 0, 0), new Vector3(0.04f, height, 1), gridColorWithAlpha, neonMat);
        }

        // Generate Horizontal Grid Lines
        float yStart = -orthographicSize;
        float yEnd = orthographicSize;
        for (float y = yStart; y <= yEnd; y += gridSpacing)
        {
            CreateGridLine(gridParent.transform, $"HLine_{y}", new Vector3(0, y, 0), new Vector3(width, 0.04f, 1), gridColorWithAlpha, neonMat);
        }
    }

    private void CreateGridLine(Transform parent, string lineName, Vector3 position, Vector3 scale, Color color, Material neonMat)
    {
        GameObject lineGo = new GameObject(lineName);
        lineGo.transform.SetParent(parent);
        lineGo.transform.localPosition = position;
        lineGo.transform.localScale = scale;

        SpriteRenderer sr = lineGo.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.sortingOrder = -10; // Behind everything in gameplay space

        if (neonMat != null)
        {
            Material instanceMat = new Material(neonMat);
            instanceMat.SetColor("_BaseColor", color);
            instanceMat.SetColor("_EmissionColor", color * 1.2f);
            sr.sharedMaterial = instanceMat;
        }
    }

    private void SetupPostProcessingVolume()
    {
        Volume volume = FindObjectOfType<Volume>();
        if (volume == null)
        {
            GameObject volumeGo = new GameObject("Global Volume");
            volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
        }

        VolumeProfile profile = volume.sharedProfile;
        if (profile == null)
        {
#if UNITY_EDITOR
            string path = "Assets/Settings/NeonNoirVolumeProfile.asset";
            // Ensure folder structure
            if (!System.IO.Directory.Exists("Assets/Settings"))
            {
                System.IO.Directory.CreateDirectory("Assets/Settings");
            }
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
                AssetDatabase.SaveAssets();
            }
#else
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
#endif
            volume.sharedProfile = profile;
        }

        // Configure Bloom
        if (!profile.TryGet<Bloom>(out var bloom))
        {
            bloom = profile.Add<Bloom>(true);
        }
        bloom.active = true;
        bloom.intensity.Override(2.5f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.7f);

        // Configure Vignette
        if (!profile.TryGet<Vignette>(out var vignette))
        {
            vignette = profile.Add<Vignette>(true);
        }
        vignette.active = true;
        vignette.intensity.Override(0.4f);
        vignette.smoothness.Override(0.2f);
        vignette.color.Override(Color.black);

        // Configure Chromatic Aberration
        if (!profile.TryGet<ChromaticAberration>(out var chromatic))
        {
            chromatic = profile.Add<ChromaticAberration>(true);
        }
        chromatic.active = true;
        chromatic.intensity.Override(0.1f);
    }

    private void ApplyGlowToEntities()
    {
        // 1. Color Player Cyan
        GameObject playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            Color neonCyan = new Color(0f, 0.95f, 1f, 1f); // #00F3FF
            ApplyNeonColor(playerGo, neonCyan, 2.0f);
        }

        // 2. Color Basic Enemies (if present) to Neon Violet
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            Color neonViolet = new Color(0.74f, 0f, 1f, 1f); // #BD00FF
            ApplyNeonColor(enemy, neonViolet, 2.0f);
        }
    }

    private void EnsureCameraJuiceComponent()
    {
        if (targetCamera != null)
        {
            CameraJuice juice = targetCamera.GetComponent<CameraJuice>();
            if (juice == null)
            {
                targetCamera.gameObject.AddComponent<CameraJuice>();
            }

            // Remove legacy CameraShake if it is present to prevent conflicts
            CameraShake shake = targetCamera.GetComponent<CameraShake>();
            if (shake != null)
            {
                // Disable or delete legacy camera shake in scene if conflict occurs
                shake.enabled = false;
            }
        }
    }

    public static Material GetOrCreateNeonMaterial()
    {
        Material mat = null;
#if UNITY_EDITOR
        string path = "Assets/Materials/NeonGlowMaterial.mat";
        if (!System.IO.Directory.Exists("Assets/Materials"))
        {
            System.IO.Directory.CreateDirectory("Assets/Materials");
        }
        mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }
#else
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.EnableKeyword("_EMISSION");
#endif
        return mat;
    }

    /// <summary>
    /// Programmatically applies neon color values to standard Renderers.
    /// </summary>
    public static void ApplyNeonColor(GameObject go, Color color, float emissionMultiplier)
    {
        if (go == null) return;
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        Material baseNeonMat = GetOrCreateNeonMaterial();

        foreach (Renderer r in renderers)
        {
            // Do not override line renderers used for UI / trajectory paths
            if (r is LineRenderer) continue;

            Material instanceMat = new Material(baseNeonMat);
            instanceMat.SetColor("_BaseColor", color);
            instanceMat.SetColor("_EmissionColor", color * emissionMultiplier);
            r.material = instanceMat;
        }
    }

    /// <summary>
    /// Applies high-intensity crimson emission to collectible or score particles.
    /// </summary>
    public static void ApplyNeonColorToParticles(GameObject particleSystemGo, Color color, float emissionMultiplier)
    {
        if (particleSystemGo == null) return;
        ParticleSystemRenderer psr = particleSystemGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            Material baseNeonMat = GetOrCreateNeonMaterial();
            Material instanceMat = new Material(baseNeonMat);
            instanceMat.SetColor("_BaseColor", color);
            instanceMat.SetColor("_EmissionColor", color * emissionMultiplier);
            psr.material = instanceMat;
        }
    }
}

#if UNITY_EDITOR
public class NeonSceneGeneratorEditor : EditorWindow
{
    [MenuItem("Tools/Neon Noir Arcade/Generate Arena")]
    public static void GenerateArenaMenu()
    {
        // Find NeonSceneGenerator in scene or create one
        NeonSceneGenerator generator = FindObjectOfType<NeonSceneGenerator>();
        if (generator == null)
        {
            GameObject go = new GameObject("NeonSceneGenerator");
            generator = go.AddComponent<NeonSceneGenerator>();
        }

        generator.GenerateArena();
        Debug.Log("Neon Noir Arcade Arena generated successfully!");
    }
}
#endif
