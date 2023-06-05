using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.Buffs
{
    public class Boost : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs(BoostPotion.MovementSpeedMultiplier);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 1f + BoostPotion.MovementSpeedMultiplier / 100f;
        }
    }
}
