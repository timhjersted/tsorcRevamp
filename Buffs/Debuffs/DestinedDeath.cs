using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class DestinedDeath : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.pvpBuff[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
            int currentLossPct = (int)MathF.Round(modPlayer.DestinedDeathCurrentHPLoss * 100f);
            int targetLossPct = modPlayer.DestinedDeathStacks * 10;

            if (modPlayer.DestinedDeathActive)
            {
                tip = $"Maximum life reduced by {currentLossPct}% (Stacks: {modPlayer.DestinedDeathStacks}/6)";
                if (currentLossPct >= 60)
                {
                    tip += "\n60% max life loss cap reached! Gained +15% increased damage!";
                }
            }
            else if (modPlayer.DestinedDeathCurrentHPLoss > 0f)
            {
                tip = $"Recovering maximum life! Current loss: {currentLossPct}% (+10%/sec recovery)";
            }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.DestinedDeathActive = true;
            if (modPlayer.DestinedDeathStacks == 0)
            {
                modPlayer.DestinedDeathStacks = 1;
            }
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.DestinedDeathActive = true;
            if (modPlayer.DestinedDeathStacks < 6)
            {
                modPlayer.DestinedDeathStacks++;
            }
            player.buffTime[buffIndex] = time; // Refresh duration to minimum 12 seconds (720 ticks)
            return true;
        }
    }
}
