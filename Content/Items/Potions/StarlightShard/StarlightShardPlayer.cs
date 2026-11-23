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
                //^x% of maximum mana per second, since there are 60 ticks per second, manaregenbonuses are usually in the double digits so this is good scaling

                if (RestorationTimer >= 1f)
                {
                    int manaInt = (int)RestorationTimer;
                    Player.statMana += manaInt;
                    if (Player.statMana > Player.statManaMax2) //respect the cap so it doesn't spill over for a moment
                    {
                        Player.statMana = Player.statManaMax2;
                    }
                    RestorationTimer -= manaInt;
                }
            }

            if (!Restoration)
            {
                RestorationTimer = 0;
            }
        }
    }
}