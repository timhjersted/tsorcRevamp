using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems;

public class ResourceCrystals : GlobalItem
{
    public override bool CanUseItem(Item item, Player player)
    {
        if ((item.type == ItemID.LifeCrystal | item.type == ItemID.ManaCrystal) && item.stack < 2)
        {
            return false;
        }
        return base.CanUseItem(item, player);
    }

    public override bool? UseItem(Item item, Player player)
    {
        if (item.type == ItemID.LifeCrystal | item.type == ItemID.ManaCrystal)
        {
            item.stack--;
        }
        return base.UseItem(item, player);
    }
}