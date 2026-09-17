using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls;

public class BequeathedSoul : Soul
{
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.33f, 0.75f, 0.70f);
    }

}