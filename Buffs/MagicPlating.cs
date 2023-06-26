using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
    public class MagicPlating : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.endurance += player.GetModPlayer<tsorcRevampPlayer>().MagicPlatingStacks * 0.01f;
        }
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = LaUtils.GetTextValue("CommonItemTooltip.Generic.DRStatBoost", Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().MagicPlatingStacks) + LaUtils.GetTextValue("Buffs.MagicPlating.Description");
        }
    }
}