using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private float floatSpeed = 100f; // Tăng tốc độ bay cho phù hợp với ScreenSpace (pixel/s)
    [SerializeField] private float lifetime = 0.8f;

    private float timer;

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }

        // Tự động làm con của Canvas để hiển thị đúng trong UI
        if (transform.parent == null)
        {
            GameObject canvas = GameObject.Find("Canvas/Damage");
            if (canvas == null) canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                transform.SetParent(canvas.transform, false);
            }
        }
    }

    // Thay vì dùng OnEnable, tạo một hàm Setup để truyền dữ liệu từ ngoài vào
    public void Setup(string value, Color color)
    {
        textMesh.text = value;
        textMesh.color = color;
        timer = lifetime;
        
        // Random offset bằng pixel
        transform.position += new Vector3(Random.Range(-30f, 30f), Random.Range(10f, 40f), 0);
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
