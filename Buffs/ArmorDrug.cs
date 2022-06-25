using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class ArmorDrug : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Armor Drug");
            Description.SetDefault("Defense is increased by 13!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 13;
        }
    }
}
