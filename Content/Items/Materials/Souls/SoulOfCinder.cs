using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls;

public class SoulOfCinder : Soul
{
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.95f, 0.45f, 0.12f);
    }

}