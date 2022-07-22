using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class MoonlightGreatsword : ModItem
    {


        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The Moonlight Greatsword, the sword of legend..." +
                                "\nGlows and gains piercing projectiles at night" +
                                "\nScales with your magic or melee stats, whichever is higher" +
                                "\nLaunches glimmering waves of moonlight");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Pink;
            Item.damage = 727;
            Item.height = 72;
            Item.width = 72;
            Item.knockBack = 12f;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useAnimation = 27;
            Item.useTime = 27;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 1000000;
            Item.shoot = ModContent.ProjectileType<Projectiles.MLGSCrescent>();
            Item.shootSpeed = 2f; //Yes it looks slow but it gets *1.2f each tick in it's AI. My attempt at making the sword look like it's not spawning in the player.
        }


        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.GetTotalDamage(DamageClass.Magic).ApplyTo(100) < player.GetTotalDamage(DamageClass.Melee).ApplyTo(100))
            {
                Item.DamageType = DamageClass.Melee;
            }
            else
            {
                Item.DamageType = DamageClass.Magic;
            }
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
        {
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
            spriteBatch.Draw(baseTexture, Item.Center - Main.screenPosition, new Rectangle(0, 0, baseTexture.Width, baseTexture.Height), lightColor, rotation, new Vector2(Item.width / 2, Item.height / 2), Item.scale, SpriteEffects.None, 0.1f);
            if (!Main.dayTime)
            {
                spriteBatch.Draw(glowTexture, Item.Center - Main.screenPosition, new Rectangle(0, 0, glowTexture.Width, glowTexture.Height), Color.White, rotation, new Vector2(Item.width / 2, Item.height / 2), Item.scale, SpriteEffects.None, 0.1f);
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
