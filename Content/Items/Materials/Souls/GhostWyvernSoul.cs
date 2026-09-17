using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace tsorcRevamp.Content.Items.Materials.Souls;

public class GhostWyvernSoul : Soul
{
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 8));
        ItemID.Sets.AnimatesAsSoul[Item.type] = true;
        ItemID.Sets.ItemIconPulse[Item.type] = true;
        ItemID.Sets.ItemNoGravity[Item.type] = true;
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.85f, 0.33f, 0.23f);
    }

}