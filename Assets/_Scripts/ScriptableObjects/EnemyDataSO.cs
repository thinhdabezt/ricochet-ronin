using UnityEngine;

public enum EnemyMovementType
{
    Static,
    Patrol,
    Blink,
    ChasePlayer
}

public enum EnemySpecialMechanic
{
    None,
    FrontShield,
    ExplodeOnDeath,
    SplitOnDeath,
    WeaverDrone
}

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Scriptable Objects/Enemy Data Expanded")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Base Identity")]
    public string enemyName;
    public Color enemyColor;

    [Header("Combat Stats")]
    public int maxHealth;
    public int scoreValue;
    public float timeBonusOnKill; // Lượng thời gian cộng thêm (nếu áp dụng cơ chế thời gian)

    [Header("Movement Behavior")]
    public EnemyMovementType movementType;
    public float moveSpeed = 2f;
    public float actionCooldown = 3f; // Thời gian cooldown cho blink hoặc đẻ slow zone

    [Header("Special Mechanics")]
    public EnemySpecialMechanic specialMechanic;
    public GameObject customEffectPrefab; // Prefab hỗ trợ (SlowZone, Explosion, hoặc Split)
}
