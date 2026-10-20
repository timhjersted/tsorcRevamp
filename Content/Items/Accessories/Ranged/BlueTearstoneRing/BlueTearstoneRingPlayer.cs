using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Ranged.BlueTearstoneRing;

public class BlueTearstoneRingPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }
}