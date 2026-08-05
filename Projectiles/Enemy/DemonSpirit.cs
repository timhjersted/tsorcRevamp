using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class DemonSpirit : ModProjectile
    {
        const int ExtraLifetime = 0;
        const int HomingCutoffTime = 60;
        const float ExplosionDamageRadius = 92f;
        const float ExplosionDustRadius = 128f;
        const float ArcOffset = 160f;

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 46;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 320;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.damage = 20;
            Projectile.friendly = false;
            Projectile.penetrate = 3;
            Projectile.scale = 0.97f;
            Projectile.alpha = 70;
            Projectile.light = .7f;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Demon Spirit");
            Main.projFrames[Type] = 6;
        }

        public override bool PreKill(int timeLeft)
        {
            Vector2 explosionCenter = Projectile.Center;
            EnemyShaderBurst.Spawn(Projectile.GetSource_Death(), explosionCenter, EnemyVFXBurstKind.DemonSpiritSoulBurst);
            Projectile.position = explosionCenter - new Vector2(ExplosionDamageRadius);
            Projectile.width = (int)(ExplosionDamageRadius * 2f);
            Projectile.height = (int)(ExplosionDamageRadius * 2f);
            Projectile.penetrate = 10;
            Projectile.Damage();
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f }, explosionCenter);

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), explosionCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 280, 18);
            }

            for (int i = 0; i < 44; i++)
            {
                Vector2 dustPosition = explosionCenter + Main.rand.NextVector2Circular(ExplosionDustRadius, ExplosionDustRadius);
                Vector2 dustVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.7f, 3.4f);
                Dust dust = Dust.NewDustPerfect(dustPosition, DustID.ShadowbeamStaff, dustVelocity, 160, default, 0.7f);
                dust.noGravity = true;
            }

            for (int i = 0; i < 60; i++)
            {
                Vector2 dustPosition = explosionCenter + Main.rand.NextVector2Circular(ExplosionDustRadius, ExplosionDustRadius);
                Vector2 dustVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.8f);
                Dust smoke = Dust.NewDustPerfect(dustPosition, DustID.Smoke, dustVelocity, 140, Color.DarkViolet, Main.rand.NextFloat(1.4f, 2.2f));
                smoke.noGravity = true;
            }

            for (int i = 0; i < 50; i++)
            {
                Vector2 dustPosition = explosionCenter + Main.rand.NextVector2Circular(ExplosionDustRadius, ExplosionDustRadius);
                Vector2 dustVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 5f);
                Dust flame = Dust.NewDustPerfect(dustPosition, DustID.Torch, dustVelocity, 120, Color.Magenta, Main.rand.NextFloat(1.2f, 2.4f));
                flame.noGravity = true;
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 dustVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 6f);
                Dust firework = Dust.NewDustPerfect(explosionCenter, DustID.FireworkFountain_Red, dustVelocity, 120, Color.Violet, Main.rand.NextFloat(0.9f, 1.4f));
                firework.noGravity = true;
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    Gore.NewGore(Projectile.GetSource_Death(), explosionCenter - new Vector2(24f), Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.Next(61, 64), Main.rand.NextFloat(0.7f, 1.1f));
                }
            }

            return false;
        }

        Player targetPlayer;

        public override void AI()
        {
            Animate();

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.timeLeft += ExtraLifetime;
                Projectile.localAI[0] = Projectile.timeLeft;
                Projectile.localAI[1] = Projectile.identity % 3 - 1;
            }

            Color color = new Color();
            float dustScale = Projectile.timeLeft <= 30 ? 1f + (30f - Projectile.timeLeft) / 30f * 0.8f : 1f;
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y - 10), Projectile.width, Projectile.height, DustID.CrystalPulse, 0, 0, 160, color, dustScale);
            Main.dust[dust].noGravity = true;

            this.Projectile.ai[0] += 1f;


            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + 1.57f;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
                return;
            }

            #region Homing Code
            Vector2 move = Vector2.Zero;
            float distance = 900f;
            bool target = false;
            float speed = 4.5f; // was 2.25f — doubled, the homing spirit read as non-threatening at the old speed
            if (!target)
            {
                int targetIndex = GetClosestPlayer();
                if (targetIndex != -1)
                {
                    targetPlayer = Main.player[targetIndex];
                    target = true;
                }
            }

            if (target && Projectile.timeLeft > HomingCutoffTime)
            {
                Vector2 targetPoint = targetPlayer.Center;
                if (Projectile.ai[0] < Projectile.localAI[0] / 2f)
                {
                    targetPoint.Y += Projectile.localAI[1] * ArcOffset;
                }

                Vector2 newMove = targetPoint - Projectile.Center;
                float distanceTo = (float)Math.Sqrt(newMove.X * newMove.X + newMove.Y * newMove.Y);
                if (distanceTo < distance)
                {
                    move = newMove;
                    distance = distanceTo;
                }

                Projectile.velocity.X = (move.X / distance) * speed;
                Projectile.velocity.Y = (move.Y / distance) * speed;
            }
            #endregion
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float expiryProgress = Projectile.timeLeft <= 30
                ? MathHelper.Clamp((30f - Projectile.timeLeft) / 30f, 0f, 1f)
                : 0f;
            EnemyVFX.DrawDemonSpiritSoulComet(Projectile.Center, Projectile.velocity, expiryProgress);
            return true;
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
        }

        private int GetClosestPlayer()
        {
            int closest = -1;
            float distance = 9999;
            for (int i = 0; i < Main.player.Length; i++)
            {
                if (!Main.player[i].active || Main.player[i].dead)
                {
                    continue;
                }

                Vector2 diff = Main.player[i].Center - Projectile.Center;
                float distanceTo = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
                if (distanceTo < distance)
                {
                    distance = distanceTo;
                    closest = i;
                }
            }
            return closest;
        }
    }
}
