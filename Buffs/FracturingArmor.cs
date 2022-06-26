using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class FracturingArmor : ModBuff
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fracturing Armor");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void ModifyBuffTip(ref string tip, ref int rare)
        {
            tip = "Your armor is crumbling away. Defense reduced by " + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().FracturingArmor;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense -= player.GetModPlayer<tsorcRevampPlayer>().FracturingArmor;
            player.GetModPlayer<tsorcRevampPlayer>().HasFracturingArmor = true;
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {

            if (player.GetModPlayer<tsorcRevampPlayer>().FracturingArmor < 65)
            {
                player.GetModPlayer<tsorcRevampPlayer>().FracturingArmor += 4;
            }
            return false;
        }
    }
}
