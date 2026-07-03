using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // +25% stamina cost on everything that spends it (weapons and dodge roll). The actual multiplier lives on
    // tsorcRevampPlayer.TiredStaminaMult, which WeaponStaminaMult already folds in and the dodge roll's flat
    // stamina cost multiplies by directly - this class just flips the flag those read.
    public class Tired : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Tired = true;
        }
    }
}
