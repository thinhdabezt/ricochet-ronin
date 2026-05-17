using Assets._Scripts.Manager;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Data Congifuration")]
    [SerializeField] private EnemyDataSO enemyData;

    // [SerializeField] private GameObject deathVFXPrefab;

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
        
        GameObject textObj = ObjectPooler.Instance.Spawn(PoolType.DamageText.ToString(), transform.position, Quaternion.identity, 0.8f);

        if (textObj != null)
        {
        // Đặt vị trí ban đầu tại Enemy
        textObj.transform.position = transform.position;
        textObj.SetActive(true);

        // 2. Truy cập script DamageText để set giá trị
        DamageText dmgText = textObj.GetComponent<DamageText>();
        
        // Ví dụ: Nếu là đòn chí mạng thì màu vàng, bình thường màu trắng
        Color textColor = (dmg > 1) ? Color.yellow : Color.white;
        dmgText.Setup(dmg.ToString(), textColor);
    }   

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
        // Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        // Mượn đồ từ kho
        ObjectPooler.Instance.Spawn(PoolType.DeathVFX.ToString(), transform.position, Quaternion.identity, 0.5f);

        // Kích hoạt rung màn hình (Chúng ta sẽ code ở Juice 3)
        CameraShake.Instance.ShakeCamera(5f, 0.2f);

        Destroy(gameObject);
    }
}
