using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Projectiles.Enemy.DarkrootSentrySpells
{
    /// <summary>
    /// Thorncast shot. A hard woody spike, point leading along velocity, 14px/t straight, no gravity or homing.
    /// Damage: a 24x6 oriented capsule around the spine (the 32x12 drawn envelope is only faint fraying outside it).
    /// Life: live 80t (1,120px), then a 12t harmless fade. Tile or player contact ends it with a bark/sap burst.
    /// </summary>
    public class DarkrootThorn : DarkrootHazard
    {
        public const float Speed = 14f;
        const int LiveTicks = 80;
        const int FadeTicks = 12;

        // Damage capsule: 24px long including its 3px rounded caps, 6px thick. Segment half-length = (24 - 6) / 2.
        const float CapsuleHalfSegment = 9f;
        const float CapsuleThickness = 6f;

        public override bool IsLive => Age < LiveTicks;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = LiveTicks + FadeTicks + 30;
            Projectile.DamageType = Terraria.ModLoader.DamageClass.Magic;
        }

        public override void Disarm()
        {
            if (Age >= LiveTicks)
            {
                return;
            }

            Age = LiveTicks;
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            DisarmIfOrphaned();
            Age++;
            Projectile.rotation = Projectile.velocity.ToRotation();

            bool fading = Age >= LiveTicks;
            if (fading)
            {
                // Harmless fade: slow to a stop so the dissolve reads in place instead of flying on invisibly.
                Projectile.velocity *= 0.8f;
            }

            if (Age >= LiveTicks + FadeTicks)
            {
                Projectile.Kill();
                return;
            }

            if (!Main.dedServ && !fading && (int)Age % 3 == 0)
            {
                Dust leaf = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f, DustID.GrassBlades, -Projectile.velocity * 0.05f, 100, default, 0.7f);
                leaf.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 spineDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 spineBack = Projectile.Center - spineDirection * CapsuleHalfSegment;
            Vector2 spineFront = Projectile.Center + spineDirection * CapsuleHalfSegment;
            return MeleeBladeCollision.SegmentIntersectsRect(spineBack, spineFront, CapsuleThickness, targetHitbox);
        }

        // Hostile shots pierce players in vanilla; Kill here is relayed to the server by tsorcGlobalProjectile in MP.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || Age >= LiveTicks + FadeTicks)
            {
                return;
            }

            // Restrained 24-mote bark/sap burst and a quiet crack, at the point.
            Vector2 point = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 12f;
            DarkrootWoodVFX.Motes(point, 16, 6f, false, DustID.t_LivingWood);
            DarkrootWoodVFX.Motes(point, 8, 4f, false, DustID.GrassBlades);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f, Pitch = 0.3f }, point);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fade = 1f;
            if (Age >= LiveTicks)
            {
                fade = MathHelper.Clamp(1f - (Age - LiveTicks) / FadeTicks, 0f, 1f);
            }

            // 32x12 quad centred on the projectile; uv.x runs tail (0) to point (1) along velocity.
            DarkrootWoodVFX.Draw("DarkrootThorn", Projectile.Center, new Vector2(32f, 12f), Projectile.rotation, new Vector2(0.5f, 0.5f), 1f, IsLive ? 1f : 0f, fade);
            return false;
        }
    }
}
