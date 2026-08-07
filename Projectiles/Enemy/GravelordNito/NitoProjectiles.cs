using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Invisible melee HITBOX for Nito's sword swings. The visible blade is drawn by the boss itself
    // (a loose sword layer BEHIND its body), so this projectile draws nothing — it only mirrors the
    // exact same arc via the shared GravelordNito.Slash* helpers so the hitbox tracks the visual.
    class NitoSwordSlash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        int OwnerIndex => (int)Projectile.ai[0];
        int Kind => (int)Projectile.ai[1];
        int Dir => Projectile.velocity.X >= 0f ? 1 : -1;
        int Timer => (int)Projectile.localAI[0];
        const int SwingTicks = 18;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 330;
            Projectile.height = 260;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = SwingTicks;
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            NPC owner = OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs ? Main.npc[OwnerIndex] : null;
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            float progress = MathHelper.Clamp(Timer / (float)SwingTicks, 0f, 1f);
            // +30px matches the boss's GroundSinkPixels draw sink so the hitbox sits on the visual blade.
            Projectile.Center = owner.Center + new Vector2(0f, 30f) + global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashOffset(Kind, Dir, progress);
            Projectile.rotation = global::tsorcRevamp.NPCs.Bosses.GravelordNito.GravelordNito.SlashWorldAngle(Kind, Dir, progress);

            // The shader carries the blade silhouette; retain only a few bone sparks as texture.
            Vector2 blade = Projectile.rotation.ToRotationVector2();
            Vector2 hilt = Projectile.Center - blade * 40f;
            Vector2 tip = Projectile.Center + blade * 150f;
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = Vector2.Lerp(hilt, tip, Main.rand.NextFloat()) + Main.rand.NextVector2Circular(7f, 7f);
                Dust d = Dust.NewDustPerfect(pos, DustID.BoneTorch, blade * Main.rand.NextFloat(0.5f, 2f), 90, default, 1f);
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 hilt = Projectile.Center - Projectile.rotation.ToRotationVector2() * 60f;
            Vector2 tip = Projectile.Center + Projectile.rotation.ToRotationVector2() * 170f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(new Vector2(targetHitbox.X, targetHitbox.Y), new Vector2(targetHitbox.Width, targetHitbox.Height), hilt, tip, Kind == 2 ? 34f : 48f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Timer / (float)SwingTicks, 0f, 1f);
            Vector2 blade = Projectile.rotation.ToRotationVector2();
            Vector2 center = Projectile.Center + blade * 50f;
            NitoVFX.DrawSlash(center, Projectile.rotation,
                new Vector2(255f, Kind == 2 ? 62f : 88f), progress, 0.9f,
                Projectile.identity * 0.517f + Kind * 0.31f);
            return false;
        }
    }

    class NitoBoneShard : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bone;

        // Optional spin-up telegraph. ai[0] = 0 keeps the original fire-immediately behaviour (used by
        // Hollow Command's radial burst, which is its own telegraph); Bone Volley passes 60 so its
        // shards materialise, hang and spin for a full second first.
        int ChargeTicks => (int)Projectile.ai[0];
        int OwnerIndex => (int)Projectile.ai[1];
        float FanOffset => Projectile.ai[2];
        int Timer => (int)Projectile.localAI[0];
        float LaunchSpeed => Projectile.localAI[1];
        // ChargeTicks > 0 guard matters: CanDamage/ShouldUpdatePosition can be queried BEFORE the first
        // AI() tick, when Timer is still 0 — without it a zero-charge shard (Hollow Command's radial
        // burst) would read as "charging" and spend its first frame frozen and harmless.
        bool Charging => ChargeTicks > 0 && Timer <= ChargeTicks;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 7;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Remember the speed the spawner asked for, then hold position; the launch below restores
            // it along a freshly-aimed vector.
            Projectile.localAI[1] = Projectile.velocity.Length();
            if (ChargeTicks > 0)
            {
                Projectile.timeLeft += ChargeTicks;
                Projectile.velocity = Vector2.Zero;
            }
        }

        // A shard hanging in the air waiting to launch must not clip on terrain or deal damage.
        public override bool ShouldUpdatePosition() => !Charging;
        public override bool? CanDamage() => Charging ? false : null;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Charging)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation += 0.42f; // spin in place — the actual "incoming" tell
                Lighting.AddLight(Projectile.Center, 0.18f, 0.18f, 0.2f);
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                        DustID.Smoke, Main.rand.NextVector2Circular(1.1f, 1.1f), 120, new Color(150, 150, 150), 1f);
                    d.noGravity = true;
                }
                return;
            }
            if (Timer == ChargeTicks + 1 && ChargeTicks > 0)
            {
                LaunchAtTarget();
            }
            Projectile.rotation += Projectile.velocity.X * 0.04f;
            Projectile.velocity.Y += 0.08f;
            Lighting.AddLight(Projectile.Center, 0.16f, 0.16f, 0.22f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.BoneTorch, Projectile.velocity * -0.05f, 110, default, 0.85f);
                d.noGravity = true;
            }
        }

        ///<summary>Aims at release rather than at spawn: a full second of lead time locked in at spawn
        ///would make every volley trivially walk-away-able. The telegraph warns; the shot still tracks.</summary>
        void LaunchAtTarget()
        {
            float speed = LaunchSpeed > 0.1f ? LaunchSpeed : 7.5f;
            Vector2 aim = new Vector2(speed, 0f);
            if (OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs)
            {
                NPC owner = Main.npc[OwnerIndex];
                if (owner.active && owner.target >= 0 && owner.target < Main.maxPlayers)
                {
                    Player target = Main.player[owner.target];
                    if (target.active && !target.dead)
                    {
                        aim = UsefulFunctions.Aim(Projectile.Center, target.Center, speed);
                    }
                }
            }
            Projectile.velocity = aim.RotatedBy(FanOffset);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.35f, Pitch = 0.3f }, Projectile.Center);
            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Charging)
            {
                return true; // no motion trail on a shard that isn't moving yet — just the spinning bone
            }
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 center = Projectile.Center - direction * 24f;
            float progress = MathHelper.Clamp(1f - Projectile.timeLeft / 210f, 0.08f, 0.72f);
            NitoVFX.DrawSoulTrail(center, direction.ToRotation(), new Vector2(78f, 24f), progress, 0.62f,
                Projectile.identity * 0.437f);
            return true;
        }
    }

    class NitoDeathNova : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float ExpandSpeed = 8f;
        const float RingHalfThickness = 24f;
        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 260f;
        float Radius => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 620;
            Projectile.height = 620;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = (int)(MaxRadius / ExpandSpeed) + 3;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] += ExpandSpeed;
            for (int i = 0; i < 34; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * (Radius + Main.rand.NextFloat(-7f, 7f));
                Dust d = Dust.NewDustPerfect(pos, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.BoneTorch, angle.ToRotationVector2() * 2.2f, 75, default, Main.rand.NextFloat(1f, 1.5f));
                d.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.45f, 0.35f, 0.65f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Radius > 2f)
            {
                NitoVFX.DrawDeathRing(Projectile.Center, Radius, RingHalfThickness, 0.95f);
                float flashProgress = MathHelper.Clamp(Radius / 88f, 0f, 1f);
                if (flashProgress < 1f)
                {
                    NitoVFX.DrawDeathFlash(Projectile.Center, Vector2.One * 176f,
                        flashProgress, 0.72f * (1f - flashProgress));
                }
            }
            return true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            float distClosest = Vector2.Distance(Projectile.Center, closest);
            float distFarthest = 0f;
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Top)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Left, targetHitbox.Bottom)));
            distFarthest = System.Math.Max(distFarthest, Vector2.Distance(Projectile.Center, new Vector2(targetHitbox.Right, targetHitbox.Bottom)));
            return distClosest <= Radius + RingHalfThickness && distFarthest >= Radius - RingHalfThickness;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Darkness, 5 * 60);
            target.AddBuff(BuffID.Slow, 2 * 60);
        }
    }

    class NitoGraveSpike : ModProjectile
    {
        public override string Texture => "tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordNitoSwordDance";

        const int RiseTicks = 7;
        const int HoldTicks = 80;
        // 12 -> 60: the blade used to vanish almost the instant it started withdrawing. It now sinks
        // slowly AND fades out over the same window (see the `fade` term in PreDraw).
        const int SinkTicks = 60;
        int TelegraphTicks => (int)Projectile.ai[0];
        float HeightScale => Projectile.ai[1] > 0f ? Projectile.ai[1] : 1f;
        int Timer => (int)Projectile.localAI[0];
        bool Erupted => Timer > TelegraphTicks;
        bool Sinking => Timer > TelegraphTicks + RiseTicks + HoldTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 26;
            Projectile.height = 54;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.height = (int)(54 * HeightScale);
            Projectile.timeLeft = TelegraphTicks + RiseTicks + HoldTicks + SinkTicks;
        }

        public override bool? CanDamage() => Erupted && !Sinking;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float bottom = Projectile.position.Y + Projectile.height;
            if (!Erupted)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust.NewDust(new Vector2(Projectile.position.X, bottom - 10f), Projectile.width, 8, DustID.BoneTorch, 0f, -0.8f, 100, default, 0.9f);
                }
                return;
            }
            if (Timer == TelegraphTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.55f, Pitch = -0.5f }, Projectile.Center);
                // Bursting out of the ground throws grave-fire at the breach point.
                NitoVFX.PyreBurst(new Vector2(Projectile.Center.X, bottom), 26, 4.5f, 1.2f, 20f, 8f);
            }
            if (Timer == TelegraphTicks + RiseTicks + HoldTicks + 1)
            {
                // ...and again as it begins withdrawing, so the exit reads as deliberate rather than
                // the sprite simply being switched off.
                NitoVFX.PyreBurst(new Vector2(Projectile.Center.X, bottom), 18, 3.2f, 1f, 18f, 8f);
            }
            if (!Sinking && Main.rand.NextBool(4))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, 0f, -0.6f, 100, default, 0.8f);
            }
        }

        float RiseProgress()
        {
            if (!Erupted)
            {
                return 0f;
            }
            if (Sinking)
            {
                return 1f - (Timer - TelegraphTicks - RiseTicks - HoldTicks) / (float)SinkTicks;
            }
            return MathHelper.Min(1f, (Timer - TelegraphTicks) / (float)RiseTicks);
        }

        ///<summary>Blades erupt from the ground, so they must render BEHIND the tiles — otherwise the
        ///buried portion draws over the floor and the whole spike reads as floating on top of the
        ///terrain instead of being driven through it.</summary>
        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = RiseProgress();
            float bottomY = Projectile.position.Y + Projectile.height + 2f;
            float telegraphProgress = TelegraphTicks > 0
                ? MathHelper.Clamp(Timer / (float)TelegraphTicks, 0f, 1f)
                : 1f;
            // Withdrawing now fades as well as sinks (progress runs 1 -> 0 across SinkTicks).
            float fade = Sinking ? MathHelper.Clamp(progress, 0f, 1f) : 1f;
            float riftOpacity = Sinking ? MathHelper.Clamp(progress, 0f, 1f) : 0.82f;
            NitoVFX.DrawGroundRift(new Vector2(Projectile.Center.X, bottomY - 4f),
                new Vector2(96f * HeightScale, 44f), telegraphProgress, riftOpacity);

            if (progress <= 0f)
            {
                return false;
            }
            NitoVFX.DrawGraveEruption(Projectile.Center + new Vector2(0f, Projectile.height * 0.12f),
                new Vector2(74f * HeightScale, Projectile.height * 1.35f), progress, 0.48f * fade);
            // The sword-dance blade (GravelordNitoSwordDance.png, 58x224 — already a tall, straight,
            // upright blade shape) SLIDES up out of the ground and back down (translate) instead of
            // growing in place (a Y-squash, which read like the blade flopping over in 3D). Drawn at
            // full — now DOUBLE — size the whole time; the un-emerged fraction is simply pushed below
            // the floor line. Undyed: the sprite's own crimson reads.
            // Scale must be UNIFORM (X == Y): the old code derived scaleX from Projectile.width (the
            // narrow 26px HITBOX, unrelated to the art) against the texture's width, independently of
            // scaleY — squashing the tall, slender texture into a short, fat, visibly "warped" shape.
            const float SizeScale = 2f; // twice as big as the old draw
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float scale = Projectile.height / (float)texture.Height * SizeScale;
            float drawnHeight = texture.Height * scale; // == Projectile.height * SizeScale
            float emerge = (1f - progress) * drawnHeight; // pushes the blade down into the floor when not fully risen
            Vector2 bottom = new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height + 2f + emerge) - Main.screenPosition;
            Main.EntitySpriteDraw(texture, bottom, null, Color.White * fade, 0f, new Vector2(texture.Width / 2f, texture.Height), new Vector2(scale, scale), SpriteEffects.None, 0);
            return false;
        }
    }

    ///<summary>
    ///A pair of these erupts flanking the player, holds for a beat, then CLAPS together on the spot
    ///the player was standing when they rose — detonating a blast at the meeting point. The two hands
    ///share a convergence centre (ai[1]) and exactly one of them (ai[2]) is the designated exploder,
    ///so the blast fires once rather than twice. Each also leaves a lingering grave-fire (NitoPyreFire)
    ///burning at its own breach point, which is what actually denies the flanks while they close.
    ///</summary>
    class NitoGraveHand : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        int TelegraphTicks => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : 18;
        float ConvergeCenterX => Projectile.ai[1];
        bool IsExploder => Projectile.ai[2] > 0.5f;
        int Timer => (int)Projectile.localAI[0];

        const int GrabTicks = 16;    // the initial damaging emergence
        const int PauseTicks = 60;   // the 1-second beat before they start closing
        const float ConvergeSpeed = 3.1f;
        const int MaxConvergeTicks = 150; // failsafe so a hand can never chase forever
        const float BlastRadius = 125f;   // ~250px across

        int EmergeTick => TelegraphTicks;
        int ConvergeStartTick => TelegraphTicks + GrabTicks + PauseTicks;
        bool Converging => Timer > ConvergeStartTick;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            // DOUBLED (was 86x78) on request — the grasping hand reads as a set piece now, not a prop.
            // This is the one deliberate gameplay change in the shader pass: the grab area doubles with
            // it, so visual and hitbox stay in parity. Halve these two numbers to revert.
            Projectile.width = 172;
            Projectile.height = 156;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // NewProjectile centres the hitbox on the spawn point, so doubling the height would have
            // buried the hand's wrist 39px further into the floor and dragged the ground-rift telegraph
            // (drawn at Projectile.Bottom) underground with it. Re-anchor so the BOTTOM sits where the
            // old 78px hand's bottom sat.
            Projectile.position.Y -= 39f;
            Projectile.timeLeft = ConvergeStartTick + MaxConvergeTicks + 20;
        }

        ///<summary>Hands claw up out of the ground, so like the blades they belong behind the tiles —
        ///otherwise the wrist draws on top of the floor it is supposedly bursting through.</summary>
        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        // Dangerous on the way up, and again while sweeping inward — but not during the hold, which is
        // the player's window to get out from between them.
        public override bool? CanDamage() =>
            (Timer > EmergeTick && Timer < EmergeTick + GrabTicks) || Converging;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Timer <= EmergeTick)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust.NewDust(Projectile.BottomLeft - new Vector2(0f, 8f), Projectile.width, 12, DustID.BoneTorch, 0f, -1f, 100, default, 0.9f);
                }
                return;
            }
            if (Timer == EmergeTick + 1)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.4f }, Projectile.Center);
                for (int i = 0; i < 22; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f), 80, default, 1.1f);
                }
                NitoVFX.PyreBurst(Projectile.Bottom, 30, 5f, 1.3f, 30f, 10f);
                // The breach itself keeps burning after the hand has left it.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Bottom, Vector2.Zero,
                        ModContent.ProjectileType<NitoPyreFire>(), Projectile.damage / 2, 0f, Projectile.owner);
                }
                return;
            }
            if (!Converging)
            {
                return; // the held beat
            }

            int dir = ConvergeCenterX >= Projectile.Center.X ? 1 : -1;
            Projectile.velocity = new Vector2(dir * ConvergeSpeed, 0f);
            if (Main.rand.NextBool(2))
            {
                NitoVFX.PyreBurst(Projectile.Bottom + new Vector2(0f, -6f), 1, 1.6f, 0.8f, 26f, 10f);
            }

            // Met in the middle: detonate. Only the flagged hand spawns the blast so a pair produces
            // one explosion, but BOTH retire on contact.
            if (System.Math.Abs(Projectile.Center.X - ConvergeCenterX) <= ConvergeSpeed + 1f)
            {
                if (IsExploder)
                {
                    Vector2 blastCenter = new Vector2(ConvergeCenterX, Projectile.Center.Y);
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f, Pitch = -0.35f }, blastCenter);
                    UsefulFunctions.ScreenShake(blastCenter, 7f, 16);
                    NitoVFX.PyreBurst(blastCenter, 60, 9f, 1.6f, 40f, 40f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), blastCenter, Vector2.Zero,
                            ModContent.ProjectileType<NitoDeathNova>(), Projectile.damage, 6f, Projectile.owner, BlastRadius);
                    }
                }
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float telegraphProgress = MathHelper.Clamp(Timer / (float)TelegraphTicks, 0f, 1f);
            float activeProgress = Timer <= TelegraphTicks
                ? telegraphProgress * 0.3f
                : MathHelper.Clamp((Timer - TelegraphTicks) / 10f, 0f, 1f);
            float fade = MathHelper.Clamp((Projectile.timeLeft - 4f) / 12f, 0f, 1f);
            // The rift stays behind at the BREACH point rather than sliding along with a converging
            // hand — the hole in the ground doesn't travel.
            if (!Converging)
            {
                NitoVFX.DrawGroundRift(Projectile.Bottom - new Vector2(0f, 7f),
                    new Vector2(Projectile.width * 1.35f, 48f), telegraphProgress, 0.8f * fade);
            }
            NitoVFX.DrawGraveHand(Projectile.Center, new Vector2(Projectile.width * 1.15f, Projectile.height * 1.35f),
                activeProgress, 0.82f * fade);
            return true;
        }
    }

    ///<summary>
    ///The red-and-black grave-fire a grave hand leaves burning in the hole it tore open. Damaging for
    ///its full 3-second life, then eases out over the last half-second with a scatter of ember dust
    ///rather than blinking off. Reuses the pyre-family plume shader the eruptions already use.
    ///</summary>
    class NitoPyreFire : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int BurnTicks = 180;  // 3 seconds
        const int FadeTicks = 30;
        int Timer => (int)Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 110;
            Projectile.height = 70;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BurnTicks + FadeTicks;
            Projectile.aiStyle = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Spawned at the breach (a ground point), so lift the box to sit ON the ground.
            Projectile.position.Y -= Projectile.height * 0.75f;
        }

        // Stops biting once it starts guttering out, so the visual tail is never an invisible hitbox.
        public override bool? CanDamage() => Timer < BurnTicks;

        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, 0.5f, 0.16f, 0.1f);
            if (Timer < BurnTicks && Main.rand.NextBool(2))
            {
                NitoVFX.PyreBurst(Projectile.Center + new Vector2(0f, 10f), 1, 2.2f, 0.9f, 46f, 12f);
            }
            else if (Timer == BurnTicks)
            {
                NitoVFX.PyreBurst(Projectile.Center, 22, 3.4f, 1.1f, 44f, 16f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fade = Timer < BurnTicks
                ? MathHelper.Clamp(Timer / 12f, 0f, 1f)                        // quick flare-up
                : MathHelper.Clamp(1f - (Timer - BurnTicks) / (float)FadeTicks, 0f, 1f); // smooth guttering
            float progress = MathHelper.Clamp(Timer / (float)BurnTicks, 0f, 1f);
            NitoVFX.DrawGroundRift(Projectile.Bottom - new Vector2(0f, 6f),
                new Vector2(Projectile.width * 1.3f, 46f), 1f, 0.72f * fade);
            NitoVFX.DrawGraveEruption(Projectile.Center,
                new Vector2(Projectile.width * 1.1f, Projectile.height * 1.5f), progress, 0.6f * fade);
            return false;
        }
    }

    ///<summary>
    ///A grave-spike raining down from above (GravelordSpike.png) — the ceiling/sky counterpart to
    ///NitoBoneShard's shot-and-thrown fans. Used by Sword Rain and Gravelord Judgment, both of which
    ///are already conceptually "things falling from above the player."
    ///</summary>
    class NitoCeilingSpike : ModProjectile
    {
        public override string Texture => "tsorcRevamp/NPCs/Bosses/GravelordNito/GravelordSpike";

        int DelayTicks => (int)Projectile.ai[0];
        int Timer => (int)Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 14;
            Projectile.height = 60;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.aiStyle = 0;
        }

        public override bool ShouldUpdatePosition() => Timer > DelayTicks;

        public override bool? CanDamage() => Timer > DelayTicks;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2; // sprite points down at rest
            if (Timer <= DelayTicks)
            {
                Lighting.AddLight(Projectile.Center, 0.22f, 0.18f, 0.3f);
                return;
            }
            Projectile.velocity.Y += 0.15f;
            if (Projectile.velocity.Y > 13f)
            {
                Projectile.velocity.Y = 13f;
            }
            Lighting.AddLight(Projectile.Center, 0.18f, 0.1f, 0.12f);
            if (Main.rand.NextBool(2))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, -Projectile.velocity.X * 0.1f, -Projectile.velocity.Y * 0.1f, 100, default, 0.8f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Timer <= DelayTicks && DelayTicks > 0)
            {
                float progress = MathHelper.Clamp(Timer / (float)DelayTicks, 0f, 1f);
                NitoVFX.DrawRainPortal(Projectile.Center, new Vector2(118f, 54f), progress, 0.92f);
            }
            else
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                NitoVFX.DrawSoulTrail(Projectile.Center - direction * 28f, direction.ToRotation(),
                    new Vector2(92f, 26f), 0.45f, 0.55f, Projectile.identity * 0.437f);
            }
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = -0.3f }, Projectile.Center);
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BoneTorch, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 0f), 100, default, 0.9f);
            }
        }
    }

    class NitoMiasmaCloud : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 58;
            Projectile.height = 58;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 190; // was 95 — lingers twice as long
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            // 0.98 -> 0.995: at the old drag the (already slow) cloud shed most of its speed within
            // half a second and stalled almost on top of Nito. Combined with the doubled launch speed
            // this is what actually carries the breath across the arena.
            Projectile.velocity *= 0.995f;
            Projectile.rotation += 0.03f;
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(28f, 28f), DustID.Poisoned, Main.rand.NextVector2Circular(1f, 1f), 120, default, Main.rand.NextFloat(1.1f, 1.8f));
                d.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.1f, 0.18f, 0.07f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Projectile.localAI[0] / 190f, 0f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / 18f, 0f, 1f);
            NitoVFX.DrawMiasma(Projectile.Center, new Vector2(74f, 70f), Projectile.rotation,
                progress, 0.88f * fade, Projectile.identity * 0.613f);
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 4 * 60);
            target.AddBuff(BuffID.Darkness, 3 * 60);
        }
    }
}
