using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class ArmorDrug : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Demonic Scales");
            Description.SetDefault("Your skin is covered with hard scales...");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 25;
            player.endurance += 0.15f;
        }
    }
}
