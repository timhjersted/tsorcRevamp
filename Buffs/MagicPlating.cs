using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class MagicPlating : ModBuff
    {
        public static int MagicPlatingStacks = 0;

        public override LocalizedText Description => base.Description.WithFormatArgs(MagicPlatingStacks);

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.endurance += MagicPlatingStacks * 0.01f;
        }
    }
}