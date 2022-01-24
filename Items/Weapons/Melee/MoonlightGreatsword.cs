using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class MoonlightGreatsword : ModItem {


        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The Moonlight Greatsword, the sword of legend..." +
                                "\nGlows and gains magic damage scaling at night" +
                                "\nLaunches glimmering waves of moonlight");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Pink;
            item.damage = 2000;
            item.height = 72;
            item.width = 72;
            item.knockBack = 12f;
            item.melee = true;
            item.autoReuse = true;
            item.useAnimation = 27;
            item.useTime = 27;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 1000000;
            item.shoot = ModContent.ProjectileType<Projectiles.MLGSCrescent>();
            item.shootSpeed = 2f; //Yes it looks slow but it gets *1.2f each tick in it's AI. My attempt at making the sword look like it's not spawning in the player.
        }



        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {

            if (!Main.dayTime && player.magicDamage > 1)
            {
                damage = (int)(damage * player.magicDamage);
            }

            return true;

        }

        public override bool OnlyShootOnSwing => true;

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
        {
            if (!Main.dayTime && player.magicDamage > 1)
            {
                damage = (int)(damage * player.magicDamage);
            }
        }

        Texture2D glowTexture;
        Texture2D baseTexture;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (glowTexture == null || glowTexture.IsDisposed)
            {
                glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatswordGlowmask];
            }
            if (baseTexture == null || baseTexture.IsDisposed)
            {
                baseTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatsword];
            }
            spriteBatch.Draw(baseTexture, position, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), drawColor, 0f, origin, scale, SpriteEffects.None, 0.1f);
            if (!Main.dayTime)
            {
                spriteBatch.Draw(glowTexture, position, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, 0f, origin, scale, SpriteEffects.None, 0.1f);
            }
            
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (glowTexture == null || glowTexture.IsDisposed)
            {
                glowTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatswordGlowmask];
            }
            if (baseTexture == null || baseTexture.IsDisposed)
            {
                baseTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatsword];
            }
            spriteBatch.Draw(baseTexture, item.Center - Main.screenPosition, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), lightColor, rotation, new Vector2(item.width / 2, item.height / 2), item.scale, SpriteEffects.None, 0.1f);
            if (!Main.dayTime)
            {
                spriteBatch.Draw(glowTexture, item.Center - Main.screenPosition, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, rotation, new Vector2(item.width / 2, item.height / 2), item.scale, SpriteEffects.None, 0.1f);
            }

            return false;
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 89, player.velocity.X, player.velocity.Y, 100, default, .8f);
            Main.dust[dust].noGravity = true;
        }
    }
}
