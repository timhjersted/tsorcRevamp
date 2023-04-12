using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles
{
    class SuperBlasterShot : ModProjectile
    {
        public override void SetDefaults()
        {

            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.aiStyle = 0;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 42;
            //These 2 help the projectile hitbox be centered on the projectile sprite.
            DrawOffsetX = -2;
            DrawOriginOffsetY = -2;
        }


        // public override string GlowTexture => Texture + "ChromasMod/Projectiles/protoshot_Glow";

        public override void AI()
        {
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 111, 0, 0, 30, default(Color), .85f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= .5f;

            }
            // Rotation increased by velocity.X 
            Projectile.rotation += Projectile.velocity.X * 0.08f;

            if (Projectile.localAI[0] == 0f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.7f, Pitch = -0.7f }, Projectile.Center);
                Projectile.localAI[0] += 1f;
            }

            Lighting.AddLight(Projectile.position, 0.06f, 0.3f, 0.3f);

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 6)
            {
                Projectile.alpha += 25;

                if (Projectile.alpha > 255)
                {
                    Projectile.alpha = 225;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.8f, Pitch = -0.25f }, Projectile.Center);
            for (int d = 0; d < 30; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 111, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), .75f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.8f, Pitch = -0.25f }, Projectile.Center);
            for (int d = 0; d < 30; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 111, Projectile.velocity.X * 1f, Projectile.velocity.Y * 1f, 30, default(Color), .75f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].noGravity = true;

            }
            return true;

        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.4f, Pitch = -0.25f }, Projectile.Center);
            for (int d = 0; d < 30; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 111, Projectile.velocity.X * 1.2f, Projectile.velocity.Y * 1.2f, 30, default(Color), .75f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].noGravity = true;

            }
        }

    }
}
