using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    /// <summary>
    /// The Mimic Rune resummon cooldown (MimicTear.ResummonCooldownSeconds), applied when the rune is used.
    /// A plain timed debuff does everything the cooldown needs: vanilla saves it across relogs (so quitting doesn't
    /// skip it) and clears it on death (so dying resets it). The rune CanUseItem just checks for it.
    /// </summary>
    public class MimicTearCooldown : ModBuff
    {
        // PLACEHOLDER sprite (copy of SpiritAshRecoveryBuff) — replace with bespoke art.

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true; // Can't be right-clicked away.
            Main.buffNoSave[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }
}
