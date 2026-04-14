using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    public class SpearOfMage : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Throwing spear that passes through walls and is created with mana on each throw");
        }
        public override void SetDefaults()
        {
            Item.shootSpeed = 17f;
            Item.damage = 57;
            Item.knockBack = 9f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.width = 30;
            Item.height = 84;
            Item.rare = ItemRarityID.LightRed;

            Item.consumable = false;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Ranged;

            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.LightRed_4;
            Item.shoot = ModContent.ProjectileType<Projectiles.Throwing.SpearOfMage>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<EphemeralThrowingSpear>(), 500);
            recipe.AddIngredient(ItemID.SoulofLight, 6);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
