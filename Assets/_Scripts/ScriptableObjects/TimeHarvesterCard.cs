using UnityEngine;

[CreateAssetMenu(fileName = "TimeHarvesterCard", menuName = "Roguelike/Cards/Time Harvester")]
public class TimeHarvesterCard : UpgradeCardSO
{
    private void OnEnable()
    {
        cardName = "Time Harvester";
        cardDescription = "PASSIVE STAT PERK\n\nIncreases the lifetime gained from ricochet kills (bouncing off walls) by +25%.";
        cardType = CardType.Passive;
    }

    public override void ApplyUpgrade(Player player)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TimeHarvesterBonusPercent += 0.25f;
        }
    }
}
