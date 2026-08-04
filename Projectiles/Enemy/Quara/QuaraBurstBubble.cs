using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///The Hydromancer's burst bubble: one big, slow bubble (a shimmering dust shell) drifting after
    ///the player with lazy homing. Pops on proximity, tile contact or timeout into a radial spray
    ///of droplets — the payoff of its long, interruptible cast.
    ///</summary>
    class QuaraBurstBubble : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float PopRange = 48f;
        const float DriftSpeed = 2.6f;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 240;
            Projectile.light = 0.35f;
        }

        public override void AI()
        {
            Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            if (target != null && !target.dead)
            {
                UsefulFunctions.SmoothHoming(Projectile, target.Center, 0.05f, DriftSpeed, target.velocity, false);
                //Pop in the player's face
                if (Vector2.Distance(target.Center, Projectile.Center) < PopRange)
                {
                    Projectile.Kill();
                    return;
                }
            }

            //The shell: a shimmering ring of water dust + the occasional inner gleam
            for (int i = 0; i < 3; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * 17f;
                int dust = Dust.NewDust(pos, 2, 2, DustID.Water, 0f, 0f, 60, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
            }
            if (Main.rand.NextBool(4))
            {
                int gleam = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BubbleBurst_Blue, 0f, -0.4f, 100, default, 0.9f);
                Main.dust[gleam].noGravity = true;
                Main.dust[gleam].velocity *= 0.2f;
            }
            Lighting.AddLight(Projectile.Center, 0.2f, 0.35f, 0.55f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / 240f;
            EnemyVFX.DrawQuaraBubble(Projectile.Center, Vector2.One * 38f, progress, true);
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Wet, 5 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            EnemyShaderBurst.Spawn(Projectile.GetSource_Death(), Projectile.Center, EnemyVFXBurstKind.QuaraWaterBurst);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.7f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 16; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Water, vel.X, vel.Y, 50, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
            //The burst: six droplets radially, biased upward so they rain back down
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi * i / 6f + Main.rand.NextFloat(0.3f)).ToRotationVector2() * 5f;
                    vel.Y -= 1.5f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
                        ModContent.ProjectileType<QuaraDroplet>(), (int)(Projectile.damage * 0.6f), 1f, Main.myPlayer, 0.15f);
                }
            }
        }
    }
}
