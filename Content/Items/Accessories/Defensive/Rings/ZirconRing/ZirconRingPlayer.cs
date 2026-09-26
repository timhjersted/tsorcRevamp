using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.Rings.ZirconRing;

public class ZirconRingPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public void Update()
    {
        if (Equipped)
        {
            Player.statLifeMax2 += (int)(Player.statLifeMax2 * ZirconRingItem.PercentMaxLifeIncrease / 100f);
        }
    }
}