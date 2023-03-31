using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Rejuvenation : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rejuvenation");
            Description.SetDefault("Increases damage taken by 50% but increases life regen by 50");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegen += 50;
            player.endurance *= 0.5f;
        }
    }
}
