using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.ForgottenWarriorSet
{
    // Outfit converted from the Ghost of the Forgotten Warrior NPC sheet. Vanity only and not obtainable yet (no drop or recipe).
    // Helm and front pauldron are lifted from the NPC sheet (mirrored, made opaque); the torso, every arm cell and the
    // greaves are the Black Knight rig retinted onto the warrior's steel.
    [AutoloadEquip(EquipType.Head)]
    public class ForgottenWarriorHelm : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class ForgottenWarriorArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            // The retinted rig paints its own arms and gauntlets, so the player's skin must not draw underneath.
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesHands[equipSlot] = true;

            // The pauldron lives only in the front shoulder-cap cell (0,1), which vanilla hides in the jump pose
            // for modded armour. Without this it would disappear mid-air.
            ArmorIDs.Body.Sets.showsShouldersWhileJumping[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class ForgottenWarriorGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 20;
            Item.vanity = true;
        }
    }
}
