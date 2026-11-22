using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // Applied while standing in the snow or desert biome (tsorcRevampPlayer.PostUpdateMiscEffects): no mounts, minecarts
    // included. Mount items are refused in tsorcGlobalItem.CanUseItem and anything already ridden is thrown off in
    // tsorcRevampPlayer.PostUpdateEquips. This class just flips the flag those places check.
    public class TreacherousTerrain : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MountsDisabled = true;
        }
    }
}
