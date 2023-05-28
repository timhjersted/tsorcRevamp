using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    class FracturingArmor : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = "Your armor is crumbling away. Defense reduced by " + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().FracturingArmor;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            player.statDefense -= modPlayer.FracturingArmor;
            modPlayer.HasFracturingArmor = true;
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.FracturingArmor < 65)
            {
                modPlayer.FracturingArmor += 4;
            }

            return false;
        }
    }
}
