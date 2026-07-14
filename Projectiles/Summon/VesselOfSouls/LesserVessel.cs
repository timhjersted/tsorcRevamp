using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;

namespace tsorcRevamp.Projectiles.Summon.VesselOfSouls
{
    ///<summary>
    ///Lesser Vessel — a floating soul-eye minion (a shard of the Vessel of Souls). Hovers/drifts like a
    ///demon eye via the reusable <see cref="FloaterAI"/> (flying A* around terrain, no wall-passing, keeps
    ///line-of-sight so its homing soul-skulls never spawn in walls). Uses the SoulVessel 6-frame sheet
    ///(top 3 = mouth closed, bottom 3 = mouth open when firing).
    ///</summary>
    class LesserVessel : ModProjectile
    {
        // Flip to false to use the SoulVessel 6-frame sprite (mouth open/closed) instead of StrangeEye.
        static readonly bool UseStrangeEyeSprite = true;
        // StrangeEye: rotate ONE frame smoothly to aim (no jittery 9-frame swapping). Tune the offset if
        // the pupil doesn't point at the target; set false to fall back to the (smoothed) frame-swap.
        static readonly bool RotateStrangeEyeGaze = true;
        const int StrangeEyeFixedFrame = 1;
        const float StrangeEyeGazeOffset = 0f;
        // SoulVessel frame is 120×181 (drop to 0.5f if too big); StrangeEye is 48×48 (1f is right).
        const float SpriteScale = 1f;

        public override string Texture => UseStrangeEyeSprite
            ? "tsorcRevamp/Projectiles/Enemy/VesselOfSouls/StrangeEye"
            : "tsorcRevamp/NPCs/Bosses/VesselOfSouls/SoulVessel";

        public int lifetime = 9000; // 2.5 minutes in ticks
        int shootTimer;
        int frameTimer;
        int frameIndex;
        bool mouthOpen;
        float gazeAngle;
        Vector2 lastPos;
        int hardStuck;
        int phaseTimer;
        readonly FloaterAI.FloaterState floatState = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = UseStrangeEyeSprite ? 9 : 6;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 16; // longer, denser purple shadow trail
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;   // 1×1 tile — nimble terrain navigation (FloaterAI footprint reads this)
            Projectile.height = 16;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;   // floaters don't pass walls; FloaterAI routes around
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Summon;
        }

        // Half-size tile-collision box so it slips through tight gaps; passes through platforms.
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThroughPlatforms, ref Vector2 hitboxCenterFrac)
        {
            width = 16;
            height = 16;
            fallThroughPlatforms = true;
            return true;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;
        public override bool OnTileCollide(Vector2 oldVelocity) => false; // don't die on walls; just get stopped

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!CheckActive(player)) return;

            lifetime--;
            if (lifetime <= 0) { Projectile.Kill(); return; }

            Lighting.AddLight(Projectile.Center, 0.35f, 0.1f, 0.45f);

            NPC target = FindTarget(player);
            float distToPlayer = Vector2.Distance(Projectile.Center, player.Center);
            mouthOpen = false;

            // Leash: if it strays absurdly far (bad path / big teleport), snap home.
            if (distToPlayer > 2200f)
            {
                Projectile.Center = player.Center;
                Projectile.velocity = Vector2.Zero;
                floatState.Path = null;
            }

            // Stuck escape: phase through walls for a beat, or teleport home if that fails.
            if (phaseTimer > 0) phaseTimer--;
            Projectile.tileCollide = phaseTimer <= 0;
            bool phasing = phaseTimer > 0;

            if (target != null && distToPlayer < 1400f)
            {
                bool los = FloaterAI.Run(Projectile, target, floatState,
                    topSpeed: 6.5f, accel: 0.12f, hoverMin: 210f, hoverMax: 430f, navRadius: 30, bobAmplitude: 10f, keepLOS: !phasing);
                FaceX(target.Center.X);

                float dist = Vector2.Distance(Projectile.Center, target.Center);
                if (los && dist < 650f)
                {
                    mouthOpen = true;
                    shootTimer++;
                    if (shootTimer >= 45 && Main.myPlayer == Projectile.owner)
                    {
                        shootTimer = 0;
                        Vector2 vel = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 9f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
                            ModContent.ProjectileType<LesserVesselSkull>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0.03f);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.45f, Pitch = 0.3f }, Projectile.Center);
                    }
                }
                else shootTimer = Math.Min(shootTimer + 1, 45);
            }
            else
            {
                // No target: hover near the player — slow and calm (low speed, little drift).
                FloaterAI.Run(Projectile, player, floatState,
                    topSpeed: 3.2f, accel: 0.06f, hoverMin: 70f, hoverMax: 150f, navRadius: 30, bobAmplitude: 4f, keepLOS: false, driftAmount: 7f);
                FaceX(player.Center.X);
                shootTimer = Math.Min(shootTimer + 1, 45);
            }

            // Stuck detection: barely moving while it wants to → phase out, then teleport if still stuck.
            bool wantsMove = target != null || distToPlayer > 70f;
            if (wantsMove && Vector2.Distance(Projectile.Center, lastPos) < 1.2f) hardStuck++;
            else hardStuck = 0;
            lastPos = Projectile.Center;
            if (hardStuck == 60)
            {
                phaseTimer = 50; // phase through walls for ~0.8s to drift free
                StuckDust();
            }
            else if (hardStuck > 140)
            {
                hardStuck = 0;
                TeleportNearPlayer(player);
            }

            UpdateFrame(target);

            if (Main.rand.NextBool(4))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, 0f, 0f, 150, default, 0.7f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.2f;
            }
        }

        void StuckDust()
        {
            for (int i = 0; i < 8; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, 0f, 0f, 130, default, 1f);
                Main.dust[d].noGravity = true;
            }
        }

        void TeleportNearPlayer(Player player)
        {
            StuckDust();
            Projectile.Center = player.Center + new Vector2(Main.rand.NextFloat(-60f, 60f), -90f);
            Projectile.velocity = Vector2.Zero;
            floatState.Path = null;
            phaseTimer = 0;
            Projectile.netUpdate = true;
            StuckDust();
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
        }

        void FaceX(float targetX)
        {
            if (Math.Abs(targetX - Projectile.Center.X) > 16f)
                Projectile.spriteDirection = targetX > Projectile.Center.X ? 1 : -1;
        }

        NPC FindTarget(Player player)
        {
            NPC whip = Projectile.OwnerMinionAttackTargetNPC;
            if (whip != null && whip.CanBeChasedBy(this) && Vector2.Distance(whip.Center, Projectile.Center) < 900f)
                return whip;
            NPC found = null;
            float best = 800f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(this)) continue;
                if (Math.Abs(npc.Center.X - player.Center.X) > 800f || Math.Abs(npc.Center.Y - player.Center.Y) > 800f) continue;
                float d = Vector2.Distance(npc.Center, Projectile.Center);
                if (d < best) { best = d; found = npc; }
            }
            return found;
        }

        void UpdateFrame(NPC target)
        {
            if (UseStrangeEyeSprite)
            {
                // Gaze toward the target; when idle, look at the PLAYER (stable) instead of the jittery
                // steering velocity — and rotate more slowly — so it isn't erratic while idle.
                Player owner = Main.player[Projectile.owner];
                Vector2 gazeDir = target != null ? target.Center - Projectile.Center : owner.Center - Projectile.Center;
                float rate = target != null ? 0.10f : 0.05f;
                gazeAngle = gazeAngle.AngleLerp(gazeDir.ToRotation(), rate);
                if (RotateStrangeEyeGaze)
                {
                    // Rotate a single frame → perfectly smooth aiming (no discrete-frame jitter).
                    Projectile.frame = StrangeEyeFixedFrame;
                    Projectile.rotation = gazeAngle + StrangeEyeGazeOffset;
                }
                else
                {
                    Projectile.rotation = 0f;
                    float a = gazeAngle;
                    if (a < 0f) a += MathHelper.TwoPi;
                    Projectile.frame = (int)Math.Round(a / (MathHelper.TwoPi / 8f)) & 7;
                }
                return;
            }
            // SoulVessel: top 3 frames closed, bottom 3 open (when firing).
            if (++frameTimer >= 8) { frameTimer = 0; frameIndex = (frameIndex + 1) % 3; }
            Projectile.frame = (mouthOpen ? 3 : 0) + frameIndex;
        }

        bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<LesserVesselBuff>());
                return false;
            }
            if (owner.HasBuff(ModContent.BuffType<LesserVesselBuff>())) Projectile.timeLeft = 2;
            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            int frameH = tex.Height / Main.projFrames[Projectile.type];
            Rectangle src = new Rectangle(0, Projectile.frame * frameH, tex.Width, frameH);
            Vector2 origin = src.Size() / 2f;
            float opacity = lifetime < 120 ? lifetime / 120f : 1f;
            bool rotating = UseStrangeEyeSprite && RotateStrangeEyeGaze;
            SpriteEffects fx = rotating ? SpriteEffects.None
                : (Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);

            // Leonhard-style cached afterimage trail — a purple shadow hugging just behind the sprite.
            for (int k = Projectile.oldPos.Length - 1; k > 0; k--)
            {
                if (Projectile.oldPos[k] == Vector2.Zero) continue;
                Vector2 dp = Projectile.oldPos[k] + Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                float f = (Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length;
                Color trail = new Color(150, 60, 200, 0) * f * 0.5f * opacity;
                Main.EntitySpriteDraw(tex, dp, src, trail, Projectile.oldRot[k], origin, SpriteScale * (0.7f + 0.3f * f), fx, 0);
            }

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                src, lightColor * opacity, Projectile.rotation, origin, SpriteScale, fx, 0);
            return false;
        }
    }
}
