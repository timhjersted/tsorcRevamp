using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // "Boomerang Crescent": arcs out in a wide curling path (continuously rotating its own
    // velocity), then switches to homing back toward the OWNER NPC's CURRENT position - a second,
    // separately-timed threat on the return leg for anyone who doesn't reposition after the first
    // pass. Reuses AbyssSlash's crescent sprite (same texture, different ModProjectile/behavior).
    //
    // ai[0] = curve direction (+1 curls one way, -1 the other). ai[1] = owner NPC's whoAmI (so the
    // return leg can re-target wherever the boss currently is, not just where it was at launch).
    class BoomerangCrescent : ModProjectile
    {
        const float Speed = 7f;
        const float CurveRatePerTick = 0.05f;
        const int OutboundArcTicks = 45;
        const float CatchDistance = 50f;
        const float ReturnHomingRate = 0.10f;

        float CurveDir => Projectile.ai[0];
        int OwnerIndex => (int)Projectile.ai[1];

        bool _returning;
        int _turnFlashTimer;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssSlash";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 120;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.scale = 0.55f;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.65f;
            Projectile.timeLeft = 280;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.15f, 0.85f));
            Animate();

            Projectile.localAI[0]++;
            float elapsed = Projectile.localAI[0];

            NPC owner = OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs ? Main.npc[OwnerIndex] : null;

            if (!_returning)
            {
                // Continuously rotating velocity traces the wide curling "out past the player" arc.
                Projectile.velocity = Projectile.velocity.RotatedBy(CurveRatePerTick * CurveDir);
                if (elapsed >= OutboundArcTicks)
                {
                    _returning = true;
                    _turnFlashTimer = 12;
                    if (!Main.dedServ)
                    {
                        SpawnTurnParticles();
                    }
                }
            }
            else if (owner != null && owner.active)
            {
                Vector2 toOwner = owner.Center - Projectile.Center;
                Vector2 desired = toOwner.SafeNormalize(Vector2.UnitY) * Speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, ReturnHomingRate);

                if (toOwner.Length() < CatchDistance)
                {
                    Projectile.Kill();
                }
            }

            Projectile.rotation += 0.18f * CurveDir;

            if (_turnFlashTimer > 0)
            {
                _turnFlashTimer--;
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
                Vector2 position = Projectile.Center
                    - direction * Main.rand.NextFloat(4f, 20f)
                    + perpendicular * Main.rand.NextFloat(-10f, 10f);
                Vector2 velocity = -direction * Main.rand.NextFloat(0.35f, 1.15f)
                    + perpendicular * Main.rand.NextFloat(-0.8f, 0.8f);
                Color tint = _returning
                    ? new Color(230, 70, 186)
                    : new Color(156, 82, 238);
                int dustType = Main.rand.NextBool(6)
                    ? DustID.SilverFlame
                    : Main.rand.NextBool(3) ? DustID.ShadowbeamStaff : DustID.PurpleTorch;
                Dust d = Dust.NewDustPerfect(position, dustType, velocity, 130, tint,
                    Main.rand.NextFloat(0.62f, 0.96f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float curveDirection = CurveDir < 0f ? -1f : 1f;

            // Low-opacity sprite ghosts make the true curved trajectory readable without implying
            // that the projectile's old positions remain damaging.
            for (int i = Projectile.oldPos.Length - 2; i >= 3; i -= 4)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                {
                    continue;
                }

                float history = 1f - i / (float)Projectile.oldPos.Length;
                Color ghostColor = (_returning
                    ? new Color(188, 38, 150, 0)
                    : new Color(92, 44, 188, 0)) * (0.16f + history * 0.16f);
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Main.EntitySpriteDraw(texture, oldCenter - Main.screenPosition, frame,
                    ghostColor, Projectile.oldRot[i], origin, 0.27f + history * 0.025f,
                    SpriteEffects.None, 0f);
            }

            // A mostly opaque sprite body keeps the damaging crescent readable even over the
            // darkest arena tiles. Additive shader layers below provide the animated magic.
            Color bodyColor = _returning
                ? new Color(198, 48, 170, 240)
                : new Color(126, 50, 218, 238);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
                bodyColor, Projectile.rotation, origin, 0.31f, SpriteEffects.None, 0f);

            // T_Windstreak3 is vertical in texture space, hence the +90-degree rotation here.
            ArtoriasVFX.DrawBoomerangRibbon(Projectile.Center - direction * 47f,
                direction.ToRotation() + MathHelper.PiOver2, new Vector2(34f, 126f),
                _returning, curveDirection, _returning ? 0.96f : 0.88f);
            ArtoriasVFX.DrawBoomerangOrbit(Projectile.Center, Vector2.One * (_returning ? 94f : 86f),
                Projectile.rotation * 0.32f, _returning, curveDirection, _returning ? 0.94f : 0.86f);
            ArtoriasVFX.DrawBoomerangCore(texture, frame, Projectile.Center, Projectile.rotation,
                new Vector2(78f, 74f), _returning, curveDirection, 0.70f, aura: true);
            ArtoriasVFX.DrawBoomerangCore(texture, frame, Projectile.Center, Projectile.rotation,
                new Vector2(54f, 52f), _returning, curveDirection, 1f, aura: false);

            if (_turnFlashTimer > 0)
            {
                float progress = 1f - _turnFlashTimer / 12f;
                ArtoriasVFX.DrawBoomerangPulse(Projectile.Center,
                    Vector2.One * MathHelper.Lerp(74f, 108f, progress), progress, 0.88f);
            }
            return false;
        }

        void SpawnTurnParticles()
        {
            Vector2 tangent = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 8; i++)
            {
                Vector2 radial = (MathHelper.TwoPi * i / 8f).ToRotationVector2();
                Vector2 velocity = radial * Main.rand.NextFloat(1.2f, 2.8f)
                    + tangent * Main.rand.NextFloat(-0.5f, 0.5f);
                Dust d = Dust.NewDustPerfect(Projectile.Center + radial * 12f,
                    i % 3 == 0 ? DustID.ShadowbeamStaff : DustID.PurpleTorch,
                    velocity, 90, new Color(225, 70, 190), Main.rand.NextFloat(0.7f, 1.05f));
                d.noGravity = true;
            }
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 140, 255, 220);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, tint, 1f);
                d.noGravity = true;
            }
        }
    }
}
