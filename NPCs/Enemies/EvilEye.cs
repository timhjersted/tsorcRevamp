using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace tsorcRevamp.NPCs.Enemies{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    public class EvilEye : ModNPC
    {
        // ── Attack rotation state machine ──────────────────────────────────────
        // NPC.ai[0] = current State, NPC.ai[1] = ticks spent in current state,
        // NPC.ai[2] = sub-counter (burst shot index in BurstFlank).
        private enum State
        {
            Approach,
            Circle,
            BurstFlank,
            HoverBob,
            NovaRing,
            ChargeTelegraph,
            ChargeDash,
            ChargeRecover
        }

        private static readonly State[] ActivePool = { State.Circle, State.BurstFlank, State.HoverBob, State.NovaRing };

        const string DashTeleportEventID = "EvilEyeDashTeleport";

        const int FrameWidth = 80;
        const int FrameHeight = 40;

        const float BaseContactDamage = 50f;
        const float ChargeContactDamage = 80f;

        const float ApproachSpeed = 1.4f;
        const int ApproachMinTicks = 90;

        const float CircleRadius = 220f;
        const float CircleShotInterval = 25f;
        const int CircleTicks = 260;

        const float BurstShotInterval = 12f;
        const float FlankTeleportDistance = 250f;

        const float HoverShotInterval = 45f;
        const int HoverTicks = 200;

        const float ChargeTelegraphTicks = 45f;
        const float DashSpeed = 15f;
        const int DashTicks = 26;
        const int ChargeRecoverTicks = 50;
        const int DashCooldownTicks = 260;
        const float DashMinRange = 200f;
        const float DashMaxRange = 700f;

        const float FlameDamage = 24f;
        const float FlameSpeed = 7f;
        const float SeekChance = 4f; // 1 in 4 shots from Circle/HoverBob seek weakly

        const float NovaTelegraphTicks = 25f;
        const int NovaShotCount = 12;
        const float NovaRecoverTicks = 40f;

        const float EnrageLifeFraction = 0.3f;
        const float EnrageIntervalMult = 0.6f; // shots/orbit/dash-cooldown all speed up by this factor

        const int GroundBlastCooldownTicks = 300;
        const int GroundBlastDamage = 30;

        // Not netsynced on purpose (flavor movement/timing only, matches the existing
        // laxness around AI flavor state in this codebase - see Leonhard's instance fields).
        float circleAngle;
        int hoverSide = 1;
        Vector2 dashDirection;
        int dashCooldown;
        int groundBlastCooldown;
        bool enraged;
        int commitFlashTimer;

        public override void SetDefaults()
        {
            NPC.width = 80;
            NPC.height = 120;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 900f;
            NPC.npcSlots = 100;
            NPC.scale = 1f;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.1f;
            Main.npcFrameCount[NPC.type] = 3;
            AnimationType = -1;
            NPC.damage = (int)BaseContactDamage;
            NPC.defense = 5;
            NPC.lifeMax = 400;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5;

            dashCooldown = DashCooldownTicks;
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (!Main.hardMode) return 0f;
            if (!spawnInfo.Player.ZoneHallow || !spawnInfo.Player.ZoneOverworldHeight) return 0f;
            if (NPC.AnyNPCs(ModContent.NPCType<EvilEye>())) return 0f;
            if (!Main.rand.NextBool(150)) return 0f;
            return 0.3f;
        }
        #endregion

        void TeleportEffect(Vector2 position)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item8, position);
            for (int m = 0; m < 20; m++)
            {
                int dustID = Dust.NewDust(position, NPC.width, NPC.height, DustID.BlueTorch, 0, 0, 100, default, 2f);
                Main.dust[dustID].noGravity = true;
                Main.dust[dustID].velocity = new Vector2(MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()), MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()));
                Main.dust[dustID].velocity *= 6f;
            }
        }

        void ShootFlame(Player player, bool seeking = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Vector2 velocity = player.Center - NPC.Center;
            if (velocity == Vector2.Zero)
            {
                velocity = new Vector2(0f, 1f);
            }
            velocity.Normalize();
            velocity *= FlameSpeed;
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
            int proj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.EvilEyeFlame>(), (int)FlameDamage, 1f, Main.myPlayer);
            if (seeking && Main.projectile[proj].active)
            {
                Main.projectile[proj].ai[0] = 1f;
            }
        }

        // Shared "gathering power" telegraph visual: dust spawns on a ring around the point
        // and drifts inward, reading as the eye pulling energy into itself before it releases.
        void SpawnInwardChargeDust(Vector2 center, float progress)
        {
            if (Main.rand.NextFloat() < 0.3f + progress * 0.6f)
            {
                Vector2 edge = Main.rand.NextVector2CircularEdge(50f, 50f);
                int dust = Dust.NewDust(center + edge, 2, 2, DustID.BlueTorch, 0, 0, 100, default, 1f + progress);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = -edge * 0.05f;
            }
        }

        void TryDropGroundBlast(Player player)
        {
            if (groundBlastCooldown > 0 || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (!Main.rand.NextBool(90))
            {
                return;
            }
            groundBlastCooldown = GroundBlastCooldownTicks;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.EvilEyeGroundBlast>(), GroundBlastDamage, 4f, Main.myPlayer);
        }

        void CheckEnrage()
        {
            if (enraged || NPC.life > NPC.lifeMax * EnrageLifeFraction)
            {
                return;
            }
            enraged = true;
            NPC.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item72 with { Volume = 1f, Pitch = 0.5f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 10f, 20);
            UsefulFunctions.DustRingPrecise(NPC.Center, 30f, DustID.BlueTorch, 40, 4f, 150, 1.8f);
            EnterState(State.NovaRing);
        }

        void EnterState(State state)
        {
            NPC.ai[0] = (float)state;
            NPC.ai[1] = 0f;
            NPC.ai[2] = 0f;
            NPC.netUpdate = true;
        }

        void EnterRandomActiveState()
        {
            State next = ActivePool[Main.rand.Next(ActivePool.Length)];
            if (next == State.HoverBob)
            {
                hoverSide = Main.rand.NextBool() ? 1 : -1;
            }
            EnterState(next);
        }

        void EnterChargeRecover()
        {
            NPC.damage = (int)BaseContactDamage;
            EnterState(State.ChargeRecover);
        }

        bool TryStartCharge(Player player)
        {
            if (dashCooldown > 0)
            {
                return false;
            }
            float dist = Vector2.Distance(NPC.Center, player.Center);
            if (dist < DashMinRange || dist > DashMaxRange)
            {
                return false;
            }
            if (!Main.rand.NextBool(200))
            {
                return false;
            }
            EnterState(State.ChargeTelegraph);
            return true;
        }

        void TickApproach(Player player)
        {
            Vector2 toPlayer = player.Center - NPC.Center;
            if (toPlayer != Vector2.Zero)
            {
                toPlayer.Normalize();
            }
            NPC.velocity = Vector2.Lerp(NPC.velocity, toPlayer * ApproachSpeed, 0.03f);

            NPC.ai[1]++;
            if (NPC.ai[1] >= ApproachMinTicks && Vector2.Distance(NPC.Center, player.Center) < 500f)
            {
                EnterRandomActiveState();
            }
        }

        void TickCircle(Player player)
        {
            circleAngle += enraged ? 0.03f : 0.02f;
            Vector2 desired = player.Center + new Vector2((float)Math.Cos(circleAngle), (float)Math.Sin(circleAngle)) * CircleRadius;
            Vector2 toDesired = desired - NPC.Center;
            NPC.velocity = Vector2.Lerp(NPC.velocity, toDesired * 0.06f, 0.1f);

            NPC.ai[1]++;
            NPC.ai[2]++;
            float interval = enraged ? CircleShotInterval * EnrageIntervalMult : CircleShotInterval;
            if (NPC.ai[2] >= interval)
            {
                ShootFlame(player, Main.rand.NextBool((int)SeekChance));
                NPC.ai[2] = 0f;
            }
            if (NPC.ai[1] >= CircleTicks)
            {
                EnterState(State.Approach);
            }
        }

        void TickBurstFlank(Player player)
        {
            NPC.ai[1]++;
            float interval = enraged ? BurstShotInterval * EnrageIntervalMult : BurstShotInterval;
            if (NPC.ai[2] < 6f && NPC.ai[1] >= interval)
            {
                ShootFlame(player);
                NPC.ai[1] = 0f;
                NPC.ai[2]++;
                if (NPC.ai[2] == 3f)
                {
                    Vector2 offset = NPC.Center - player.Center;
                    if (offset == Vector2.Zero)
                    {
                        offset = new Vector2(Main.rand.NextBool() ? 1 : -1, 0f);
                    }
                    offset.Normalize();
                    float jitter = MathHelper.ToRadians(Main.rand.NextFloat(-20f, 20f));
                    Vector2 destination = player.Center + (-offset).RotatedBy(jitter) * FlankTeleportDistance;

                    TeleportEffect(NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0f, Main.myPlayer, 90, 18);
                    }
                    NPC.Center = destination;
                    NPC.velocity = Vector2.Zero;
                    NPC.netUpdate = true;
                    TeleportEffect(NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0f, Main.myPlayer, 90, 18);
                    }
                }
            }
            else if (NPC.ai[2] >= 6f)
            {
                EnterState(State.Approach);
            }
        }

        void TickHoverBob(Player player)
        {
            NPC.ai[1]++;
            NPC.ai[2]++;
            float bob = (float)Math.Sin(NPC.ai[1] * 0.05f) * 20f;
            Vector2 desired = new Vector2(player.Center.X + hoverSide * 180f, player.Center.Y - 60f + bob);
            Vector2 toDesired = desired - NPC.Center;
            NPC.velocity = Vector2.Lerp(NPC.velocity, toDesired * 0.05f, 0.08f);

            float interval = enraged ? HoverShotInterval * EnrageIntervalMult : HoverShotInterval;
            if (NPC.ai[2] >= interval)
            {
                ShootFlame(player, Main.rand.NextBool((int)SeekChance));
                NPC.ai[2] = 0f;
            }
            if (NPC.ai[1] >= HoverTicks)
            {
                EnterState(State.Approach);
            }
        }

        void TickNovaRing(Player player)
        {
            NPC.velocity *= 0.9f;
            NPC.ai[1]++;

            if (NPC.ai[1] < NovaTelegraphTicks)
            {
                float t = NPC.ai[1] / NovaTelegraphTicks;
                Lighting.AddLight(NPC.Center, 0.2f + t * 0.5f, 0.3f + t * 0.5f, 0.9f);
                SpawnInwardChargeDust(NPC.Center, t);
            }
            else if (NPC.ai[1] == NovaTelegraphTicks)
            {
                FireNovaRing();
            }
            else if (NPC.ai[1] >= NovaTelegraphTicks + NovaRecoverTicks)
            {
                EnterState(State.Approach);
            }
        }

        void FireNovaRing()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.9f, Pitch = -0.2f }, NPC.Center);
            UsefulFunctions.DustRingPrecise(NPC.Center, 30f, DustID.BlueTorch, 30, 0f, 150, 1.5f);
            for (int i = 0; i < NovaShotCount; i++)
            {
                float angle = MathHelper.TwoPi * i / NovaShotCount;
                Vector2 velocity = angle.ToRotationVector2() * FlameSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.EvilEyeFlame>(), (int)FlameDamage, 1f, Main.myPlayer);
            }
        }

        void TickChargeTelegraph(Player player)
        {
            NPC.velocity *= 0.85f;
            NPC.ai[1]++;

            float telegraphDuration = enraged ? ChargeTelegraphTicks * 0.7f : ChargeTelegraphTicks;
            float t = NPC.ai[1] / telegraphDuration;
            Lighting.AddLight(NPC.Center, 0.2f + t * 0.6f, 0.3f + t * 0.6f, 1f);
            SpawnInwardChargeDust(NPC.Center, t);

            if (NPC.ai[1] >= telegraphDuration)
            {
                Vector2 toPlayer = player.Center - NPC.Center;
                if (toPlayer == Vector2.Zero)
                {
                    toPlayer = new Vector2(NPC.direction, 0f);
                }
                toPlayer.Normalize();
                dashDirection = toPlayer;
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.8f }, NPC.Center);
                NPC.damage = (int)ChargeContactDamage;
                // Commit flash: one bright frame right at the exact instant the dash is locked in,
                // beyond the ramping windup glow above - the unambiguous "now" tell.
                Lighting.AddLight(NPC.Center, 1.2f, 1.4f, 2.4f);
                commitFlashTimer = 8;
                EnterState(State.ChargeDash);
            }
        }

        void TickChargeDash(Player player)
        {
            NPC.ai[1]++;
            NPC.velocity = dashDirection * DashSpeed;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BlueTorch, 0, 0, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }

            if (NPC.ai[1] >= DashTicks)
            {
                EnterChargeRecover();
            }
        }

        void TickChargeRecover(Player player)
        {
            NPC.velocity *= 0.9f;
            NPC.ai[1]++;
            if (NPC.ai[1] >= ChargeRecoverTicks)
            {
                dashCooldown = enraged ? (int)(DashCooldownTicks * EnrageIntervalMult) : DashCooldownTicks;
                EnterState(State.Approach);
            }
        }

        public override void AI()
        {
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            CheckEnrage();

            #region Frame animation
            int frameHeight = FrameHeight;
            State state = (State)NPC.ai[0];
            bool charging = state == State.ChargeTelegraph || state == State.ChargeDash;

            if (state == State.ChargeDash)
            {
                NPC.direction = dashDirection.X < 0 ? -1 : 1;
            }
            else
            {
                NPC.direction = player.Center.X < NPC.Center.X ? -1 : 1;
            }
            NPC.spriteDirection = NPC.direction;
            NPC.rotation = NPC.velocity.X * 0.08f;

            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= (charging ? 4.0 : 8.0))
            {
                NPC.frame.Y += frameHeight;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
            #endregion

            if (dashCooldown > 0)
            {
                dashCooldown--;
            }
            if (groundBlastCooldown > 0)
            {
                groundBlastCooldown--;
            }
            if (commitFlashTimer > 0)
            {
                commitFlashTimer--;
            }

            // Idle ambiance - a faint constant glow and drifting motes even when it isn't
            // doing anything special, so it reads as magical at rest rather than inert.
            if (!charging)
            {
                Lighting.AddLight(NPC.Center, enraged ? 0.5f : 0.15f, enraged ? 0.3f : 0.25f, enraged ? 0.9f : 0.55f);
                if (Main.rand.NextBool(20))
                {
                    int idleDust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BlueTorch, 0, 0, 200, default, 0.8f);
                    Main.dust[idleDust].noGravity = true;
                    Main.dust[idleDust].velocity *= 0.1f;
                }
            }

            if (state == State.Approach || state == State.Circle || state == State.HoverBob)
            {
                if (TryStartCharge(player))
                {
                    state = State.ChargeTelegraph;
                }
            }

            if (state != State.ChargeTelegraph && state != State.ChargeDash && state != State.ChargeRecover)
            {
                TryDropGroundBlast(player);
            }

            switch (state)
            {
                case State.Approach:
                    TickApproach(player);
                    break;
                case State.Circle:
                    TickCircle(player);
                    break;
                case State.BurstFlank:
                    TickBurstFlank(player);
                    break;
                case State.HoverBob:
                    TickHoverBob(player);
                    break;
                case State.NovaRing:
                    TickNovaRing(player);
                    break;
                case State.ChargeTelegraph:
                    TickChargeTelegraph(player);
                    break;
                case State.ChargeDash:
                    TickChargeDash(player);
                    break;
                case State.ChargeRecover:
                    TickChargeRecover(player);
                    break;
            }

            NPC.noTileCollide = true;
            NPC.noGravity = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if ((State)NPC.ai[0] != State.ChargeDash)
            {
                return;
            }

            if (tsorcRevampWorld.CompletedDynamicEvents.Contains(DashTeleportEventID))
            {
                EnterChargeRecover();
                return;
            }

            tsorcRevampWorld.CompletedDynamicEvents.Add(DashTeleportEventID);
            TeleportPlayerAway(target);
            // The eye's job is done - despawn it rather than resume attacking.
            // Bypasses OnKill (matches GaibonFireball.cs's NPC.active=false convention)
            // so this escape doesn't also grant coins/gores as if it were defeated.
            DespawnAfterTeleport();
        }

        void TeleportPlayerAway(Player target)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                // Departure burst - player's current position, right before they vanish.
                SoundEngine.PlaySound(SoundID.Item6, target.Center);
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 1f, Pitch = -0.4f }, target.Center);
                Lighting.AddLight(target.Center, 0.6f, 0.9f, 2.2f);
                UsefulFunctions.DustRingPrecise(target.Center, 10f, DustID.BlueTorch, 40, 4f, 140, 1.7f);
                for (int i = 0; i < 25; i++)
                {
                    int dust = Dust.NewDust(target.position, target.width, target.height, DustID.BlueTorch, 0, 0, 100, default, 2.5f);
                    Main.dust[dust].noGravity = true;
                }
                UsefulFunctions.ScreenShake(target.Center, 18f, 20);
            }

            target.SafeTeleport(new Vector2(6982f * 16f, 531f * 16f));

            if (Main.netMode != NetmodeID.Server)
            {
                // Arrival burst - the new position, right after they land.
                SoundEngine.PlaySound(SoundID.Item6, target.Center);
                Lighting.AddLight(target.Center, 0.6f, 0.9f, 2.2f);
                UsefulFunctions.DustRingPrecise(target.Center, 10f, DustID.BlueTorch, 40, -4f, 140, 1.7f);
                for (int i = 0; i < 20; i++)
                {
                    int dust = Dust.NewDust(target.position, target.width, target.height, DustID.BlueTorch, 0, 0, 100, default, 2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 2f;
                }
                UsefulFunctions.ScreenShake(target.Center, 12f, 16);
            }
        }

        void DespawnAfterTeleport()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath5, NPC.Center);
                UsefulFunctions.DustRingPrecise(NPC.Center, 10f, DustID.BlueTorch, 30, 2.5f, 120, 1.4f);
                for (int i = 1; i <= 3; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-20, 21) * 0.1f, Main.rand.Next(-40, -10) * 0.1f), Mod.Find<ModGore>($"Cloud Gore {i}").Type, 1f);
                }
            }
            NPC.active = false;
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            // Shatter beat - it's an eye, so it "breaks" before the smoke puffs cover the exit.
            SoundEngine.PlaySound(SoundID.Shatter, NPC.position);
            for (int i = 0; i < 15; i++)
            {
                int shard = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceTorch, 0f, 0f, 0, default, Main.rand.NextFloat(1f, 1.6f));
                Main.dust[shard].noGravity = true;
                Main.dust[shard].velocity *= Main.rand.NextFloat(2f, 5f);
            }

            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f, Pitch = 0.2f }, NPC.position);
            UsefulFunctions.DustRingPrecise(NPC.Center, 20f, DustID.BlueTorch, 36, 3f, 120, 1.5f);
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BlueTorch, 0f, 0f, 100, default, Main.rand.NextFloat(1.2f, 2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= Main.rand.NextFloat(1f, 3f);
            }
            for (int i = 1; i <= 3; i++)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-40, -10) * 0.2f), Mod.Find<ModGore>($"Cloud Gore {i}").Type, 1f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            State state = (State)NPC.ai[0];
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);
            Rectangle sourceRect = new Rectangle(NPC.frame.X, NPC.frame.Y, FrameWidth, FrameHeight);
            Vector2 drawCenter = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);

            if (state == State.ChargeTelegraph || state == State.ChargeDash)
            {
                // Enrage warms the trail toward orange-red as an extra "this is more dangerous now" tell.
                Color baseTint = enraged ? Color.Lerp(new Color(120, 170, 255), Color.OrangeRed, 0.35f) : new Color(120, 170, 255);

                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] - screenPos + origin + new Vector2(0f, NPC.gfxOffY);
                    float alphaFrac = (float)(NPC.oldPos.Length - k) / NPC.oldPos.Length;
                    Color trailColor = baseTint * (alphaFrac * 0.6f);
                    spriteBatch.Draw(texture, drawPos, sourceRect, trailColor, NPC.rotation, origin, NPC.scale, effects, 0f);
                }
            }

            if (commitFlashTimer > 0)
            {
                // Flash-to-white overlay drawn on top of the normal sprite, fading out over its lifetime.
                float flashFrac = commitFlashTimer / 8f;
                spriteBatch.Draw(texture, drawCenter, sourceRect, Color.White * flashFrac, NPC.rotation, origin, NPC.scale, effects, 0f);
            }

            return true;
        }
    }
}
