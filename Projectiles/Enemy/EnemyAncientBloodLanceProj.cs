using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Projectiles.Enemy
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
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                int dustType = Main.rand.NextBool(2) ? DustID.Blood : DustID.Wraith;
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(14f, 14f);
                Vector2 dustVel = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.4f, 1.8f) + Main.rand.NextVector2Circular(0.5f, 0.5f);
                Dust dust = Dust.NewDustPerfect(dustPos, dustType, dustVel, 100, default, Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), 600);
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
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.Blood, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.2f);
                dust.noGravity = true;
            }
            Projectile.active = false;
        }
    }
}
