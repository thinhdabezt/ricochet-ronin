using UnityEngine;

[CreateAssetMenu(fileName = "TrailOfFireCard", menuName = "Roguelike/Cards/Trail of Fire")]
public class TrailOfFireCard : UpgradeCardSO
{
    private void OnEnable()
    {
        cardName = "Trail of Fire";
        cardDescription = "ACTIVE MUTATION\n\nLeaves a burning path behind you during a dash that damages enemies.";
        cardType = CardType.Mutation;
    }

    public override void ApplyUpgrade(Player player)
    {
        player.HasTrailOfFire = true;
    }
}
