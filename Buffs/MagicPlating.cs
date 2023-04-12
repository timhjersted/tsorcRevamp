using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MagicPlating : ModBuff
    {
        public static int MagicPlatingStacks = 0;
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Magic Plating");
            // Description.SetDefault("Damage taken is reduced by attacking");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.endurance += MagicPlatingStacks * 0.01f;
        }
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = $"+{MagicPlatingStacks}% reduced damage against the next attack";
        }
    }
}