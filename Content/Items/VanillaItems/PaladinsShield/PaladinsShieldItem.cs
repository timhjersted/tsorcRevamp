using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.VanillaItems.FrozenTurtleShell;

namespace tsorcRevamp.Content.Items.VanillaItems.PaladinsShield;

public class PaladinsShieldItem : GlobalItem
{
    public override void UpdateAccessory(Item item, Player player, bool hideVisual)
    {
        if (item.type == ItemID.PaladinsShield || item.type == ItemID.FrozenShield || item.type == ItemID.HeroShield)
        {
            player.GetModPlayer<PaladinsShieldPlayer>().Equipped = true;
        }
    }
}