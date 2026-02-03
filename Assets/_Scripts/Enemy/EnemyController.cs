using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private GameObject deathVFXPrefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            Die();
        }
    }

    private void Die()
    {
        // Tạo hiệu ứng nổ tại vị trí kẻ địch, không xoay (Quaternion.identity)
        Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        // Kích hoạt rung màn hình (Chúng ta sẽ code ở Juice 3)
        CameraShake.Instance.ShakeCamera(5f, 0.2f);

        Destroy(gameObject);
    }
}
