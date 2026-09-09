using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Straight-line crescent slash, fired toward wherever the player was at release (no homing).
    // The source sprite is a plain white 4-frame crescent (170x170/frame) - tinted purple via
    // GetAlpha, scaled down small ("tiny"), with a purple point light and dust trail.
    class AbyssSlash : ModProjectile
    {
        bool UsesFanBoomerangVFX => Projectile.ai[1] >= 0.5f;

        float _dustOrbitAngle;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssSlash";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 120;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.scale = 0.35f;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.6f;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.1f);
            if (UsesFanBoomerangVFX)
            {
                // The fan variant carries the boomerang shader stack (purple magic) on top of the
                // plain white crescent light above - casts both instead of replacing one.
                Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.15f, 0.85f));
            }

            Animate();

            if (!Main.dedServ && Main.rand.NextBool(UsesFanBoomerangVFX ? 2 : 3))
            {
                bool white = Main.rand.NextBool(UsesFanBoomerangVFX ? 6 : 5);
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    white ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.12f), 110,
                    white ? new Color(232, 226, 255) : new Color(150, 48, 228),
                    Main.rand.NextFloat(0.72f, 1.02f));
                d.noGravity = true;
            }

            if (UsesFanBoomerangVFX)
            {
                SpawnOrbitingDust();
            }
        }

        /// <summary>Extra purple motes circling INSIDE the boomerang shader silhouette (Orbit draws
        /// out to ~92px across - this stays well within that, ~10-22px out from center), fan variant
        /// only. Mirrors BoomerangCrescent's own SpawnOrbitingDust so the two attacks that share this
        /// shader stack read consistently.</summary>
        void SpawnOrbitingDust()
        {
            if (Main.dedServ)
            {
                return;
            }

            _dustOrbitAngle += 0.30f;
            if (!Main.rand.NextBool(2))
            {
                return;
            }

            float radius = Main.rand.NextFloat(10f, 22f);
            Vector2 radial = _dustOrbitAngle.ToRotationVector2();
            Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2);
            Vector2 position = Projectile.Center + radial * radius;
            Vector2 velocity = tangent * Main.rand.NextFloat(0.9f, 1.7f);
            Dust d = Dust.NewDustPerfect(position, DustID.PurpleTorch, velocity, 110,
                new Color(170, 70, 235), Main.rand.NextFloat(0.55f, 0.85f));
            d.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rotation = direction.ToRotation();

            if (UsesFanBoomerangVFX)
            {
                Texture2D texture = TextureAssets.Projectile[Type].Value;
                Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
                Vector2 origin = frame.Size() * 0.5f;

                for (int i = Projectile.oldPos.Length - 2; i >= 3; i -= 4)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;

                    float history = 1f - i / (float)Projectile.oldPos.Length;
                    Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                    Main.EntitySpriteDraw(texture, oldCenter - Main.screenPosition, frame,
                        new Color(94, 30, 176, 110) * (0.34f + history * 0.22f),
                        Projectile.oldRot[i], origin, 0.27f, SpriteEffects.None, 0f);
                }

                // The alpha-blended body guarantees a legible projectile silhouette; the shader
                // layers add motion and breakup without being solely responsible for visibility.
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame,
                    new Color(124, 46, 210, 235), rotation, origin, 0.31f,
                    SpriteEffects.None, 0f);
                ArtoriasVFX.DrawBoomerangRibbon(Projectile.Center - direction * 40f,
                    rotation + MathHelper.PiOver2, new Vector2(38f, 116f),
                    returning: false, curveDirection: 1f, opacity: 0.88f);
                ArtoriasVFX.DrawBoomerangOrbit(Projectile.Center, Vector2.One * 92f,
                    rotation * 0.28f, returning: false, curveDirection: 1f, opacity: 0.86f);
                ArtoriasVFX.DrawBoomerangCore(texture, frame, Projectile.Center, rotation,
                    new Vector2(76f, 72f), returning: false, curveDirection: 1f,
                    opacity: 0.68f, aura: true);
                ArtoriasVFX.DrawBoomerangCore(texture, frame, Projectile.Center, rotation,
                    new Vector2(54f, 52f), returning: false, curveDirection: 1f,
                    opacity: 1f, aura: false);
                return false;
            }

            ArtoriasVFX.DrawProjectileTrail(Projectile.Center - direction * 38f, rotation,
                new Vector2(88f, 30f), 0.35f, 0.58f);
            float animationProgress = (Projectile.frame + Projectile.frameCounter / 4f)
                / Main.projFrames[Type];
            // 94x110 = the old 78x92 at +20%, matching the approved C-shaped arc revamp
            // (ArtoriasSwordSwipe.fx - see the offline preview harness's abyss_slash_arc FOCUS).
            ArtoriasVFX.DrawSwordSwipe(Projectile.Center, rotation,
                new Vector2(94f, 110f), animationProgress, 0.94f);
            return false;
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(190, 90, 255, 220);
        }

        // ai[0] holds the firing NPC's whoAmI + 1 (0 = "no owner") so the dodge-punish-chain system
        // knows this swipe actually connected. The +1 offset exists because this projectile is ALSO
        // reused anonymously (no owner) by Spiral Fan's straight-shot bursts, which have nothing to
        // do with that system and must not accidentally report a hit against whatever NPC happens
        // to occupy slot 0. ai[1] is visual-only: Spiral Fan sets it to 1 to request the richer
        // boomerang shader stack without changing this projectile's straight path or collision.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int ownerIdx = (int)Projectile.ai[0] - 1;
            if (ownerIdx >= 0 && ownerIdx < Main.maxNPCs && Main.npc[ownerIdx].active
                && Main.npc[ownerIdx].ModNPC is PuppetNPC invader)
            {
                invader.ReportAttackHit();
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, default, 1f);
                d.noGravity = true;
            }
        }
    }
}
