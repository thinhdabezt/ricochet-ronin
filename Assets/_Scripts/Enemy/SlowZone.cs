using UnityEngine;

public class SlowZone : MonoBehaviour
{
    private float lifetime = 4f;

    private void Start()
    {
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Drains player's velocity to slow them down significantly
                rb.linearVelocity *= 0.85f;
            }
        }
    }
}
