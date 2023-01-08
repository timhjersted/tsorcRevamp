using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class CelestialShards : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons an array of shards which unleash divine wrath upon your foes");

        }
               

        public override void SetDefaults()
        {

            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTurn = true;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.maxStack = 1;
            Item.damage = 170;
            Item.autoReuse = true;
            Item.knockBack = (float)4;
            Item.scale = (float)1;
            Item.UseSound = SoundID.Item34;
            //item.projectile=Sandstorm;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = (float)10;
            Item.crit = 2;
            Item.mana = 14;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.CrystalShard, 12);
            //recipe.AddIngredient(ItemID.SandBlock, 150);
            recipe.AddIngredient(Mod.Find<ModItem>("FlameOfTheAbyss").Type, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 120000);
            recipe.AddTile(TileID.DemonAltar);

            //recipe.Register();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 projVector = new Vector2(10, 0).RotatedByRandom(MathHelper.TwoPi);
            for (int i = 0; i < 5; i++)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, projVector.RotatedBy(i * MathHelper.TwoPi / 5f), ModContent.ProjectileType<Projectiles.Magic.CelestialShard>(), damage, player.inventory[player.selectedItem].knockBack, player.whoAmI);
            }

            return true;
        }
    }
}
