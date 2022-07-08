using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("OldStuddedLeatherHelmet")]
    [LegacyName("OldLeatherHelmet")]
    [AutoloadEquip(EquipType.Head)]
    public class LeatherHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases ranged crit by 10%\nSet bonus: +5% Ranged Damage, 20% less chance to consume ammo");
        }
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.defense = 3;
            Item.value = 12000;
            Item.rare = ItemRarityID.White;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<LeatherArmor>() && legs.type == ModContent.ItemType<LeatherGreaves>();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += 0.1f;
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.05f;
            player.ammoCost80 = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Leather, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 150);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
