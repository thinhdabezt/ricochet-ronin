using UnityEngine;

[CreateAssetMenu(fileName = "DoubleDashCard", menuName = "Roguelike/Cards/Double Dash")]
public class DoubleDashCard : UpgradeCardSO
{
    private void OnEnable()
    {
        cardName = "Double Dash";
        cardDescription = "ACTIVE MUTATION\n\nAllows redirecting mid-dash by left-clicking. Costs 5s of lifetime per redirection.";
        cardType = CardType.Mutation;
    }

    public override void ApplyUpgrade(Player player)
    {
        player.HasDoubleDash = true;
    }
}
