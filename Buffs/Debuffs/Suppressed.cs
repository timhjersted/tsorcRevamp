using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // Nullifies the mobility perks of Supersonic Boots / Supersonic Wings / Supersonic Wings II (and, to a
    // lesser degree, Wings of Seath) down to roughly early-hardmode boot territory. The actual stat clamping
    // happens in tsorcRevampPlayer.PostUpdateRunSpeeds() (keyed off supersonicLevel, which is already set by
    // whichever of these items is equipped) and in each item's UpdateEquip/UpdateAccessory for the noKnockback/
    // waterWalk/iceSkate perks - this class just flips the flag those places check.
    public class Suppressed : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.HasBuff(ModContent.BuffType<global::tsorcRevamp.Buffs.Bonfire>()))
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.GetModPlayer<tsorcRevampPlayer>().Suppressed = true;
        }
    }
}
