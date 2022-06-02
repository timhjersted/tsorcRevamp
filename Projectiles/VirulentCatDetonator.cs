using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class VirulentCatDetonator : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {

            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            Projectile.width = 18;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 92;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;

            //These 2 help the projectile hitbox be centered on the projectile sprite.
            DrawOffsetX = 0;
            DrawOriginOffsetY = 0;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 18, 12), Color.White, Projectile.rotation, new Vector2(9, 6), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.Center.X, (int)Projectile.Center.Y, SoundID.Item91.Style, .7f, -0.5f);
                Projectile.localAI[0] += 1f;
            }

            Lighting.AddLight(Projectile.position, 0.455f, 0.826f, 0.238f);
            Projectile.rotation = Projectile.velocity.ToRotation(); // projectile faces sprite right

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft >= 89 && Projectile.timeLeft <= 90)
            {
                for (int d = 0; d < 6; d++)
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(Projectile.position.X - 7, Projectile.position.Y - 7), 24, 24, 75, Projectile.velocity.X * .5f, Projectile.velocity.Y * .5f, 100, default(Color), .8f)];
                    dust.noGravity = true;
                }
            }

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft < 91)
            {
                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 75, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath9.WithVolume(.8f));
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 75, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), 1f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath9.WithVolume(.8f));
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 75, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), 1f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].noGravity = true;

            }
            return true;

        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath9.WithVolume(.4f));
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 75, Projectile.velocity.X * 1.2f, Projectile.velocity.Y * 1.2f, 30, default(Color), 1f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].noGravity = true;

            }
        }

    }
}
