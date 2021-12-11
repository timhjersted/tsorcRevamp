using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class MoonlightGreatsword : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The Moonlight Greatsword" +
                                "\nThe sword of legend..." +
                                "\n...");
        }

        //If only it was this easy :/
		//public override string Texture => TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.UltimaWeapon];

        public override void SetDefaults() {
            item.rare = ItemRarityID.Pink;
            item.damage = 250;
            item.height = 76;
            item.width = 76;
            item.knockBack = 14f;
            item.melee = true;
            item.autoReuse = true;
            item.useAnimation = 27;
            item.useTime = 27;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 1000000;
            item.shoot = ModContent.ProjectileType<Projectiles.Crescent>();
            item.shootSpeed = 10f;
        }
        /*public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Excalibur, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 5);
            recipe.AddIngredient(ItemID.SoulofSight, 5);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 85000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
        */

       

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if (Main.rand.Next(5) == 0)
            {
                item.shoot = ModContent.ProjectileType<Projectiles.CrescentTrue>();
                item.shootSpeed = 10f;
            }
                return true;
        }


        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 15, player.velocity.X * 0.2f + player.direction * 3, player.velocity.Y * 0.2f, 100, default, 1.0f);
            Main.dust[dust].noGravity = true;
        }
    }
}
