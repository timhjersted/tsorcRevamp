using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class GreenBlossom : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs(Items.Potions.GreenBlossom.StaminaRegen);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += Items.Potions.GreenBlossom.StaminaRegen / 100f;
        }
    }
}
