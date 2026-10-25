using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.VanillaItems.FrozenTurtleShell;

public class FrozenTurtleShellItem : GlobalItem
{
    public override void UpdateAccessory(Item item, Player player, bool hideVisual)
    {
        if (item.type == ItemID.FrozenTurtleShell || item.type == ItemID.FrozenShield)
        {
            player.GetModPlayer<FrozenTurtleShellPlayer>().Equipped = true;
        }
    }
}