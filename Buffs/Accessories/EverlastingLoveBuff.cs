using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Accessories
{
    public class EverlastingLoveBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegen += 3;
            player.aggro -= 200;
        }
    }
}
