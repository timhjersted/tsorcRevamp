using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class ThornDecapitator : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Thorn Decapitator");
            Tooltip.SetDefault("");

        }

        public override void SetDefaults()
        {
            Item.width = 70;
            Item.height = 80;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 25;
            Item.autoReuse = true;
            Item.useTime = 25;
            Item.maxStack = 1;
            Item.damage = 29;
            Item.knockBack = 5;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            //item.prefixType=483;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.BladeofGrass, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
