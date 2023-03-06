using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Shockwave : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Shockwave");
            // Description.SetDefault("Release a damaging shockwave when you land while holding DOWN.");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Shockwave = true;
        }
    }
}
