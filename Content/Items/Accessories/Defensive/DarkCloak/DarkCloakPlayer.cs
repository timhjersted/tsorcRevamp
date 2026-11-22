using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.DarkCloak;

public class DarkCloakPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }
    public void ApplyDarkCloak(in int currentHealth, in int threshold, float efficiency = 1f)
    {
        if (currentHealth <= threshold)
        {
            Player.statDefense += (int)(DarkCloakItem.Defense2 * efficiency);
        }
    }
}