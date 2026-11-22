using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls
{
    // PLACEHOLDER sprite (copy of SoulOfChaos) — replace with bespoke art
    public class MarilithSoul : Soul
    {
        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.95f, 0.35f, 0.12f);
        }
    }
}
