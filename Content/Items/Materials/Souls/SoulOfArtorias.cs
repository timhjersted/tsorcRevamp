using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls;

public class SoulOfArtorias : Soul
{
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.9f, 0.9f, 0.9f);
    }

}