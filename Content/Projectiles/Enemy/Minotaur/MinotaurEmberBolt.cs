using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Minotaur
{
    ///<summary>
    ///Minotaur Mage's bread-and-butter fire bolt: straight, mid-speed, sold by a torch-flame trail
    ///with a bright core. Applies a short On Fire!.
    ///</summary>
    class MinotaurEmberBolt : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
            Projectile.light = 0.5f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.dedServ) return;
            Projectile.localAI[0]++;

            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 80, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
            if (Main.rand.NextBool(3))
            {
                int core = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 100, default, 0.9f);
                Main.dust[core].noGravity = true;
                Main.dust[core].velocity *= 0.2f;
            }
            // One small white-hot spark every three ticks, peeling sideways/back from the moving head.
            // Entirely cosmetic: no extra projectiles or damage, and no server particle work.
            if (Projectile.localAI[0] % 3f == 0f)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 sideways = forward.RotatedBy(MathHelper.PiOver2);
                Dust spark = Dust.NewDustPerfect(Projectile.Center + forward * 3f
                    + Main.rand.NextVector2Circular(2f, 2f), DustID.WhiteTorch,
                    -forward * Main.rand.NextFloat(0.7f, 2.4f) + sideways * Main.rand.NextFloat(-1.4f, 1.4f),
                    30, Color.White, Main.rand.NextFloat(0.45f, 0.7f));
                spark.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.35f, 0.1f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 2 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.4f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 35; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, vel.X, vel.Y, 80, default, 1.4f);
                Main.dust[dust].noGravity = true;
            }
            for (int i = 0; i < 6; i++)
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 4.6f),
                    30, Color.White, Main.rand.NextFloat(0.45f, 0.7f));
                spark.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override void PostDraw(Color lightColor)
        {
            float fade = MathHelper.Clamp(Projectile.timeLeft / 15f, 0f, 1f);
            MinotaurFireVFX.Draw(Projectile.Center, new Vector2(20f, 32f), fade * 0.85f,
                Projectile.identity * 0.17f, Projectile.rotation - MathHelper.PiOver2, firebolt: true);
        }
    }
}
