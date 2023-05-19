using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs
{
    class Bonfire : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Bonfire");
            /* Description.SetDefault("Stay a little while... Let your soul heal \n" +
                                   "Enemy spawns disabled"); */
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
        }
    }
}
