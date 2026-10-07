using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class Hollowed : ModBuff
    {
        // PLACEHOLDER sprite pending something better. Autoloads from Hollowed.png.
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true; //I hate having to add this but without this Bonfires just clear it instantly upon respawn...
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            // Only Bearer of the Curse respawns apply this buff (tsorcRevampPlayer.OnRespawn), so every Hollowed is from dying.
            tip += "\n" + LangUtils.GetTextValue("Buffs.Hollowed.ReturnToBloodstain");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex] = 2;
            player.GetModPlayer<tsrHollowedPlayer>().Hollowed = true;

        }


        private class tsrHollowedPlayer : ModPlayer
        {
            public bool Hollowed = false;
            public bool HollowedLastFrame = false;

            public override void ResetEffects()
            {
                Hollowed = false;
            }

            public override void PostUpdate()
            {
                if (Hollowed)
                {
                    Player.statLifeMax2 = (int)(Player.statLifeMax2 * 0.80f);
                }

                //If they just lost the debuff this frame, restore the missing health
                if(HollowedLastFrame && !Hollowed)
                {
                    Player.Heal((int)(Player.statLifeMax2 * 0.2f));
                }

                HollowedLastFrame = Hollowed;

                Player.statLife = Math.Min(Player.statLife, Player.statLifeMax2);
            }
        }
    }

}
