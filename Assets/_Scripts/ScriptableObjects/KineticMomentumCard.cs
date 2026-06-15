using UnityEngine;

[CreateAssetMenu(fileName = "KineticMomentumCard", menuName = "Roguelike/Cards/Kinetic Momentum")]
public class KineticMomentumCard : UpgradeCardSO
{
    private void OnEnable()
    {
        cardName = "Kinetic Momentum";
        cardDescription = "PASSIVE STAT PERK\n\nEach wall bounce during a dash adds +1 damage to that dash (resets when player stops).";
        cardType = CardType.Passive;
    }

    public override void ApplyUpgrade(Player player)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.KineticMomentumBonusDamage += 1;
        }
    }
}
