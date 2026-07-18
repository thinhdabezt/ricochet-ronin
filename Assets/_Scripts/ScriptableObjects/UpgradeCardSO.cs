using UnityEngine;

public enum CardType 
{ 
    Mutation, 
    Passive, 
    Corrupted 
}

public abstract class UpgradeCardSO : ScriptableObject
{
    [Header("Card Identity")]
    public string cardName;
    [TextArea] public string cardDescription;
    public CardType cardType;
    public Sprite cardIcon;

    [Header("Index System")]
    public IndexEntryData indexData;

    // The Strategy Pattern execution hook
    public abstract void ApplyUpgrade(Player player);
}
