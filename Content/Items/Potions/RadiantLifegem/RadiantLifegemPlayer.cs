using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Potions.RadiantLifegem;

public class RadiantLifegemPlayer : ModPlayer
{
    public bool Healing;
    public int HealingTimer;
    public override void ResetEffects()
    {
        Healing = false;
    }

    public override void PostUpdateEquips()
    {
        if (Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
        {
            if (Healing)
            {
                HealingTimer++;

                if (HealingTimer == RadiantLifegemItem.HealingDivisor)
                {
                    Player.statLife += 1;
                    HealingTimer = 0;
                }
            }
            else
            {
                HealingTimer = 0;
            }
        }

    }
}