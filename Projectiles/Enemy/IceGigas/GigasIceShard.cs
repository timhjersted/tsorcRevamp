using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Quara;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Small ice shard (vanilla ice-spike texture): the shrapnel of the Ice Gigas kit. Thrown by giant
    ///hailstone impacts, shattering statues, prison pillars and the stagger ice-shed.
    ///ai[0] = gravity per tick (0 = straight).
    ///</summary>
    class GigasIceShard : ModProjectile
    {
        // Quara-only modes. Cleric of Sorrow and Ice Gigas leave ai[1] at zero, retaining this
        // projectile's original straight/gravity behaviour exactly.
        const float QuaraGuidedThirty = 1f;
        const float QuaraOverflightThirty = 2f;
        const float QuaraGuidedSixty = 3f;
        const float QuaraWaterIgniter = 4f;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSpike;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 180;
            Projectile.scale = 0.9f;
            Projectile.light = 0.2f;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            float mode = Projectile.ai[1];
            if (mode == QuaraWaterIgniter)
            {
                CheckWaterResidue();
            }
            else if (mode == QuaraGuidedThirty || mode == QuaraGuidedSixty)
            {
                GuideToPlayer(mode == QuaraGuidedSixty ? 60 : 30);
            }
            else if (mode == QuaraOverflightThirty)
            {
                RunOverflightRoute();
            }

            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 14f)
            {
                Projectile.velocity.Y = 14f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Frost, 0f, 0f, 100, default, 0.9f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
        }

        void CheckWaterResidue()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile residue = Main.projectile[i];
                if (!residue.active || residue.type != ModContent.ProjectileType<QuaraWaterResidue>()
                    || !Projectile.Hitbox.Intersects(residue.Hitbox))
                {
                    continue;
                }
                (residue.ModProjectile as QuaraWaterResidue)?.Ignite();
                Projectile.Kill();
                return;
            }
        }

        void GuideToPlayer(int guideTicks)
        {
            if (Projectile.localAI[0] > guideTicks)
            {
                return; //the player can now read and dodge the locked trajectory
            }
            int targetIndex = (int)Projectile.ai[2];
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
            {
                return;
            }
            Player target = Main.player[targetIndex];
            if (!target.active || target.dead)
            {
                return;
            }
            float speed = MathHelper.Clamp(Projectile.velocity.Length(), 7f, 12f);
            Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.055f);
        }

        void RunOverflightRoute()
        {
            int targetIndex = (int)Projectile.ai[2];
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
            {
                return;
            }
            Player target = Main.player[targetIndex];
            if (!target.active || target.dead)
            {
                return;
            }
            float age = Projectile.localAI[0];
            if (age <= 26f)
            {
                return; //the launch velocity gives the clean initial climb above the target
            }
            if (age <= 52f)
            {
                //Continue past the player to the far side before the player-seeking portion begins.
                float behind = Math.Sign(Projectile.Center.X - target.Center.X);
                if (behind == 0f) behind = Math.Sign(Projectile.velocity.X);
                Vector2 routePoint = target.Center + new Vector2(behind * 72f, -64f);
                float speed = MathHelper.Clamp(Projectile.velocity.Length(), 7f, 12f);
                Vector2 desired = (routePoint - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.075f);
                return;
            }
            if (age <= 82f)
            {
                GuideToPlayer(82);
            }
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.3f, Pitch = 0.4f }, Projectile.Center);
            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ice, vel.X, vel.Y, 60, default, 1f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.ai[1] != QuaraGuidedThirty && Projectile.ai[1] != QuaraOverflightThirty)
            {
                return;
            }
            target.AddBuff(BuffID.Chilled, 9 * 60);
            if (target.HasBuff(BuffID.Wet))
            {
                target.AddBuff(BuffID.Frozen, 60);
                target.AddBuff(BuffID.Frostburn, 3 * 60);
            }
        }
    }
}
