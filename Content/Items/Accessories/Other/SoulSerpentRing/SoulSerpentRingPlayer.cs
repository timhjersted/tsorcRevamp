using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Other.SoulSerpentRing;

public class SoulSerpentRingPlayer : ModPlayer
{
    public bool SoulSerpentRing;
    public override void ResetEffects()
    {
        SoulSerpentRing = false;
    }
}