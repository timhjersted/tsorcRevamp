using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Other.SilverSerpentRing;

public class SilverSerpentRingPlayer : ModPlayer
{
    public bool SilverSerpentRing;
    public override void ResetEffects()
    {
        SilverSerpentRing = false;
    }
}