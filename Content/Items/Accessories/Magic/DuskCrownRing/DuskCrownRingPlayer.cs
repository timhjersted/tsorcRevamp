using Terraria.ModLoader;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Accessories.Magic.DuskCrownRing;

public class DuskCrownRingPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public void MaxHealthPenalty()
    {
        Player.GetModPlayer<MaxHealthCalculator>().MaxHealthMultiplier *= 1f - (DuskCrownRingItem.BadMaxLife / 100f);
    }
}