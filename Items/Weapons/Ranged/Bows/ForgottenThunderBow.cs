using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Magic.Tomes;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    class ForgottenThunderBow : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Casts a bolt of lightning from your bow, doing massive damage over time. ");
        }
        public override void SetDefaults()
        {
            Item.damage = 245;
            Item.height = 78;
            Item.knockBack = 4;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.shootSpeed = 39;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item5;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = PriceByRarity.Purple_11;
            Item.width = 30;
            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.Bolt3Lightning>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<Projectiles.Magic.Bolt3Lightning>(), (int)(player.GetTotalDamage(DamageClass.Ranged).ApplyTo(Item.damage) * 1.9f), knockback, player.whoAmI);

            Vector2 lowPos = position + new Vector2(0, 22);
            Projectile.NewProjectile(source, lowPos, velocity, type, damage, knockback, player.whoAmI);

            Vector2 highPos = position + new Vector2(0, -22);
            Projectile.NewProjectile(source, highPos, velocity, type, damage, knockback, player.whoAmI);

            return false; 
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ForgottenThunderBowScroll>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Humanity>(), 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 160000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
