using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    // Singleton pattern để dễ gọi từ bất cứ đâu
    public static ObjectPooler Instance;

    [Header("Pool Settings")]
    [SerializeField] private GameObject vfxPrefab; // Prefab cần pool
    [SerializeField] private int poolSize = 20;    // Số lượng tạo sẵn

    // Danh sách chứa các vật thể trong kho
    private List<GameObject> pooledObjects;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Khởi tạo danh sách
        pooledObjects = new List<GameObject>();

        // Vòng lặp tạo sẵn 20 vật thể ngay khi game bắt đầu
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(vfxPrefab);
            obj.SetActive(false); // Tắt nó đi (Cất vào kho)
            obj.transform.parent = transform; // Gom nó vào object này cho gọn Hierarchy
            pooledObjects.Add(obj); // Ghi tên vào sổ cái
        }
    }

    public GameObject GetPooledObject()
    {
        // Duyệt qua danh sách xem có cái nào đang RẢNH (không active) không
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].activeInHierarchy) // Nếu vật thể này đang không được sử dụng
            {
                return pooledObjects[i]; // Trả về nó để dùng
            }
        }

        // Nếu cả 20 cái đều đang bận (game quá hỗn loạn)?
        // Cách xử lý đơn giản: Bỏ qua hiệu ứng (return null) hoặc Mở rộng kho (Advanced).
        // Ở đây ta tạm thời return null.
        return null;
    }
}
