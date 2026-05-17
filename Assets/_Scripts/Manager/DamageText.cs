using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float lifetime = 0.8f;

    private float timer;

    // Thay vì dùng OnEnable, tạo một hàm Setup để truyền dữ liệu từ ngoài vào
    public void Setup(string value, Color color)
    {
        textMesh.text = value;
        textMesh.color = color;
        timer = lifetime;
        
        // Tạo một chút ngẫu nhiên để các con số không đè khít lên nhau
        transform.localPosition += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0, 0.5f), 0);
    }

    void Update()
    {
        // Hiệu ứng bay lên
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Đếm ngược để tự trả về Pool
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            gameObject.SetActive(false); // Khi SetActive(false), Queue sẽ sẵn sàng cho lần tới
        }
    }
}
