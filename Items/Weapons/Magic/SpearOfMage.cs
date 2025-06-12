using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class SpearOfMage : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Throwing spear that passes through walls and is created with mana on each throw");
        }
        public override void SetDefaults()
        {
            Item.shootSpeed = 16f;
            Item.damage = 66;
            Item.knockBack = 9f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 35;
            Item.useTime = 35;
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
            Item.shoot = ModContent.ProjectileType<Projectiles.SpearOfMage>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<EphemeralThrowingSpear>(), 100);
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
