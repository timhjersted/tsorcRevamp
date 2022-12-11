using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class GravityAlignment : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Gravitional Alignment");
            Description.SetDefault("Attuned with the magical energy of the area\n" +
                "Press 'up' to reverse gravity");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            //player.GetModPlayer<tsorcRevampPlayer>().GravityAlignment = true;
        }
    }
}
