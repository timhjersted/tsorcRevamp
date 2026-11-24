using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Eater of Worlds Toxic Drip. Every 8-12s a random exposed body piece near a player oozes for one second,
    /// then lobs a toxic glob at that player. The drip leaves no water behind and nearby water stays normal.
    /// </summary>
    public class EaterOfWorldsToxicDrip : ModSystem
    {
        // One drip per 8-12s for the whole boss (all split worms share it), each piece at most once per 10s.
        public const int MinDripIntervalTicks = 8 * 60;
        public const int MaxDripIntervalTicks = 12 * 60;
        public const int PieceCooldownTicks = 10 * 60;

        // A piece only drips when a player is within 40 tiles with a clear line to it, so every glob is seen.
        public const float GlobReachRange = 40 * 16;

        private static int nextDripTimer = MinDripIntervalTicks;

        private static bool IsEaterSegment(NPC npc)
        {
            bool isEaterType = npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail;
            return npc.active && isEaterType;
        }

        public override void PostUpdateNPCs()
        {
            // Server/singleplayer only: drips are projectiles, which sync themselves.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool anyEaterAlive = false;
            List<NPC> dripCandidates = new List<NPC>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!IsEaterSegment(npc))
                {
                    continue;
                }

                anyEaterAlive = true;

                // Only pieces a player can actually see and dodge: a drip 100+ tiles down the worm's length
                // was the reason playtesters never noticed the attack.
                bool isBodyReady = npc.type == NPCID.EaterofWorldsBody && npc.GetGlobalNPC<EaterOfWorldsDripPiece>().CooldownTicks <= 0;
                if (isBodyReady && IsInOpenAir(npc) && FindDripTarget(npc.Center) >= 0)
                {
                    dripCandidates.Add(npc);
                }
            }

            bool anyPlayerAlive = false;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    anyPlayerAlive = true;
                    break;
                }
            }

            // Fight over: boss dead or despawned, or a wipe. Reset so the next fight starts with a full delay.
            if (!anyEaterAlive || !anyPlayerAlive)
            {
                nextDripTimer = MinDripIntervalTicks;
                return;
            }

            if (nextDripTimer > 0)
            {
                nextDripTimer--;
                return;
            }

            // Timer is up but every piece is buried or cooling down: hold at 0 and try again next tick.
            if (dripCandidates.Count == 0)
            {
                return;
            }

            NPC dripper = Main.rand.Next(dripCandidates);
            dripper.GetGlobalNPC<EaterOfWorldsDripPiece>().CooldownTicks = PieceCooldownTicks;
            nextDripTimer = Main.rand.Next(MinDripIntervalTicks, MaxDripIntervalTicks + 1);

            int targetPlayer = FindDripTarget(dripper.Center);

            Projectile.NewProjectile(dripper.GetSource_FromAI(), dripper.Center, Vector2.Zero,
                ModContent.ProjectileType<EaterToxicOoze>(), 0, 0f, Main.myPlayer, dripper.whoAmI, targetPlayer);
        }

        /// <summary>
        /// Closest living player within GlobReachRange of the point with a clear line to it, or -1.
        /// Server/SP: picks the drip's piece and the glob's aim.
        /// </summary>
        public static int FindDripTarget(Vector2 from)
        {
            int bestPlayer = -1;
            float bestDistanceSquared = GlobReachRange * GlobReachRange;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (!player.active || player.dead)
                {
                    continue;
                }

                float distanceSquared = Vector2.DistanceSquared(from, player.Center);
                if (distanceSquared >= bestDistanceSquared)
                {
                    continue;
                }

                bool hasLineOfSight = Collision.CanHitLine(from, 1, 1, player.Center, 1, 1);
                if (hasLineOfSight)
                {
                    bestPlayer = i;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return bestPlayer;
        }

        // A piece burrowing through dirt can't drip; it must be in air (not solid, not already underwater).
        private static bool IsInOpenAir(NPC npc)
        {
            Point tileCoords = npc.Center.ToTileCoordinates();

            if (!WorldGen.InWorld(tileCoords.X, tileCoords.Y, 10))
            {
                return false;
            }

            Tile tile = Main.tile[tileCoords.X, tileCoords.Y];
            bool isSolid = tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
            return !isSolid && tile.LiquidAmount == 0;
        }

        public override void OnWorldUnload()
        {
            nextDripTimer = MinDripIntervalTicks;
        }
    }

    /// <summary>Per-body-piece drip cooldown. Server/SP only; clients never read it.</summary>
    public class EaterOfWorldsDripPiece : GlobalNPC
    {
        public int CooldownTicks;

        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return entity.type == NPCID.EaterofWorldsBody;
        }

        public override void PostAI(NPC npc)
        {
            if (CooldownTicks > 0)
            {
                CooldownTicks--;
            }
        }
    }

    /// <summary>
    /// Toxic Drip telegraph. ai[0] = body piece whoAmI, ai[1] = target player (-1 = none).
    /// Rides the piece for OozeTicks while a green glob swells on it (bubbling sound, converging green
    /// motes), then the server lobs an EaterToxicGlob at the target.
    /// </summary>
    public class EaterToxicOoze : ModProjectile
    {
        // Same sprite as the glob it releases, so the swelling preview becomes the shot without a pop.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ToxicBubble;

        // 60-tick ooze telegraph before the glob is released.
        private const int OozeTicks = 60;

        private const int AttachedTimerSlot = 0;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = OozeTicks + 10;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.scale = 0.2f;
        }

        public override void AI()
        {
            float attachedTicks = Projectile.localAI[AttachedTimerSlot];
            int pieceIndex = (int)Projectile.ai[0];
            NPC piece = Main.npc[pieceIndex];
            bool pieceAlive = piece.active && piece.type == NPCID.EaterofWorldsBody;

            if (attachedTicks == 0 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item85 with { Volume = 1f, Pitch = -0.4f }, Projectile.Center);
            }

            // Ooze telegraph: the glob swells from 20% to full size on the piece while green motes are
            // pulled in from a 40px ring, denser as the release nears.
            if (attachedTicks < OozeTicks && pieceAlive)
            {
                Projectile.Center = piece.Center;
                Projectile.velocity = Vector2.Zero;
                Projectile.localAI[AttachedTimerSlot] = attachedTicks + 1f;

                float progress = attachedTicks / OozeTicks;
                Projectile.scale = MathHelper.Lerp(0.2f, 1f, progress);

                if (!Main.dedServ)
                {
                    Lighting.AddLight(Projectile.Center, 0.25f * progress, 0.8f * progress, 0.2f * progress);

                    int moteCount = 1 + (int)(progress * 3f);

                    for (int i = 0; i < moteCount; i++)
                    {
                        Vector2 ringOffset = Main.rand.NextVector2CircularEdge(40f, 40f);
                        Vector2 inwardVelocity = -ringOffset * 0.08f;
                        Dust body = Dust.NewDustPerfect(piece.Center + ringOffset, DustID.Poisoned, inwardVelocity, 40, default, MathHelper.Lerp(1f, 1.6f, progress));
                        body.noGravity = true;
                    }

                    // Bright glint layer so the tell reads against the dark corruption sky.
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 glintOffset = Main.rand.NextVector2Circular(piece.width * 0.5f, piece.height * 0.5f);
                        Dust glint = Dust.NewDustPerfect(piece.Center + glintOffset, DustID.GreenTorch, new Vector2(0f, 0.6f), 0, default, 1.4f);
                        glint.noGravity = true;
                    }
                }

                return;
            }

            // Release (telegraph done, or the piece died early: the glob leaves from where it last was).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Re-pick the target if the one chosen at the tell has died or left.
                int targetPlayer = (int)Projectile.ai[1];
                bool targetValid = targetPlayer >= 0 && targetPlayer < Main.maxPlayers && Main.player[targetPlayer].active && !Main.player[targetPlayer].dead;

                if (!targetValid)
                {
                    targetPlayer = EaterOfWorldsToxicDrip.FindDripTarget(Projectile.Center);
                }

                // No one to aim at: let it fall straight down from the piece.
                Vector2 launchVelocity = new Vector2(0f, 1f);

                if (targetPlayer >= 0)
                {
                    launchVelocity = EaterToxicGlob.SolveLob(Projectile.Center, Main.player[targetPlayer].Center);
                }

                int damage = Utilities.EnemyDamage.Projectile(EaterToxicGlob.ExpertHitDamage);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, launchVelocity,
                    ModContent.ProjectileType<EaterToxicGlob>(), damage, 0f, Main.myPlayer);
            }

            Projectile.Kill();
        }

        // Release cue: wet squelch and a short downward gush. No pop: the glob takes over at full size.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.NPCHit13 with { Volume = 0.9f, Pitch = -0.3f }, Projectile.Center);

            for (int i = 0; i < 30; i++)
            {
                Vector2 gushVelocity = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(0.5f, 4f));
                // dust-lint: ok - a release gush, not an impact: the glob flies on and owns the 3-layer burst.
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Poisoned, gushVelocity, 40, default, Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = false;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EaterToxicGlob.DrawGlob(Projectile);
            return false;
        }
    }

    /// <summary>
    /// The Toxic Drip glob: a green bubble lobbed on a gravity arc that lands 48px past the target.
    /// Bursts on tiles (or after 3s with a 15-tick fade) into a 4-tile poison splash. Green and arcing
    /// by design so it can't be confused with Vile Spit (purple, slow, homing, floats through walls).
    /// </summary>
    public class EaterToxicGlob : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ToxicBubble;

        // Expert hit before defense. EoW body contact is 24 and the head 42; a dodgeable telegraphed lob sits between.
        public const int ExpertHitDamage = 36;

        // Poison Buildup out of 100: a direct hit, and the landing splash for anyone within SplashRadius.
        public const int HitBuildup = 35;
        public const int SplashBuildup = 25;
        public const float SplashRadius = 4 * 16;

        // Arc: 0.25 px/tick^2 gravity, 36-70 tick flight (7 px/tick of distance), lands 48px past the target.
        public const float Gravity = 0.25f;
        private const float MaxFallSpeed = 14f;
        private const float OvershootPixels = 48f;

        private const int LifetimeTicks = 180;
        private const int FadeTicks = 15;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        /// <summary>
        /// Launch velocity that lands the glob OvershootPixels past the target after a distance-scaled flight.
        /// Accounts for gravity being added before each move: total drop over T ticks = Gravity * T * (T + 1) / 2.
        /// </summary>
        public static Vector2 SolveLob(Vector2 from, Vector2 target)
        {
            float side = 1f;

            if (target.X < from.X)
            {
                side = -1f;
            }

            Vector2 landing = target + new Vector2(side * OvershootPixels, 0f);
            Vector2 offset = landing - from;
            float flightTicks = MathHelper.Clamp(offset.Length() / 7f, 36f, 70f);

            float velocityX = offset.X / flightTicks;
            float velocityY = offset.Y / flightTicks - Gravity * (flightTicks + 1f) / 2f;
            return new Vector2(velocityX, velocityY);
        }

        public override void AI()
        {
            Projectile.velocity.Y = System.Math.Min(Projectile.velocity.Y + Gravity, MaxFallSpeed);

            // Tumble in the direction of travel; the bubble sprite is round, so no leading edge.
            Projectile.rotation += 0.12f * System.Math.Sign(Projectile.velocity.X);

            // Last FadeTicks of a timeout: fade out instead of vanishing at full opacity.
            if (Projectile.timeLeft <= FadeTicks)
            {
                Projectile.Opacity = Projectile.timeLeft / (float)FadeTicks;
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, 0.2f, 0.7f, 0.15f);

            // Trail: 3 body motes interpolated along this tick's step (gap <= 5px at 14 px/tick) plus a glint.
            Vector2 previousCenter = Projectile.Center - Projectile.velocity;

            for (int i = 0; i < 3; i++)
            {
                Vector2 motePosition = Vector2.Lerp(previousCenter, Projectile.Center, i / 3f) + Main.rand.NextVector2Circular(4f, 4f);
                Dust body = Dust.NewDustPerfect(motePosition, DustID.Poisoned, Projectile.velocity * 0.1f, 40, default, 1.2f);
                body.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, Main.rand.NextVector2Circular(1f, 1f), 0, default, 1.3f);
                glint.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Runs on the hit player's machine, which owns the poison meter.
            Buffs.Debuffs.PoisonBuildup.Apply(target, HitBuildup);
        }

        // Every death path (tile, timeout, cleared) bursts: splash buildup for the local player plus a 3-layer burst.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Each client checks only its own player: the poison meter is owned by the victim's machine.
            Player localPlayer = Main.LocalPlayer;
            bool inSplash = Vector2.Distance(localPlayer.Center, Projectile.Center) < SplashRadius;

            if (inSplash && Collision.CanHitLine(Projectile.Center, 1, 1, localPlayer.Center, 1, 1))
            {
                Buffs.Debuffs.PoisonBuildup.Apply(localPlayer, SplashBuildup);
            }

            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.8f, Pitch = 0.1f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.6f, Pitch = 0.2f }, Projectile.Center);

            // Body: a ring the size of the splash radius, so the poisoned area is visible.
            for (int i = 0; i < 40; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, SplashRadius / 12f);
                Dust body = Dust.NewDustPerfect(Projectile.Center, DustID.Poisoned, burstVelocity, 40, default, Main.rand.NextFloat(1.2f, 1.8f));
                body.noGravity = true;
            }

            // Glints.
            for (int i = 0; i < 20; i++)
            {
                Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, Main.rand.NextVector2Circular(4f, 4f), 0, default, 1.5f);
                glint.noGravity = true;
            }

            // Lingering mist.
            for (int i = 0; i < 12; i++)
            {
                Dust mist = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(SplashRadius * 0.6f, 12f), DustID.Poisoned, new Vector2(0f, -0.4f), 120, default, 1f);
                mist.noGravity = true;
                mist.fadeIn = 1.6f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawGlob(Projectile);
            return false;
        }

        /// <summary>Glowing bubble draw shared by the ooze preview and the glob: centred, scaled, mostly unlit.</summary>
        public static void DrawGlob(Projectile projectile)
        {
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            Vector2 drawPosition = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
            Vector2 origin = texture.Size() / 2f;
            Color litColor = Lighting.GetColor(projectile.Center.ToTileCoordinates());
            Color drawColor = Color.Lerp(litColor, Color.White, 0.6f) * projectile.Opacity;

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0);
        }
    }
}
