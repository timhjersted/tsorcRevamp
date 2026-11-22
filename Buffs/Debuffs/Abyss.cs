using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // A single custom curse: stat penalties and the TornWings flag are applied directly, without adding
    // vanilla Weak or a separate Torn Wings buff. Melee damage and flat defense are deliberately unaffected.
    // The Covenant ring grants immunity. Environmental presence is maintained independently by AbyssWorldSystem;
    // this flag also preserves temporary boss/enemy Abyss effects outside Super Hardmode, without changing time.
    public class Abyss : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.EnterTheAbyss = true;

            // Slow attacks across damage classes, without Weak's melee-damage or flat-defense loss.
            player.GetAttackSpeed(DamageClass.Generic) -= 0.08f;
            player.moveSpeed -= 0.1f;

            // -30% defense. DefenseStat's * operator multiplies into a separate running FinalMultiplier that's
            // only applied once at the very end (see Player.DefenseStat.operator int), so unlike a flat -=
            // this is safe to apply here regardless of buff-loop ordering relative to other defense sources.
            player.statDefense *= 0.7f;

            // TornWings
            modPlayer.TornWings = true;

            // No passive life regen while cursed by the Abyss. Buff Update() runs after Player's natural
            // regen is folded into lifeRegen for the tick (same pattern vanilla's own Poisoned debuff uses),
            // so clamping here zeroes out any positive regen without touching negative regen from other debuffs.
            if (player.lifeRegen > 0)
            {
                player.lifeRegen = 0;
            }
        }
    }
}
