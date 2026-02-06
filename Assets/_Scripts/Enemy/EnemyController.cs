using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Data Congifuration")]
    [SerializeField] private EnemyDataSO enemyData;

    [SerializeField] private GameObject deathVFXPrefab;

    private SpriteRenderer sr;
    private int currentHealth;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        currentHealth = enemyData.maxHealth;
        sr.color = enemyData.enemyColor;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            TakeDamage(1);
        }
    }

    private void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        CameraShake.Instance.ShakeCamera(3f, 0.1f);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        GameEvents.OnEnemyDie?.Invoke(enemyData.scoreValue);

        // Tạo hiệu ứng nổ tại vị trí kẻ địch, không xoay (Quaternion.identity)
        Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        // Kích hoạt rung màn hình (Chúng ta sẽ code ở Juice 3)
        CameraShake.Instance.ShakeCamera(5f, 0.2f);

        Destroy(gameObject);
    }
}
