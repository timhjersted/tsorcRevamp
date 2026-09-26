using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.RubyCrystal;

public class RubyCrystalPlayer : ModPlayer
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
            Player.statLifeMax2 += RubyCrystalItem.MaxLifeIncrease;
        }
    }
}