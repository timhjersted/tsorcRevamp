using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class EnemyAncientBloodLanceProj : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = 0.8f;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            // On-brand Destined Death red & black trail: 3 interpolated motes/tick along the velocity
            // step (gap <= 4.5px at 13.25px/t) — 2 crimson RedTorch + 1 dark Shadowflame (noLight).
            if (!Main.dedServ)
            {
                Vector2 backwards = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                for (int i = 0; i < 3; i++)
                {
                    float fraction = (i + 0.5f) / 3f;
                    bool soot = i == 1;
                    Vector2 dustPos = Projectile.Center - Projectile.velocity * fraction
                        + Main.rand.NextVector2Circular(5f, 5f);
                    Vector2 dustVel = backwards * Main.rand.NextFloat(0.6f, 2.2f)
                        + Main.rand.NextVector2Circular(0.6f, 0.6f);
                    Dust dust = Dust.NewDustPerfect(
                        dustPos,
                        soot ? DustID.Shadowflame : DustID.RedTorch,
                        dustVel,
                        soot ? 70 : 45,
                        soot ? new Color(18, 4, 10) : new Color(206, 16, 34),
                        soot ? Main.rand.NextFloat(0.95f, 1.35f) : Main.rand.NextFloat(0.85f, 1.35f));
                    dust.noGravity = true;
                    dust.noLight = soot;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), Buffs.Debuffs.DestinedDeath.DefaultDurationTicks);
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[2] == 1f)
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                EnemyVFX.DrawBlackKnightSpearWake(Projectile.Center - direction * 28f,
                    direction.ToRotation(), new Vector2(84f, 18f), 0.66f);
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawOrigin = new Vector2(54f, 54f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation + MathHelper.PiOver4;
            Main.EntitySpriteDraw(texture, drawPos, null, Projectile.GetAlpha(lightColor),
                rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * (MathHelper.TwoPi / 16f);
                    Vector2 velocity = angle.ToRotationVector2() * 2.6f;
                    float dir = MathF.Cos(angle) < 0f ? -1f : 1f;
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, velocity,
                        ModContent.ProjectileType<DestinedDeathBlaze>(), 0, 0f, Main.myPlayer,
                        dir, 1f);
                }

                if (Projectile.ai[2] == 1f)
                {
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer,
                        (float)RedKnightBurstKind.SpearImpact, 0.85f);
                }
            }
            if (!Main.dedServ)
            {
                // 3-layer Destined Death impact burst: crimson body + dark soot + fast sparks + billowing smoke.
                for (int i = 0; i < 24; i++)
                {
                    bool soot = i % 3 == 0;
                    Dust body = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                        soot ? DustID.Shadowflame : DustID.RedTorch,
                        Main.rand.NextVector2Circular(4.2f, 4.2f),
                        soot ? 70 : 45,
                        soot ? new Color(18, 4, 10) : new Color(206, 16, 34),
                        soot ? Main.rand.NextFloat(0.95f, 1.4f) : Main.rand.NextFloat(0.9f, 1.4f));
                    body.noGravity = true;
                    body.noLight = soot;
                }

                for (int i = 0; i < 12; i++)
                {
                    Vector2 sparkDir = Main.rand.NextVector2Unit();
                    Dust spark = Dust.NewDustPerfect(
                        Projectile.Center + sparkDir * Main.rand.NextFloat(2f, 8f),
                        DustID.RedTorch,
                        sparkDir * Main.rand.NextFloat(5.5f, 9.5f),
                        20,
                        new Color(255, 65, 80),
                        Main.rand.NextFloat(0.55f, 0.85f));
                    spark.noGravity = true;
                }

                for (int i = 0; i < 8; i++)
                {
                    Dust smoke = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                        DustID.Smoke,
                        Main.rand.NextVector2Circular(1.6f, 1.6f),
                        160,
                        new Color(22, 10, 14),
                        Main.rand.NextFloat(0.55f, 0.8f));
                    smoke.noGravity = true;
                    smoke.noLight = true;
                    smoke.fadeIn = Main.rand.NextFloat(1.3f, 1.65f);
                }
            }
            Projectile.active = false;
        }
    }
}
