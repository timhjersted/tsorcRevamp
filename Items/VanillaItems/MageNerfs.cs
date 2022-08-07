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

            //Lunar items
            if (item.type == ItemID.NebulaBlaze)
            {
                item.mana = 24;
            }
            if (item.type == ItemID.NebulaArcanum)
            {
                item.mana = 60;
            }
            if (item.type == ItemID.LastPrism)
            {
                item.mana = 30;
            }
            if (item.type == ItemID.LunarFlareBook)
            {
                item.damage = 80;
                item.mana = 39;
            }
        }
    }
}