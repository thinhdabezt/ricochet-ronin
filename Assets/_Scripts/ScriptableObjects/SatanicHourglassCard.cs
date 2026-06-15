using UnityEngine;

[CreateAssetMenu(fileName = "SatanicHourglassCard", menuName = "Roguelike/Cards/Satanic Hourglass")]
public class SatanicHourglassCard : UpgradeCardSO
{
    private void OnEnable()
    {
        cardName = "Satanic Hourglass";
        cardDescription = "CORRUPTED PERK\n\nIdle lifetime drain rate is doubled, but killing an enemy freezes all other enemies for 1s.";
        cardType = CardType.Corrupted;
    }

    public override void ApplyUpgrade(Player player)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.IsSatanicHourglassActive = true;
        }
    }
}
