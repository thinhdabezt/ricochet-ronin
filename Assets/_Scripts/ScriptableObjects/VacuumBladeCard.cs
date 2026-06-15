using UnityEngine;

[CreateAssetMenu(fileName = "VacuumBladeCard", menuName = "Roguelike/Cards/Vacuum Blade")]
public class VacuumBladeCard : UpgradeCardSO
{
    private void OnEnable()
    {
        cardName = "Vacuum Blade";
        cardDescription = "ACTIVE MUTATION\n\nCreates a wind vortex during dash that pulls nearby enemies toward your path.";
        cardType = CardType.Mutation;
    }

    public override void ApplyUpgrade(Player player)
    {
        player.HasVacuumBlade = true;
    }
}
