using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Potions.StarlightShard;

public class StarlightShardPlayer : ModPlayer
{
    public bool Restoration;
    public float RestorationTimer;
    public override void ResetEffects()
    {
        Restoration =  false;
    }

    public override void PostUpdateEquips()
    {
        if (Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
        {
            if (Restoration) //Restores 1% of maximum mana over 12 seconds by default
            {
                RestorationTimer += (float)Player.statManaMax2 / (100f * 60f) * (1f + ((float)Player.manaRegenBonus / 10f)); 
                //^1% of maximum mana per second, since there are 60 ticks per second, manaregenbonuses are usually in the double digits so this is insane scaling

                if (RestorationTimer >= 10f)
                {
                    Player.statMana += 10;
                    RestorationTimer -= 10f;
                }
                if (RestorationTimer >= 1f)
                {
                    Player.statMana += 1;
                    RestorationTimer -= 1f;
                }
            }

            if (!Restoration)
            {
                RestorationTimer = 0;
            }
        }
    }
}