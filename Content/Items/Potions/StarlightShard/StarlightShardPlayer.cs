using Terraria;
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

    public override void PostUpdate()
    {
        if (Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
        {
            if (Restoration) //Restores 1% of maximum mana over 12 seconds by default
            {
                RestorationTimer += (float)Player.statManaMax2 / (100f * 60f) * (StarlightShardItem.BaseRestorationPercent + ((float)Player.manaRegenBonus / StarlightShardItem.RestorationScalingDivisor)); 
                //^x% of maximum mana per second, since there are 60 ticks per second, manaregenbonuses are usually in the double digits so this is insane scaling

                if (RestorationTimer >= 1f)
                {
                    int fullInt = (int)RestorationTimer;
                    Player.statMana += fullInt;
                    RestorationTimer -= fullInt;
                }
            }

            if (!Restoration)
            {
                RestorationTimer = 0;
            }
        }
    }
}