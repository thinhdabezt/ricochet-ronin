using System.Collections;
using UnityEngine;

public class ReturnToPool : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f; // Thời gian tồn tại trước khi trả về kho

    // Hàm này chạy mỗi khi object được SetActive(true)
    private void OnEnable()
    {
        StartCoroutine(DisableRoutine());
    }

    private IEnumerator DisableRoutine()
    {
        yield return new WaitForSeconds(lifetime);

        // Thay vì Destroy, ta chỉ tắt nó đi
        gameObject.SetActive(false);
    }
}
