using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles.Summon.NecromanticSerpent;
using Terraria.Localization;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Armor
{
    public class NecromanticSerpentBuff : ModBuff
    {
        public override string Texture => "tsorcRevamp/Buffs/Armor/ShunpoBlink";
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (!modPlayer.NecromanticSerpent)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 2;
            }
        }
    }
}
