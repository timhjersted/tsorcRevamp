using Terraria.ModLoader;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.Rings.ZirconRing;

public class ZirconRingPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void PostUpdateEquips()
    {
        if (Equipped)
        {
            Player.GetModPlayer<MaxHealthCalculator>().MaxHealthMultiplier +=
                ZirconRingItem.PercentMaxLifeIncrease / 100f;
        }
    }
}