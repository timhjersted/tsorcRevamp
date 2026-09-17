using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls;

public class CursedSoul : Soul
{
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.85f, 0f, 0f);
    }
}