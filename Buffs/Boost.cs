using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Boost : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Boost");
            // Description.SetDefault("Increased movement speed");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 1.2f;
        }
    }
}
