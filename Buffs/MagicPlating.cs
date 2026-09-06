using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
    public class MagicPlating : ModBuff
    {
        public const int MagicPlatingStacksCap = 40;
        public const int MagicPlatingStacksChance = 6;
        public const int StacksOnHit = 8;
        public const int StacksOnMinionHit = StacksOnHit / 4;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.endurance += player.GetModPlayer<tsorcRevampPlayer>().MagicPlatingStacks * 0.01f;
        }
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = LangUtils.GetTextValue("CommonItemTooltip.DRStat", Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().MagicPlatingStacks) + LangUtils.GetTextValue("Buffs.MagicPlating.Description");
        }

        public static int ManageStacks(int currentStacks, bool minion = false)
        {
            int stacksToAdd = StacksOnHit;
            if (minion)
            {
                stacksToAdd = StacksOnMinionHit;
            }
            int totalStacks = currentStacks + stacksToAdd;

            if (totalStacks > MagicPlatingStacksCap)
            {
                totalStacks = MagicPlatingStacksCap;
            }
            return totalStacks;
        }
    }
}