using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class LunarEventItemNerfs : GlobalItem
    {

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.DayBreak)
            {
                item.useAnimation = 32;
                item.useTime = 32;
                item.damage = 125;
            }
            if (item.type == ItemID.Phantasm)
            {
                item.damage = 35;
            }
            if (item.type == ItemID.LastPrism)
            {
                item.damage = 75;
            }
            if (item.type == ItemID.LunarFlareBook)
            {
                item.damage = 85;
            }
        }
    }
}
