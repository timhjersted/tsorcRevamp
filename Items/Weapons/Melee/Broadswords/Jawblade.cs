using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class Jawblade : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("'A blade of bone and fangs'" +
                "\nShoots out a homing skull upon hitting enemies with the blade"); */
        }
        public bool canitshoot = false;

        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 76;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.damage = 56;
            Item.knockBack = 5f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Beige * 0.75f;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, UsefulFunctions.Aim(player.Center, target.Center, 10), ProjectileID.BookOfSkullsSkull, damageDone, 1, player.whoAmI);
        }

        //TODO: Remove this
        public override bool CanShoot(Player player)
        {
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<CalciumBlade>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
