using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // Bundles Weak + a flat 30% defense reduction + TornWings into a single icon/tooltip so it reads as one curse
    // rather than a pile of unrelated debuffs. The effects are replicated directly (matching vanilla's own
    // numbers for Weak) instead of stacking the vanilla buffs themselves, so only this one icon shows.
    // Wearing the Covenant of Artorias ring cures it instantly (see the CovenantOfArtoriasEquipped check below) -
    // EnterTheAbyss stays true afterward because the ring sets it independently via UpdateEquip/UpdateVanity.
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

            if (modPlayer.CovenantOfArtoriasEquipped)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            // Weak (vanilla BuffID.Weak numbers). meleeDamage/meleeSpeed are internal on Player and not
            // accessible from mod code, so use the public GetDamage/GetAttackSpeed accessors instead - same
            // underlying StatModifier/float, just reached through the public API.
            player.GetDamage(DamageClass.Melee) -= 0.051f;
            player.GetAttackSpeed(DamageClass.Melee) -= 0.051f;
            player.statDefense -= 4;
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
