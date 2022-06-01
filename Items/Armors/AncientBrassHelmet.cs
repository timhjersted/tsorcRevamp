using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientBrassHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 2;
            Item.value = 6000;
            Item.rare = ItemRarityID.Orange;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientBrassArmor>() && legs.type == ModContent.ItemType<AncientBrassGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.statDefense += 6;
            player.moveSpeed += 0.1f;
            player.GetDamage(DamageClass.Ranged) += 0.05f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronHelmet, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 100);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
