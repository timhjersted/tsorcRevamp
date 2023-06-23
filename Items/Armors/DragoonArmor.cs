using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class DragoonArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("+30% melee speed" +
                "\nThe legends of Arradius spoke of the one who would wear this" +
                "\nYou are a master of all forces, the protector of Earth, the Hero of the age." +
                "\nSet Bonus: Harmonized with the four elements: fire, water, earth and air, including +2 life regen and flight" +
                "\nBoosts damage, crit chance and movement speed by 30%"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 30;
            Item.value = 5000;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += 0.3f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            //If the tier 1 head is on, it will handle the set bonus.
            //Applying it again here would make it apply twice.
            if(head.type == ModContent.ItemType<DragoonHelmet>())
            {
                return false;
            }
            
            //If the head has been upgraded, check if the greaves are valid. If so, apply the set bonus.
            if (head.type == ModContent.ItemType<DragoonHelmet2>())
            {
                if (legs.type == ModContent.ItemType<DragoonGreaves>() || legs.type == ModContent.ItemType<DragoonGreaves2>())
                {
                    return true;
                }
            }

            return false;
        }
        public override void UpdateArmorSet(Player player)
        {
            DragoonHelmet.ApplyDragoonSetBonus(player);
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<RedHerosShirt>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(ItemID.SoulofMight, 10);
            recipe.AddIngredient(ItemID.SoulofSight, 10);
            recipe.AddIngredient(ItemID.SoulofFright, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
