using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // Nullifies the mobility perks of Supersonic Boots / Supersonic Wings / Supersonic Wings II (and, to a
    // lesser degree, Wings of Seath) down to roughly early-hardmode boot territory, and blocks the vanilla
    // Gravitation and Featherfall buffs (removed, along with permanent-potion effects). The actual stat clamping happens in tsorcRevampPlayer.PostUpdateRunSpeeds()
    // (keyed off supersonicLevel, which is already set by whichever of these items is equipped) and in each
    // item's UpdateEquip/UpdateAccessory for the noKnockback/waterWalk/iceSkate perks; the Gravitation/Featherfall block and
    // the mount ban are in PostUpdateEquips(), and wings are grounded (no flight, no glide, wings stay folded) like Torn Wings via
    // tsorcRevampPlayer.WingsDisabled. This class just flips the flag those places check.
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
            player.GetModPlayer<tsorcRevampPlayer>().MountsDisabled = true;
        }
    }
}
