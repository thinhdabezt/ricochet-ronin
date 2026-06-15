using UnityEngine;

[CreateAssetMenu(fileName = "AdrenalineRushCard", menuName = "Roguelike/Cards/Adrenaline Rush")]
public class AdrenalineRushCard : UpgradeCardSO
{
    private void OnEnable()
    {
        cardName = "Adrenaline Rush";
        cardDescription = "PASSIVE STAT PERK\n\nSlows down time by an extra 20% during the aiming state.";
        cardType = CardType.Passive;
    }

    public override void ApplyUpgrade(Player player)
    {
        player.AimingTimeScaleModifier *= 0.8f;
    }
}
