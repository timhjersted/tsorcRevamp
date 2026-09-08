using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.SummonerRework
{
    /// <summary>
    /// Visible countdown for every Spirit Ash slot's 90-second warranty (see SpiritAshesRecoveryPlayer).
    /// Applied the moment a slot is born, not when it dies - a living, undamaged ash still shows this
    /// while its own warranty is running, same as one that has already fallen and is blocked. Shows the
    /// LONGEST remaining warranty across every summon type the player has out or has lost recently, so
    /// the buff only clears once every pending one has actually matured - using the soonest instead would
    /// let it disappear while a different type's slot was still running (or still locked out).
    /// </summary>
    public class SpiritAshRecoveryBuff : ModBuff
    {
        // PLACEHOLDER sprite (copy of ArcherSpiritBuff) - replace with bespoke art.

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false; // showing the countdown is the whole point
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int remaining = player.GetModPlayer<SpiritAshesRecoveryPlayer>().GetLongestRemainingTicks();

            if (remaining <= 0)
            {
                SpiritAshesRecoveryPlayer.DebugLog($"SpiritAshRecoveryBuff removing itself at tick={Main.GameUpdateCount} - GetLongestRemainingTicks()=0");
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.buffTime[buffIndex] = remaining;
        }

        /// <summary>
        /// The buff icon can only show one combined countdown (see Update/GetLongestRemainingTicks), so
        /// the hover tooltip lists every individual pending slot instead - one line per unresolved death,
        /// each ticking down independently. Only ever shown for the local player, same as any other buff
        /// tooltip, so reading their own recovery state directly is safe.
        /// </summary>
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            List<(int ProjectileType, int RemainingTicks)> pending =
                Main.LocalPlayer.GetModPlayer<SpiritAshesRecoveryPlayer>().GetAllPendingRecoveries();

            if (pending.Count == 0)
            {
                return;
            }

            StringBuilder text = new StringBuilder(tip);

            foreach ((int projectileType, int remainingTicks) in pending)
            {
                int seconds = (int)System.Math.Ceiling(remainingTicks / 60.0);
                string name = Lang.GetProjectileName(projectileType).Value;
                text.Append('\n').Append(name).Append(": ").Append(seconds).Append('s');
            }

            tip = text.ToString();
        }
    }
}
