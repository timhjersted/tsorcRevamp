using Terraria;
using Terraria.ID;

namespace tsorcRevamp.Content.Items.Materials.Souls;

// PLACEHOLDER sprite (copy of BequeathedSoul) — replace with bespoke teal/coral Hydromancer soul art
/// <summary>Unique guaranteed drop of the Quara Hydromancer; 3 craft each piece of its armor.</summary>
public class HydromancerSoul : Soul
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.LightRed;
        Item.value = Item.sellPrice(gold: 1);
    }

    public override void PostUpdate()
    {
        Lighting.AddLight(Item.Center, 0.2f, 0.55f, 0.7f);
    }
}
