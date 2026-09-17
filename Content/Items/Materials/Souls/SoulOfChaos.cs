using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls;

public class SoulOfChaos : Soul
{
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.70f, 0.20f, 0.13f);
    }

}