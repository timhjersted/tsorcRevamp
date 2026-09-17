using Microsoft.Xna.Framework;
using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls;

public class SoulOfLife : Soul
{
    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, Color.Green.ToVector3());
    }
}