using Assets._Scripts.Manager;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static ObjectPooler;

public class ObjectPooler : MonoBehaviour
{
    // Singleton pattern để dễ gọi từ bất cứ đâu
    private static ObjectPooler _instance;
    public static ObjectPooler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ObjectPooler>();
            }
            if (_instance != null)
            {
                _instance.InitiatePool();
            }
            return _instance;
        }
    }

    [System.Serializable]
    public class Pool
    {
        public string key;
        public GameObject prefab;
        public int size;
    }

    // Danh sách chứa các vật thể trong kho
    [SerializeField] private List<Pool> pools;

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, GameObject> prefabDictionary;
    private bool isInitialized = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            InitiatePool();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            InitiatePool();
        }
    }

    public void InitiatePool()
    {
        if (isInitialized && poolDictionary != null && prefabDictionary != null) return;
        isInitialized = true;

        // Khởi tạo danh sách
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        prefabDictionary = new Dictionary<string, GameObject>();

        // Vòng lặp tạo sẵn 20 vật thể ngay khi game bắt đầu
        foreach (Pool p in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < p.size; i++)
            {
                GameObject obj = Instantiate(p.prefab);
                obj.SetActive(false); // Tắt nó đi để chờ dùng

                PoolObject poolObject = obj.AddComponent<PoolObject>();
                poolObject.poolKey = p.key;
                poolObject.pooler = this;

                objectPool.Enqueue(obj); // Đưa vào hàng đợi
            }

            poolDictionary.Add(p.key, objectPool);
            prefabDictionary.Add(p.key, p.prefab);
        }
    }

    public GameObject Spawn(string key, Vector3 position, Quaternion quaternion, float autoReturnTime)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogError($"Pool with key {key} does not exist.");
            return null;
        }

        Queue<GameObject> objectPool = poolDictionary[key];
        GameObject obj;

        // Nếu pool hết object
        if (objectPool.Count == 0)
        {
            obj = Instantiate(prefabDictionary[key]);

            PoolObject poolObject = obj.AddComponent<PoolObject>();
            poolObject.poolKey = key;
            poolObject.pooler = this;
        }
        else
        {
            obj = objectPool.Dequeue(); // Lấy một object từ pool
        }

        // 2. Đặt vị trí và kích hoạt
        obj.transform.position = position;
        obj.transform.rotation = quaternion;
        obj.SetActive(true);

        if (autoReturnTime > 0)
        {
            StartCoroutine(AutoReturnCoroutine(obj, autoReturnTime));
        }

        return obj;
    }

    private IEnumerator AutoReturnCoroutine(GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);

        if (obj.activeInHierarchy)
        {
            ReturnToPool(obj);
        }
    }

    public void ReturnToPool(GameObject obj)
    {
        PoolObject poolObject = obj.GetComponent<PoolObject>();
        obj.SetActive(false); // Tắt nó đi
        poolDictionary[poolObject.poolKey].Enqueue(obj); // Đưa nó trở lại pool
    }
}
