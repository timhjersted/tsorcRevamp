using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems;

public class ResourceCrystals : GlobalItem
{
    public override bool CanUseItem(Item item, Player player)
    {
        if (item.type == ItemID.LifeCrystal && player.statLifeMax < 400 && item.stack < 2)
        {
            return false;
        }
        if (item.type == ItemID.ManaCrystal && player.statManaMax < 200 && item.stack < 2)
        {
            return false;
        }
        return base.CanUseItem(item, player);
    }

    public override bool? UseItem(Item item, Player player)
    {
        if (item.type == ItemID.LifeCrystal && player.statLifeMax < 400)
        {
            item.stack--;
        }
        if (item.type == ItemID.ManaCrystal && player.statManaMax < 200)
        {
            item.stack--;
        }
        return base.UseItem(item, player);
    }
}