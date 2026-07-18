using System.Collections.Generic;
using UnityEngine;

public class IndexManager : MonoBehaviour
{
    public static IndexManager Instance { get; private set; }

    [Header("Database of all index items in the project")]
    [SerializeField] private List<IndexEntryData> allEntries = new List<IndexEntryData>();

    private HashSet<string> unlockedIDs = new HashSet<string>();
    private const string SAVE_KEY = "RicochetRonin_Index_Save";

    public List<IndexEntryData> AllEntries => allEntries;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadIndexData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UnlockEntry(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        
        if (!unlockedIDs.Contains(id))
        {
            unlockedIDs.Add(id);
            Debug.Log($"🎉 Unlocked Index Entry: {id}");
            
            TriggerUnlockPopup(id);
            SaveIndexData();
        }
    }

    public bool IsUnlocked(string id) => unlockedIDs.Contains(id);

    private void TriggerUnlockPopup(string id)
    {
        IndexEntryData entry = allEntries.Find(e => e.EntryID == id);
        if (entry != null)
        {
            Debug.Log($"DISCOVERED: {entry.EntryName}");
        }
    }

    public void SaveIndexData()
    {
        IndexSaveData data = new IndexSaveData { unlockedIDs = new List<string>(unlockedIDs) };
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public void LoadIndexData()
    {
        unlockedIDs.Clear();
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            IndexSaveData data = JsonUtility.FromJson<IndexSaveData>(json);
            if (data != null && data.unlockedIDs != null)
            {
                unlockedIDs = new HashSet<string>(data.unlockedIDs);
            }
        }
    }

    [System.Serializable]
    private class IndexSaveData
    {
        public List<string> unlockedIDs;
    }

    public void SetAllEntries(List<IndexEntryData> entries)
    {
        allEntries = entries;
    }
}
