using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls;

public class SoulOfOccultist : Soul
{
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.7f, 0f, 0.25f);
    }
}