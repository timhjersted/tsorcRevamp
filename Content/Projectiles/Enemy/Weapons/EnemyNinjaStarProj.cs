using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Hostile grey ninja star thrown by the Black Ninja invader.  Flies straight, then STICKS into
    /// whatever it hits and lingers ~3 s as a contact hazard before popping in a small spark/smoke
    /// puff with a little AoE. (The abyssal ninjas keep <see cref="PuppetThrowingStar"/>.)
    /// </summary>
    public class EnemyNinjaStarProj : ModProjectile
    {

        // ai[0]: 0 = flying, 1 = stuck.   ai[1]: ticks remaining while stuck.
        private bool Stuck => Projectile.ai[0] == 1f;
        private bool _exploded;

        private const int StuckDurationTicks = 180; // 3 s
        private const float ExplodeAoeRadius = 44f;
        // The texture has four-way rotational symmetry. A very fast spin aliases into a stationary
        // wobble at gameplay scale, so keep the angular step small and reinforce it with two faint
        // rotation-aware echoes in PreDraw.
        private const float FlightSpinRadiansPerTick = 0.16f; // ~9.2°/tick, about 1.5 revolutions/second

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 11;
            Projectile.height = 11;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;     // persists so it can stick + keep hurting on contact
            Projectile.timeLeft = 600;
            Projectile.tileCollide = true;
            Projectile.scale = 0.425f;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            if (Stuck)
            {
                Projectile.velocity = Vector2.Zero;
                // Occasional glint while it sits embedded.
                if (Main.rand.NextBool(10))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        DustID.Silver, Vector2.Zero, 150, default, Main.rand.NextFloat(0.6f, 1.0f));
                    d.noGravity = true;
                }
                if (--Projectile.ai[1] <= 0f)
                    Explode();
                return;
            }

            // Flying: lock the spin handedness once, so vertical throws still visibly rotate.
            // localAI is cosmetic; every client receives the same initial velocity and makes the
            // same choice. The stuck branch above returns before this line, freezing the embedded star.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = Projectile.velocity.X < 0f ? -1f : 1f;
            }
            Projectile.rotation += Projectile.localAI[0] * FlightSpinRadiansPerTick;
            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(5f), 1, 1, DustID.Smoke,
                    Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 150, Color.LightGray, 0.9f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            if (!Stuck)
            {
                for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                    {
                        continue;
                    }

                    float opacity = 0.18f * (1f - i / (float)Projectile.oldPos.Length);
                    Main.EntitySpriteDraw(
                        texture,
                        Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                        null,
                        lightColor * opacity,
                        Projectile.oldRot[i],
                        origin,
                        Projectile.scale,
                        SpriteEffects.None,
                        0);
                }
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!Stuck)
                Stick(oldVelocity);
            return false; // never bounce / die from a tile — it embeds
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // A direct hit on the player pops it immediately; a star already stuck in terrain just
            // keeps chipping on contact until its timer runs out.
            if (!Stuck)
                Explode();
        }

        private void Stick(Vector2 oldVelocity)
        {
            Projectile.ai[0] = 1f;
            Projectile.ai[1] = StuckDurationTicks;
            // Nudge a few px into the surface it hit so it reads as embedded.
            Projectile.Center += oldVelocity.SafeNormalize(Vector2.Zero) * 4f;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, PitchVariance = 0.2f }, Projectile.Center); // embeds with a thud
        }

        private void Explode()
        {
            if (_exploded)
                return;
            _exploded = true;

            SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.5f, PitchVariance = 0.3f }, Projectile.Center);

            for (int i = 0; i < 30; i++) // sparks
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    Main.rand.NextVector2Circular(2.6f, 2.6f), 0, default, Main.rand.NextFloat(0.8f, 1.4f));
                d.noGravity = true;
            }
            for (int i = 0; i < 27; i++) // smoke
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    Main.rand.NextVector2Circular(1.7f, 1.7f), 120, Color.Gray, Main.rand.NextFloat(1.2f, 2.1f));
                d.noGravity = true;
            }

            // Little AoE on the pop.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int aoeDamage = Math.Max(1, Projectile.damage / 2);
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active && !p.dead && !p.immune && p.Distance(Projectile.Center) <= ExplodeAoeRadius)
                    {
                        int dir = p.Center.X < Projectile.Center.X ? -1 : 1;
                        p.Hurt(PlayerDeathReason.ByProjectile(p.whoAmI, Projectile.whoAmI), aoeDamage, dir);
                    }
                }
            }

            Projectile.Kill();
        }
    }
}
