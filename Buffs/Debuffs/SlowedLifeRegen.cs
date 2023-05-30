using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class SlowedLifeRegen : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.lifeRegen - 3 <= 0)
            {
                player.lifeRegen = 0;
            }
            else
            {
                player.lifeRegen -= 3;
            }
        }
    }
}