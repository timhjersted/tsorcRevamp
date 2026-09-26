using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Potions.Lifegem;

public class LifegemPlayer : ModPlayer
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

                if (HealingTimer == LifegemItem.HealingDivisor)
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