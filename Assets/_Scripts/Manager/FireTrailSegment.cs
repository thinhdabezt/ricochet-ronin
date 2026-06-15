using UnityEngine;

public class FireTrailSegment : MonoBehaviour
{
    private void Start()
    {
        // Trail vanishes after 2 seconds
        Destroy(gameObject, 2.0f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.GetComponent<EnemyController>();
            if (enemy != null)
            {
                // Deal damage to the enemy
                enemy.TakeDamage(1);
            }
        }
    }
}
