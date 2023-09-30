using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class MagicEdit : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            //vanilla damage is 85, mana is 20
            if(item.type == ItemID.RazorbladeTyphoon)
            {
                item.damage = 40;
                item.mana = 30;
            }

            //vanilla damage is 70, mana is 5
            if(item.type == ItemID.BubbleGun)
            {
                item.damage = 30;
                item.mana = 10;
            }
        }
    }
}