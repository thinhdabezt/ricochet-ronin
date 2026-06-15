using UnityEngine;

public class Portal : MonoBehaviour
{
    private void Update()
    {
        // Smoothly rotate the portal to create an energy rift visual effect
        transform.Rotate(Vector3.forward * -120f * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AdvanceToNextFloor();
            }
            Destroy(gameObject);
        }
    }
}
