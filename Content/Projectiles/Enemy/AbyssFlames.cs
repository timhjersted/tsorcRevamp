using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    class AbyssFlames : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.timeLeft = 600;
            Projectile.damage = 46;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.9f;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.alpha = 125;
        }
        // ai[0] = 1: short 120-tick lifetime. ai[1] = 1: collides with tiles (Corrupted Hornet); left 0 the flame passes
        // through rock on purpose (Serpent of the Abyss rains it from 400px above the player). localAI[0] = age in ticks.
        public override bool PreAI()
        {
            Projectile.localAI[0]++;
            float age = Projectile.localAI[0];

            // Only on the first tick: setting it every tick (as before) reset the countdown forever and the flame never expired.
            if (age == 1 && Projectile.ai[0] == 1)
            {
                Projectile.timeLeft = 120;
            }

            // tileCollide isn't synced, so it's derived each tick from the synced ai[1] on every machine.
            Projectile.tileCollide = Projectile.ai[1] == 1;

            // Wobble: up to 0.5°/tick of turn following a sine (heading swings about ±3°). Was a Main.rand turn per tick, which
            // drifted each client's copy to a different place. identity is synced, so flames in a volley wobble out of step.
            float wobbleDegrees = 0.5f * (float)Math.Sin(age * 0.15f + Projectile.identity);
            Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.ToRadians(wobbleDegrees));

            // Fade the last 15 ticks out from the base alpha 125 to invisible, so a flame that expires mid-air doesn't pop.
            if (Projectile.timeLeft <= 15)
            {
                float fade = 1f - Projectile.timeLeft / 15f;
                Projectile.alpha = (int)MathHelper.Lerp(125f, 255f, fade);
            }

            Projectile.rotation += 1f;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.PinkTorch, 0, 0, 50, default, 2.6f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 223, 0, 0, 50, default, 2.5f);
                Main.dust[dust].noGravity = true;
            }

            // Miasma accent: a violet/pink mote every 3 ticks in the Miasma meter's colours, so the flame reads as a Miasma source.
            if (!Main.dedServ && Projectile.localAI[0] % 3 == 0)
            {
                Color accentColor = MiasmaPlayer.MiasmaColor;
                if (Projectile.localAI[0] % 6 == 0)
                {
                    accentColor = MiasmaPlayer.MiasmaPinkColor;
                }

                Vector2 driftVelocity = -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.5f, 0.5f);
                Dust accent = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.RainbowMk2, driftVelocity, 60, accentColor, 1.1f);
                accent.noGravity = true;
            }

            if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
            {
                float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
                Projectile.velocity.X *= accel;
                Projectile.velocity.Y *= accel;
            }
            return true;
        }
        // 2 hits fill the 100-point Miasma meter; a flame-triggered proc drains for 20s instead of the standard 2 minutes.
        public const int MiasmaBuildupPerHit = 50;
        public const int MiasmaProcDurationTicks = 20 * 60;

        // Runs on the hit player's machine; MiasmaBuildup.Apply reports the gain to the server, which owns the meter.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), 4 * 60, false);
            MiasmaBuildup.Apply(target, MiasmaBuildupPerHit, MiasmaProcDurationTicks);

            // Hostile projectiles pierce players unless they kill themselves (vanilla never spends penetrate on a player hit).
            // In MP this kill happens on the victim's machine; tsorcGlobalProjectile relays it to the server.
            Projectile.Kill();
        }

        // Every death path bursts in the trail's colours: tile hit, player hit (OnHitPlayer kills it), or a small puff if it faded out.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            int burstCount = 24;
            if (timeLeft <= 0)
            {
                burstCount = 6;
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f }, Projectile.Center);
            }

            for (int i = 0; i < burstCount; i++)
            {
                Dust flame = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, Main.rand.NextVector2Circular(4f, 4f), 50, default, 2f);
                flame.noGravity = true;
            }

            for (int i = 0; i < burstCount / 2; i++)
            {
                Dust shadow = Dust.NewDustPerfect(Projectile.Center, DustID.FireworkFountain_Pink, Main.rand.NextVector2Circular(2.5f, 2.5f), 50, default, 1.8f);
                shadow.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.6f, 0.1f, 0.7f);
        }
    }
}
