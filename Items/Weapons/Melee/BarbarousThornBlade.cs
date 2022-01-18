using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class BarbarousThornBlade : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Barbarous Thorn Blade");
            Tooltip.SetDefault("A blade that lashes out with the fury of a mythical beast"
            + "\nRight-click to shoot briars");
        }
        public override void SetDefaults()
        {
            item.damage = 36;
            item.melee = true;
            item.width = 44;
            item.height = 44;
            item.useTime = 17;
            item.useAnimation = 17;
            item.useStyle = ItemUseStyleID.Stabbing;
            item.knockBack = 3.5f;
            item.value = 25000;
            item.rare = ItemRarityID.Orange;
            item.scale = .9f;
            item.autoReuse = true;
            item.useTurn = false;
            item.shoot = 0;
            item.UseSound = SoundID.Item7;
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {

                item.useStyle = 1;
                item.shoot = ModContent.ProjectileType<Projectiles.Briar>();
                item.shootSpeed = 8.5f;
                item.useTime = 34;
                item.useAnimation = 34;
                item.UseSound = SoundID.Item1;

            }

            else
            {
                item.useStyle = 3;
                item.shoot = 0;
                item.useTime = 17;
                item.useAnimation = 17;
                item.useTurn = false;
                item.UseSound = SoundID.Item7;
            }

            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            //damage = (int)(damage * .85f);
            float numberProjectiles = 3; //Main.rand.Next(3); // 3, 4, or 5 shots
            float rotation = MathHelper.ToRadians(15); // Spread degrees.
            position += Vector2.Normalize(new Vector2(speedX, speedY)) * 30f; // Distance spawned from player
            for (int i = 0; i < numberProjectiles; i++)
            {
                Vector2 perturbedSpeed = new Vector2(speedX, speedY).RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (numberProjectiles - 1))) * 1f; // The speed at which projectiles move.
                Projectile.NewProjectile(position.X, position.Y, perturbedSpeed.X, perturbedSpeed.Y, type, damage, knockBack, player.whoAmI);
            }
            return false;
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (Main.rand.Next(3) == 0) //quantity of particles
            {
                if (player.altFunctionUse == 2)
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 96/*ID of particle*/, 0f, 0f, 30, default(Color), 1.1f/*Size of particle */);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity.X += player.direction * 6f;
                    Main.dust[dust].velocity.Y += 0.2f;

                }
                else
                {
                    int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 96, 0f, 0f, 60, default(Color), 1.1f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }
        Texture2D glowTexture;
        Texture2D baseTexture;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (glowTexture == null || glowTexture.IsDisposed)
            {
                glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BarbarousThornBladeGlowmask];
            }
            if (baseTexture == null || baseTexture.IsDisposed)
            {
                baseTexture = Main.itemTexture[item.type];
            }

            spriteBatch.Draw(baseTexture, position, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), drawColor, 0f, origin, scale, SpriteEffects.None, 0.1f);
            spriteBatch.Draw(glowTexture, position, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, 0f, origin, scale, SpriteEffects.None, 0.1f);

            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (glowTexture == null || glowTexture.IsDisposed)
            {
                glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BarbarousThornBladeGlowmask];
            }
            if (baseTexture == null || baseTexture.IsDisposed)
            {
                baseTexture = Main.itemTexture[item.type];
            }

            spriteBatch.Draw(baseTexture, item.Center - Main.screenPosition, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), lightColor, rotation, new Vector2(item.width / 2, item.height / 2), item.scale, SpriteEffects.None, 0.1f);
            spriteBatch.Draw(glowTexture, item.Center - Main.screenPosition, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, rotation, new Vector2(item.width / 2, item.height / 2), item.scale, SpriteEffects.None, 0.1f);

            return false;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<Items.Weapons.Melee.YellowTail>());
            recipe.AddIngredient(ItemID.HellstoneBar, 10);
            recipe.AddIngredient(ModContent.ItemType<Items.DarkSoul>(), 6000);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
