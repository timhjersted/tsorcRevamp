using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    public class ArtemisBow : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A bow forged to slay demon-gods.\nHold FIRE to charge\nArrows are faster and more accurate when the bow is charged");

        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.ArtemisBowHeld>();
            Item.channel = true;

            Item.damage = 220; //was 370
            Item.width = 14;
            Item.height = 28;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 15f;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item7;

            Item.shootSpeed = 18f;
            Item.useAmmo = AmmoID.Arrow;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<Projectiles.ArtemisBowHeld>(), damage, knockback, player.whoAmI, type);
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.MoltenFury, 1);
            recipe.AddIngredient(ItemID.AdamantiteBar, 3);
            //recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
