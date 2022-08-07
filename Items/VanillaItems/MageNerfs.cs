using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.VanillaItems
{
    class MageNerfs : GlobalItem
    {

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.SparkleGuitar)
            {
                item.mana = 25;
            }
        }
    }
}