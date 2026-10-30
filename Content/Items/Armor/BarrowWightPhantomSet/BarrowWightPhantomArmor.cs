using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.BarrowWightPhantomSet
{
    // Outfit converted from the Barrow Wight Phantom NPC sheet. Vanity only and not obtainable yet (no drop or recipe).
    // Helm and robe hem are lifted from the NPC sheet (mirrored); the body is the Black Knight rig repainted in phantom colours.
    [AutoloadEquip(EquipType.Head)]
    public class BarrowWightPhantomHelm : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 24;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class BarrowWightPhantomArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            // The sheet paints its own sleeves, so the player's skin must not draw underneath.
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;

            // Both pauldrons live only in the shoulder-cap cells (0,1)/(1,1), which vanilla hides in the jump pose
            // for modded armour. Without this they would disappear mid-air.
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 24;
            Item.vanity = true;
        }
    }

    // The phantom has no legs: this is its tattered spectral robe, whose hem drifts through the NPC's 4 frames while walking.
    [AutoloadEquip(EquipType.Legs)]
    public class BarrowWightPhantomShroud : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 14;
            Item.vanity = true;
        }
    }
}
