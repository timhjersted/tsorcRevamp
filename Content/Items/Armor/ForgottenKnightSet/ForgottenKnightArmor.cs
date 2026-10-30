using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.ForgottenKnightSet
{
    // Outfit for the Ghost of the Forgotten Knight: the Black Knight set recoloured to silver with bronze horns and trim.
    // Vanity only and not obtainable yet (no drop or recipe). The sprites are opaque on purpose; the ghostly
    // see-through look belongs to whatever draws them (e.g. a puppet's spectral overlay), not to the art.
    [AutoloadEquip(EquipType.Head)]
    public class ForgottenKnightHelmet : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class ForgottenKnightBreastplate : ModItem
    {
        public override void SetStaticDefaults()
        {
            // The sheet paints its own arms and gauntlets, so the player's skin must not draw underneath.
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 22;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class ForgottenKnightGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 16;
            Item.vanity = true;
        }
    }
}
