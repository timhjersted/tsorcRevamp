using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls;


public class GuardianSoul : Soul
{
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.93f, 0.1f, 0.45f);
    }
}