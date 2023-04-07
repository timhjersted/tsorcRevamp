using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Magic
{
    class OrbOfSpiritualityDash : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spirit Rush Cooldown");
            Description.SetDefault("You can't dash until this runs out");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.immune = true;
        }
    }
}
