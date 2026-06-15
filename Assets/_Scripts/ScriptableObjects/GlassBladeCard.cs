using UnityEngine;

[CreateAssetMenu(fileName = "GlassBladeCard", menuName = "Roguelike/Cards/Glass Blade")]
public class GlassBladeCard : UpgradeCardSO
{
    private void OnEnable()
    {
        cardName = "Glass Blade";
        cardDescription = "CORRUPTED PERK\n\n+200% damage (base damage = 3) and +50% launch power, but collision time penalty increases to 25s.";
        cardType = CardType.Corrupted;
    }

    public override void ApplyUpgrade(Player player)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.IsGlassBladeActive = true;
        }
        player.UpgradeWeaponPower(0.50f);
    }
}
