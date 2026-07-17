using Assets._Scripts.Manager;
using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Data Configuration")]
    [SerializeField] private EnemyDataSO enemyData;

    private SpriteRenderer sr;
    private int currentHealth;

    // Movement & Mechanic Variables
    private Vector3 initialPos;
    private bool patrollingRight = true;
    private float patrolRange = 2.5f;
    private float actionTimer = 0f;
    private Transform playerTransform;
    private bool isFusing = false;
    private GameObject shieldObj;
    private bool isFrozen = false;
    private Vector3 ninjaTargetPos;
    private bool hasNinjaTarget = false;

    public void Initialize(EnemyDataSO data)
    {
        enemyData = data;
        currentHealth = enemyData.maxHealth;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.color = enemyData.enemyColor;
        
        // Re-setup initial position on initialize (important for spawned split slimes)
        initialPos = transform.position;
        isFusing = false;
        GetComponent<Collider2D>().enabled = true;
        transform.localScale = Vector3.one * (data.enemyName == "Mini Slime" ? 0.5f : 1.0f);

        hasNinjaTarget = false;

        SetupShieldVisual();
    }

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        initialPos = transform.position;
        playerTransform = GameObject.FindWithTag("Player")?.transform;

        hasNinjaTarget = false;

        // Initialize if not already set by GameManager
        if (enemyData != null && currentHealth == 0)
        {
            currentHealth = enemyData.maxHealth;
            sr.color = enemyData.enemyColor;
            SetupShieldVisual();
        }
    }

    private void SetupShieldVisual()
    {
        if (enemyData == null) return;

        // Destoy existing shield if any
        if (shieldObj != null) Destroy(shieldObj);

        if (enemyData.specialMechanic == EnemySpecialMechanic.FrontShield)
        {
            shieldObj = new GameObject("ShieldVisual");
            shieldObj.transform.SetParent(transform, false);
            shieldObj.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            shieldObj.transform.localScale = new Vector3(0.2f, 1.2f, 1f);

            var shieldSr = shieldObj.AddComponent<SpriteRenderer>();
            if (sr != null) shieldSr.sprite = sr.sprite;
            shieldSr.color = new Color(0.2f, 0.7f, 1.0f, 0.8f); // Cyan shield color
            shieldSr.sortingOrder = sr != null ? sr.sortingOrder + 1 : 1;
        }
    }

    private void Update()
    {
        if (isFrozen || isFusing || enemyData == null) return;

        if (playerTransform == null)
        {
            playerTransform = GameObject.FindWithTag("Player")?.transform;
        }

        HandleMovement();
        HandleSpecialCooldowns();
    }

    private void HandleMovement()
    {
        switch (enemyData.movementType)
        {
            case EnemyMovementType.Patrol:
                if (enemyData.enemyName == "Ninja")
                {
                    // Random 2D wander within a 4-unit radius of initialPos
                    if (!hasNinjaTarget)
                    {
                        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
                        float randomDist = Random.Range(1.0f, 4.0f);
                        Vector3 offset = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f) * randomDist;
                        ninjaTargetPos = initialPos + offset;

                        // Clamp to play area bounds (X: [-11, 11], Y: [-5, 5])
                        ninjaTargetPos.x = Mathf.Clamp(ninjaTargetPos.x, -11f, 11f);
                        ninjaTargetPos.y = Mathf.Clamp(ninjaTargetPos.y, -5f, 5f);

                        hasNinjaTarget = true;
                    }

                    Vector3 nextPos = Vector3.MoveTowards(transform.position, ninjaTargetPos, enemyData.moveSpeed * Time.deltaTime);
                    if (!IsPathBlockedByWall(nextPos))
                    {
                        transform.position = nextPos;
                    }
                    else
                    {
                        // Pick a new target next frame if blocked by a wall
                        hasNinjaTarget = false;
                    }

                    if (Vector3.Distance(transform.position, ninjaTargetPos) < 0.2f)
                    {
                        hasNinjaTarget = false;
                    }
                }
                else
                {
                    // Standard horizontal patrol
                    Vector3 nextPos = transform.position;
                    if (patrollingRight)
                    {
                        nextPos += Vector3.right * enemyData.moveSpeed * Time.deltaTime;
                        if (nextPos.x >= initialPos.x + patrolRange || IsPathBlockedByWall(nextPos))
                        {
                            patrollingRight = false;
                        }
                        else
                        {
                            transform.position = nextPos;
                        }
                    }
                    else
                    {
                        nextPos += Vector3.left * enemyData.moveSpeed * Time.deltaTime;
                        if (nextPos.x <= initialPos.x - patrolRange || IsPathBlockedByWall(nextPos))
                        {
                            patrollingRight = true;
                        }
                        else
                        {
                            transform.position = nextPos;
                        }
                    }
                }
                break;

            case EnemyMovementType.Blink:
                actionTimer += Time.deltaTime;
                if (actionTimer >= enemyData.actionCooldown)
                {
                    actionTimer = 0f;
                    BlinkTeleport();
                }
                break;

            case EnemyMovementType.ChasePlayer:
                if (playerTransform != null)
                {
                    Vector3 targetPos = Vector3.MoveTowards(transform.position, playerTransform.position, enemyData.moveSpeed * Time.deltaTime);
                    if (!IsPathBlockedByWall(targetPos))
                    {
                        transform.position = targetPos;
                    }
                    else
                    {
                        // Try sliding X only
                        Vector3 targetPosX = transform.position + new Vector3(targetPos.x - transform.position.x, 0f, 0f);
                        if (!IsPathBlockedByWall(targetPosX))
                        {
                            transform.position = targetPosX;
                        }
                        else
                        {
                            // Try sliding Y only
                            Vector3 targetPosY = transform.position + new Vector3(0f, targetPos.y - transform.position.y, 0f);
                            if (!IsPathBlockedByWall(targetPosY))
                            {
                                transform.position = targetPosY;
                            }
                        }
                    }
                }
                break;
        }
    }

    private void HandleSpecialCooldowns()
    {
        if (enemyData.specialMechanic == EnemySpecialMechanic.FrontShield && playerTransform != null)
        {
            // Tính hướng mặt về phía Player
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
            
            // Tốc độ xoay: 120 độ trên giây (giúp người chơi có thể lách qua khi quay nhanh)
            float rotationSpeed = 120f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else if (enemyData.specialMechanic == EnemySpecialMechanic.WeaverDrone)
        {
            actionTimer += Time.deltaTime;
            if (actionTimer >= enemyData.actionCooldown)
            {
                actionTimer = 0f;
                SpawnSlowZone();
            }
        }
    }

    private void BlinkTeleport()
    {
        // Teleport Effect
        ObjectPooler.Instance.Spawn(PoolType.DeathVFX.ToString(), transform.position, Quaternion.identity, 0.4f);
        
        Vector3 newPos = transform.position;
        int wallLayerMask = 1 << LayerMask.NameToLayer("Wall");
        int attempts = 0;
        bool validPos = false;

        while (!validPos && attempts < 30)
        {
            newPos = new Vector3(Random.Range(-11f, 11f), Random.Range(-5f, 5f), 0f);
            attempts++;
            if (Physics2D.OverlapCircle(newPos, 0.4f, wallLayerMask) == null)
            {
                validPos = true;
            }
        }

        transform.position = newPos;

        ObjectPooler.Instance.Spawn(PoolType.DeathVFX.ToString(), transform.position, Quaternion.identity, 0.4f);
    }

    private void SpawnSlowZone()
    {
        GameObject slowZoneGo = new GameObject("SlowZone");
        slowZoneGo.transform.position = transform.position;

        var szSr = slowZoneGo.AddComponent<SpriteRenderer>();
        if (sr != null) szSr.sprite = sr.sprite;
        szSr.color = new Color(0.6f, 0.2f, 0.8f, 0.3f); // Purple translucent slow area
        szSr.sortingOrder = sr != null ? sr.sortingOrder - 1 : 0;
        slowZoneGo.transform.localScale = new Vector3(3f, 3f, 1f);

        var col = slowZoneGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        slowZoneGo.AddComponent<SlowZone>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                bool isDashing = player.StateMachine != null && player.StateMachine.CurrentState is PlayerDashingState;
                if (isDashing)
                {
                    if (enemyData.specialMechanic == EnemySpecialMechanic.FrontShield)
                    {
                        // Calculate dot product of facing direction and player's relative position
                        Vector2 relativePlayerPos = collision.transform.position - transform.position;
                        float dot = Vector2.Dot(relativePlayerPos, transform.right);

                        // If player is in front hemisphere of the enemy, block the hit
                        if (dot > 0)
                        {
                            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
                            Vector3 contactPoint = (collision.transform.position + transform.position) * 0.5f;
                            Vector2 bounceDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;

                            if (playerRb != null)
                            {
                                // Deflect and bounce player back with 50% of old velocity
                                playerRb.linearVelocity = -playerRb.linearVelocity * 0.5f;
                            }

                            // Instantiate Spark Particles programmatically
                            InstantiateSparkParticles(contactPoint, bounceDir);

                            // Play iron steel metal clashing sound "KENG!" from Resources
                            AudioClip clashClip = Resources.Load<AudioClip>("metal_clash");
                            if (clashClip != null)
                            {
                                AudioSource.PlayClipAtPoint(clashClip, contactPoint);
                            }

                            SpawnBlockedText();
                            CameraShake.Instance.ShakeCamera(2f, 0.08f);
                            return; // Ignore damage
                        }
                    }

                    int damage = (GameManager.Instance != null && GameManager.Instance.IsGlassBladeActive ? 3 : 1);
                    if (GameManager.Instance != null)
                    {
                        damage += player.CurrentDashBounces * GameManager.Instance.KineticMomentumBonusDamage;
                    }
                    TakeDamage(damage);
                }
                else
                {
                    // Player is vulnerable and gets hit by the enemy!
                    float penalty = (GameManager.Instance != null && GameManager.Instance.IsGlassBladeActive ? 25f : 10f);
                    GameManager.Instance.PenalizePlayerTime(penalty);
                    CameraShake.Instance.ShakeCamera(4f, 0.15f);
 
                    // Push player back
                    Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        Vector2 pushDir = (collision.transform.position - transform.position).normalized;
                        playerRb.linearVelocity = Vector2.zero;
                        playerRb.AddForce(pushDir * 8f, ForceMode2D.Impulse);
                    }
                }
            }
        }
    }

    public void Freeze(float duration)
    {
        StartCoroutine(FreezeRoutine(duration));
    }
 
    private IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;
        yield return new WaitForSeconds(duration);
        isFrozen = false;
    }
 
    public void TakeDamage(int dmg)
    {
        if (isFusing || currentHealth <= 0) return;

        currentHealth -= dmg;
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        GameObject textObj = ObjectPooler.Instance.Spawn(PoolType.DamageText.ToString(), screenPos, Quaternion.identity, 0.8f);

        if (textObj != null)
        {
            textObj.transform.position = screenPos;
            textObj.SetActive(true);

            DamageText dmgText = textObj.GetComponent<DamageText>();
            Color textColor = Color.red; // Changed from white/yellow to red
            dmgText.Setup(dmg.ToString(), textColor);
        }   

        CameraShake.Instance.ShakeCamera(3f, 0.1f);

        if (currentHealth <= 0)
        {
            if (enemyData.specialMechanic == EnemySpecialMechanic.ExplodeOnDeath)
            {
                StartCoroutine(FusingRoutine());
            }
            else
            {
                Die();
            }
        }
    }

    private IEnumerator FusingRoutine()
    {
        isFusing = true;
        GetComponent<Collider2D>().enabled = false; // Disable collider

        float fuseDuration = 1.0f;
        float elapsed = 0f;
        Vector3 baseScale = transform.localScale;

        while (elapsed < fuseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fuseDuration);

            // Tween scale from 1.0 to 1.3
            transform.localScale = baseScale * Mathf.Lerp(1.0f, 1.3f, t);

            // Flashing frequency increases over time (5Hz to 25Hz)
            float freq = Mathf.Lerp(5f, 25f, t);
            bool showRed = Mathf.Sin(elapsed * freq * Mathf.PI * 2f) > 0f;
            sr.color = showRed ? Color.red : enemyData.enemyColor;

            yield return null;
        }

        ExplodeDetonation();
    }

    private void ExplodeDetonation()
    {
        // Spawn explosion visual
        ObjectPooler.Instance.Spawn(PoolType.DeathVFX.ToString(), transform.position, Quaternion.identity, 0.6f);
        CameraShake.Instance.ShakeCamera(6f, 0.3f);

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= 3.0f) // Explosion range
            {
                Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 pushDir = (playerTransform.position - transform.position).normalized;
                    playerRb.AddForce(pushDir * 15f, ForceMode2D.Impulse);
                }

                GameManager.Instance.PenalizePlayerTime(10f);
                SpawnExplosionWarningText();
            }
        }

        // Reward time and track dash kill
        float totalTimeBonus = enemyData.timeBonusOnKill;
        if (GameManager.Instance != null)
        {
            bool isRicochet = false;
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                var player = playerGo.GetComponent<Player>();
                if (player != null && player.CurrentDashBounces > 0)
                {
                    isRicochet = true;
                }
            }
 
            if (isRicochet)
            {
                totalTimeBonus *= (1f + GameManager.Instance.TimeHarvesterBonusPercent);
            }
 
            totalTimeBonus += GameManager.Instance.KillTimeBonusModifier;
        }
        GameManager.Instance.AddPlayerTime(totalTimeBonus);
        GameManager.Instance.RegisterDashKill();

        // Notify GameManager and destroy
        GameEvents.OnScoreAndTimeGained?.Invoke((Vector2)transform.position, enemyData.scoreValue, totalTimeBonus);
        GameEvents.OnEnemyDie?.Invoke(enemyData.scoreValue, (Vector2)transform.position);
        Destroy(gameObject);
    }

    private void Die()
    {
        // Reward time and track dash kill
        float totalTimeBonus = enemyData.timeBonusOnKill;
        if (GameManager.Instance != null)
        {
            bool isRicochet = false;
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                var player = playerGo.GetComponent<Player>();
                if (player != null && player.CurrentDashBounces > 0)
                {
                    isRicochet = true;
                }
            }
 
            if (isRicochet)
            {
                totalTimeBonus *= (1f + GameManager.Instance.TimeHarvesterBonusPercent);
            }
 
            totalTimeBonus += GameManager.Instance.KillTimeBonusModifier;
        }
        GameManager.Instance.AddPlayerTime(totalTimeBonus);
        GameManager.Instance.RegisterDashKill();

        GameEvents.OnScoreAndTimeGained?.Invoke((Vector2)transform.position, enemyData.scoreValue, totalTimeBonus);
        GameEvents.OnEnemyDie?.Invoke(enemyData.scoreValue, (Vector2)transform.position);
        ObjectPooler.Instance.Spawn(PoolType.DeathVFX.ToString(), transform.position, Quaternion.identity, 0.5f);
        CameraShake.Instance.ShakeCamera(5f, 0.2f);

        if (enemyData.specialMechanic == EnemySpecialMechanic.SplitOnDeath)
        {
            SplitSlimes();
        }

        Destroy(gameObject);
    }

    private void SplitSlimes()
    {
        for (int i = 0; i < 2; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
            GameObject miniGo = Instantiate(gameObject, transform.position + offset, Quaternion.identity, transform.parent);
            
            EnemyController miniController = miniGo.GetComponent<EnemyController>();
            if (miniController != null)
            {
                EnemyDataSO miniData = ScriptableObject.CreateInstance<EnemyDataSO>();
                miniData.enemyName = "Mini Slime";
                miniData.maxHealth = 1;
                miniData.enemyColor = enemyData.enemyColor;
                miniData.scoreValue = 10; // Balanced: 10 score points
                miniData.timeBonusOnKill = 0.5f; // Balanced: +0.5s time bonus
                miniData.movementType = EnemyMovementType.ChasePlayer;
                miniData.moveSpeed = enemyData.moveSpeed * 1.5f;
                miniData.specialMechanic = EnemySpecialMechanic.None; // mini-slimes do not split

                miniController.Initialize(miniData);
            }

            GameManager.Instance.RegisterSplitEnemy(1);
        }
    }

    private void InstantiateSparkParticles(Vector3 position, Vector2 direction)
    {
        GameObject sparksGo = new GameObject("SparkParticles");
        sparksGo.transform.position = position;

        ParticleSystem ps = sparksGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.startColor = new Color(1.0f, 0.85f, 0.2f, 1.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.gravityModifier = 1.2f;
        main.duration = 0.5f;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.1f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        sparksGo.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        var psr = sparksGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.material = new Material(Shader.Find("Sprites/Default"));
        }

        ps.Play();
    }

    private bool IsPathBlockedByWall(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position);
        float dist = dir.magnitude;
        if (dist == 0f) return false;

        int wallLayerMask = 1 << LayerMask.NameToLayer("Wall");
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.4f, dir.normalized, dist, wallLayerMask);

        return hit.collider != null;
    }

    private void SpawnBlockedText()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        GameObject textObj = ObjectPooler.Instance.Spawn(PoolType.DamageText.ToString(), screenPos, Quaternion.identity, 0.8f);
        if (textObj != null)
        {
            textObj.transform.position = screenPos;
            textObj.SetActive(true);
            DamageText dmgText = textObj.GetComponent<DamageText>();
            if (dmgText != null)
            {
                dmgText.Setup("BLOCKED!", new Color(0.2f, 0.7f, 1.0f));
            }
        }
    }

    private void SpawnExplosionWarningText()
    {
        if (playerTransform == null) return;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(playerTransform.position);
        GameObject textObj = ObjectPooler.Instance.Spawn(PoolType.DamageText.ToString(), screenPos, Quaternion.identity, 0.8f);
        if (textObj != null)
        {
            textObj.transform.position = screenPos;
            textObj.SetActive(true);
            DamageText dmgText = textObj.GetComponent<DamageText>();
            if (dmgText != null)
            {
                dmgText.Setup("EXPLODED! -1 TURN", Color.red);
            }
        }
    }
}
