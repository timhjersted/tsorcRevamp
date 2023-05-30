using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class Hollowed : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex] = 2;
            player.GetModPlayer<tsrHollowedPlayer>().Hollowed = true;

        }

        private class tsrHollowedPlayer : ModPlayer
        {
            public bool Hollowed = false;

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

                Player.statLife = Math.Min(Player.statLife, Player.statLifeMax2);
            }
        }
    }

}
