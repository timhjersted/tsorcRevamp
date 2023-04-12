using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    class BrokenSpirit : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Broken Spirit");
            // Description.SetDefault("You feel like giving up!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.noKnockback = false;
        }
    }
}