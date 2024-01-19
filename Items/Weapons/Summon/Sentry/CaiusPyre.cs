using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Sentry
{
    public class CaiusPyre : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = 200;
            Item.knockBack = 0;
            Item.mana = 10;
            Item.width = 34;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.DD2_DefenseTowerSpawn;
            Item.sentry = true;
            Item.noMelee = true;
            Item.DamageType = DamageClass.MagicSummonHybrid;
            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Sentry.CaiusPyreRift>();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
            position = Main.MouseWorld;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
            projectile.originalDamage = Item.damage;
            player.UpdateMaxTurrets();

            return false;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FireGauntlet);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 4);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 66000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}