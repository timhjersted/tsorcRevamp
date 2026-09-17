using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Souls;

// Dropped (x2) from the Vessel of Souls' treasure bag; the crafting material for its two unique
// drops (Soul Reliquary summon + Gravemaw Tome). Generic soul sprite until bespoke art exists.
public class SoulOfTheVessel : Soul
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(BequeathedSoul));

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.6f, 0.15f, 0.8f); // purple soul-glow
    }
}