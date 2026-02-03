using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy defeated!");

        Destroy(gameObject);
    }
}
