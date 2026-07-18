using UnityEngine;

public enum IndexType { Monster, Upgrade }

[CreateAssetMenu(fileName = "NewIndexEntry", menuName = "Index System/Entry Data")]
public class IndexEntryData : ScriptableObject
{
    [Header("Bắt buộc phải có ID duy nhất (ví dụ: monster_01, perk_double_jump)")]
    [SerializeField] private string entryID; 
    [SerializeField] private string entryName;
    [SerializeField] private IndexType type;
    [SerializeField] private Sprite icon;
    [TextArea(3, 5)] [SerializeField] private string description;
    [TextArea(2, 4)] [SerializeField] private string loreText;

    public string EntryID => entryID;
    public string EntryName => entryName;
    public IndexType Type => type;
    public Sprite Icon => icon;
    public string Description => description;
    public string LoreText => loreText;

    // Helper for initialization via editor script
    public void Initialize(string id, string name, IndexType t, string desc, string lore)
    {
        entryID = id;
        entryName = name;
        type = t;
        description = desc;
        loreText = lore;
    }
}
