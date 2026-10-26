using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // Corrupted Hornet — SHM underground-jungle flyer. Hovers 160-300px from its target (FloaterAI: flying A*, keeps line of
    // sight so stinger shots never spawn in walls) and deals one of two attacks from an AttackBag:
    //   Abyss Volley  40t buzz + shudder, flame motes gather on the stinger / 3 Abyss Flames in a 24° fan from the stinger / 30t
    //   Stinger Dive  35t pull-back + fast flap / distance-solved dive that overshoots you by 120px, 2x contact damage /
    //                 40t slow drift (punish window)
    // The sprite only flaps, so every tell is dust + sound + doubled flap tempo + a body shudder. Art faces LEFT.
    // MP: the server picks attacks and locks the dive velocity; state/timers/dive are synced in SendExtraAI and both sides run
    // the same timeline. Projectiles spawn on the server only; dust/sound on clients only. Reference: OolacileDemon.
    class CorruptedHornet : ModNPC, IStaggerable, IDebugAttackLabel
    {
        enum HornetAttack : byte
        {
            None,
            AbyssVolley,
            StingerDive,
        }

        // Raw spawn damage, same convention as before the rework.
        const int ContactDamage = 48;
        const int AbyssFlameDamage = 50;

        // Hover band and speed for FloaterAI (px, px/tick).
        const float HoverMin = 160f;
        const float HoverMax = 300f;
        const float HoverTopSpeed = 5f;
        const float HoverAcceleration = 0.08f;

        // Neutral pause between attacks, after each attack's own recovery (ticks). Old AI fired roughly every 100-200t.
        const int NeutralPauseMin = 50;
        const int NeutralPauseMax = 80;

        // Abyss Volley. 8 px/t: from the near edge of the hover band (160px) that is 20t to react, after a 40t tell.
        const int VolleyTellTicks = 40;
        const int VolleyRecoveryTicks = 30;
        const int VolleyShots = 3;
        const float VolleySpreadDegrees = 24f;
        const float VolleySpeed = 8f;
        const float VolleyMaxRange = 640f;

        // Stinger Dive. Max dive = 380px + 120px overshoot = 500px / 12 px/t ≈ 42t, inside the 45t cap.
        const int DiveRearTicks = 35;
        const int DiveRecoveryTicks = 40;
        const float DiveSpeed = 12f;
        const float DiveOvershoot = 120f;
        const int DiveDamageMultiplier = 2;
        const int DiveMinTicks = 10;
        const int DiveMaxTicks = 45;
        const float DiveMinRange = 100f;
        const float DiveMaxRange = 380f;

        // Animation: 3-frame wing flap, a frame every FrameTicks; telegraphs flap twice as fast.
        const int FrameTicks = 4;

        HornetAttack State = HornetAttack.None;
        int AttackTimer;
        int AttackCooldown = 60;
        Vector2 lockedVelocity;
        int diveTicks;

        // Server-only selection state (never synced; clients follow State).
        AttackBag<HornetAttack> attackBag;

        readonly FloaterAI.FloaterState floatState = new FloaterAI.FloaterState();

        ///<summary>Stinger tip: frame px (1,65) of a 50x70 frame. Vanilla draws the frame bottom-aligned 4px below the 35px hitbox,
        ///so the frame centre sits 13.5px above NPC.Center; the tip is (24,30) from that centre, rotated with the sprite.</summary>
        Vector2 Stinger
        {
            get
            {
                Vector2 frameCenter = NPC.Center + new Vector2(0f, -13.5f);
                Vector2 tipOffset = new Vector2(24f * NPC.direction, 30f).RotatedBy(NPC.rotation);
                return frameCenter + tipOffset;
            }
        }

        bool Diving => State == HornetAttack.StingerDive && AttackTimer > DiveRearTicks && AttackTimer <= DiveRearTicks + diveTicks;

        bool IsTelegraphing
        {
            get
            {
                if (State == HornetAttack.AbyssVolley)
                {
                    return AttackTimer < VolleyTellTicks;
                }

                if (State == HornetAttack.StingerDive)
                {
                    return AttackTimer <= DiveRearTicks;
                }

                return false;
            }
        }

        public string DebugAttackLabel => State switch
        {
            HornetAttack.AbyssVolley => "Abyss Volley",
            HornetAttack.StingerDive => "Stinger Dive",
            _ => "Hover",
        };

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            NPC.width = 34;
            NPC.height = 35;
            NPC.aiStyle = -1; // was vanilla Hornet AI (5) plus a bolted-on shot timer
            NPC.knockBackResist = 0.5f; // PoiseProfiles overrides this; kept in step with it
            NPC.value = 4000; // life / 2.5 : was 113
            NPC.damage = ContactDamage;
            NPC.defense = 45;
            NPC.lavaImmune = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.noGravity = true;
            NPC.lifeMax = 2000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.CorruptedHornetBanner>();
        }

        // Legacy spawn rule, used only while the "New Enemy Spawns" config is off (Systems/EnemySpawns.cs replaces it).
        // The old "NextBool(2) ? 1 : 0" averaged 0.5; returning 0.5 gives the same rate without a roll inside SpawnChance.
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneOverworldHeight)
            {
                return 0.5f;
            }

            return 0;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.WriteVector2(lockedVelocity);
            writer.Write(diveTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (HornetAttack)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            lockedVelocity = reader.ReadVector2();
            diveTicks = reader.ReadInt32();
        }

        ///<summary>Poise break: whatever it was winding up or diving into is cancelled. Already-launched flames keep flying.</summary>
        public void OnStagger(NPC npc)
        {
            State = HornetAttack.None;
            AttackTimer = 0;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = 50;
                NPC.netUpdate = true;
            }
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;

            // The dive's body hits for DiveDamageMultiplier x contact damage. defDamage is the already difficulty-scaled
            // contact damage vanilla stores at spawn. Diving reads the synced state, so every machine agrees.
            NPC.damage = NPC.defDamage;
            if (Diving)
            {
                NPC.damage = NPC.defDamage * DiveDamageMultiplier;
            }

            // Staggered: wings falter — it sags and drifts, no attacks.
            if (globalNPC.StaggerTimer > 0)
            {
                NPC.velocity *= 0.9f;
                NPC.velocity.Y += 0.1f;
                NPC.rotation = NPC.velocity.X * 0.05f;
                return;
            }

            if (State == HornetAttack.None)
            {
                NPC.TargetClosest(false);
            }

            Player target = Main.player[NPC.target];
            bool targetValid = NPC.HasValidTarget && target.active && !target.dead;

            if (!targetValid)
            {
                State = HornetAttack.None;
                NPC.velocity *= 0.95f;
                NPC.velocity.Y -= 0.05f;
                NPC.rotation = 0f;
                NPC.EncourageDespawn(10);
                return;
            }

            if (State == HornetAttack.None)
            {
                // Neutral: buzz around in the band, face the target, tilt into the drift, and count down to the next pick.
                // Big drift (40px) so it reads as a restless insect rather than a parked turret.
                FloaterAI.Run(NPC, target, floatState, HoverTopSpeed, HoverAcceleration, HoverMin, HoverMax, driftAmount: 40f);
                FaceTowards(target.Center.X);
                NPC.rotation = MathHelper.Clamp(NPC.velocity.X * 0.05f, -0.3f, 0.3f);

                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }
                else if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ChooseAttack(target);
                }

                return;
            }

            AttackTimer++;

            if (IsTelegraphing)
            {
                globalNPC.AttackTelegraphing = true;
            }
            else
            {
                globalNPC.AttackCommitted = true;
            }

            switch (State)
            {
                case HornetAttack.AbyssVolley:
                    TickAbyssVolley(target);
                    break;
                case HornetAttack.StingerDive:
                    TickStingerDive(target);
                    break;
            }
        }

        // Server-only. Deals the next attack from the bag; a card whose gate is closed waits in the bag.
        // Nothing open (no line of sight, say) → keep hovering and look again in 20 ticks.
        void ChooseAttack(Player target)
        {
            if (attackBag == null)
            {
                attackBag = new AttackBag<HornetAttack>(FillAttackBag);
            }

            float distance = NPC.Distance(target.Center);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            bool IsOpen(HornetAttack attack)
            {
                if (!lineOfSight)
                {
                    return false;
                }

                if (attack == HornetAttack.AbyssVolley)
                {
                    return distance <= VolleyMaxRange;
                }

                if (attack == HornetAttack.StingerDive)
                {
                    return distance >= DiveMinRange && distance <= DiveMaxRange;
                }

                return false;
            }

            if (!attackBag.TryDraw(IsOpen, out HornetAttack picked))
            {
                AttackCooldown = 20;
                return;
            }

            State = picked;
            AttackTimer = 0;
            NPC.netUpdate = true;
        }

        // Volley x2 is the bread-and-butter card; the dive keeps the player from turtling at range.
        void FillAttackBag(List<HornetAttack> cards)
        {
            cards.Add(HornetAttack.AbyssVolley);
            cards.Add(HornetAttack.AbyssVolley);
            cards.Add(HornetAttack.StingerDive);
        }

        void EndAttack()
        {
            State = HornetAttack.None;
            AttackTimer = 0;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = Main.rand.Next(NeutralPauseMin, NeutralPauseMax + 1);
                NPC.netUpdate = true;
            }
        }

        void TickAbyssVolley(Player target)
        {
            NPC.velocity *= 0.9f;
            FaceTowards(target.Center.X);

            // Tell: a body shudder (deterministic sine on the tilt, so every machine draws the same) while flame motes
            // spiral in from 30px to 4px onto the stinger. Same pink/shadow dusts as the Abyss Flame's own trail.
            if (AttackTimer < VolleyTellTicks)
            {
                float shudder = (float)Math.Sin(AttackTimer * 1.6f) * 0.1f;
                NPC.rotation = shudder;

                if (AttackTimer == 1)
                {
                    PlaySound(SoundID.Item32 with { Volume = 0.8f, Pitch = 0.6f });
                }

                if (!Main.dedServ)
                {
                    float progress = AttackTimer / (float)VolleyTellTicks;
                    float radius = MathHelper.Lerp(30f, 4f, progress);
                    Vector2 stinger = Stinger;

                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                        Dust mote = Dust.NewDustPerfect(stinger + offset, DustID.PinkTorch, -offset * 0.1f, 50, default, MathHelper.Lerp(0.9f, 1.8f, progress));
                        mote.noGravity = true;
                    }

                    if (progress > 0.5f && Main.rand.NextBool(3))
                    {
                        Dust shadow = Dust.NewDustPerfect(stinger + Main.rand.NextVector2Circular(4f, 4f), DustID.FireworkFountain_Pink, Vector2.Zero, 50, default, 1.4f);
                        shadow.noGravity = true;
                    }

                    Lighting.AddLight(stinger, 0.4f * progress, 0.05f, 0.5f * progress);
                }

                return;
            }

            NPC.rotation = 0f;

            if (AttackTimer == VolleyTellTicks)
            {
                PlaySound(SoundID.Item17);

                // Server-only. VolleyShots flames spread evenly across VolleySpreadDegrees, centred on the player, from the tip.
                // ai[1] = 1 opts this shot into tile collision (the Serpent of the Abyss's rain still passes through rock).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 stinger = Stinger;
                    float centerAngle = (target.Center - stinger).ToRotation();
                    float spread = MathHelper.ToRadians(VolleySpreadDegrees);

                    for (int i = 0; i < VolleyShots; i++)
                    {
                        float step = i / (float)(VolleyShots - 1);
                        float angle = centerAngle - spread * 0.5f + spread * step;
                        Vector2 velocity = angle.ToRotationVector2() * VolleySpeed;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), stinger, velocity, ModContent.ProjectileType<AbyssFlames>(), AbyssFlameDamage, 0f, Main.myPlayer, 0f, 1f);
                    }
                }
            }

            if (AttackTimer >= VolleyTellTicks + VolleyRecoveryTicks)
            {
                EndAttack();
            }
        }

        void TickStingerDive(Player target)
        {
            int diveEnd = DiveRearTicks + diveTicks;

            // Rear: backs away from the player, wings at double tempo, shadow motes drawn into the body.
            if (AttackTimer <= DiveRearTicks)
            {
                FaceTowards(target.Center.X);

                Vector2 awayFromTarget = (NPC.Center - target.Center).SafeNormalize(-Vector2.UnitY);
                NPC.velocity = Vector2.Lerp(NPC.velocity, awayFromTarget * 3f, 0.1f);
                NPC.rotation = (float)Math.Sin(AttackTimer * 1.6f) * 0.08f;

                if (AttackTimer == 1)
                {
                    PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.3f });
                }

                if (!Main.dedServ)
                {
                    Dust shadow = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 0f, 80, default, 1.4f);
                    shadow.noGravity = true;
                    shadow.velocity = (shadow.position - NPC.Center).SafeNormalize(Vector2.Zero) * -1.5f;
                }

                // Lock the dive on the last rear tick: aimed at the player, solved to overshoot them by DiveOvershoot.
                if (AttackTimer == DiveRearTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 toTarget = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                    Vector2 diveTarget = target.Center + toTarget * DiveOvershoot;
                    float diveDistance = Vector2.Distance(NPC.Center, diveTarget);
                    int solvedTicks = (int)Math.Ceiling(diveDistance / DiveSpeed);

                    lockedVelocity = toTarget * DiveSpeed;
                    diveTicks = Math.Clamp(solvedTicks, DiveMinTicks, DiveMaxTicks);
                    NPC.netUpdate = true;
                }

                return;
            }

            // Dive: locked velocity, nose tilted along it. Hitting terrain ends it early (server decides; clients follow the timer).
            if (AttackTimer <= diveEnd)
            {
                if (AttackTimer == DiveRearTicks + 1)
                {
                    PlaySound(SoundID.Item17 with { Pitch = -0.4f });
                }

                NPC.velocity = lockedVelocity;
                FaceTowards(NPC.Center.X + lockedVelocity.X);

                // Positive rotation turns the sprite clockwise: a left-facing nose dips when rotation < 0, a right-facing one when > 0.
                float diveSlope = lockedVelocity.Y / DiveSpeed;
                NPC.rotation = diveSlope * 0.5f * NPC.direction;

                if (!Main.dedServ)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Dust trail = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 0f, 80, default, 1.6f);
                        trail.noGravity = true;
                        trail.velocity = -lockedVelocity * 0.15f;
                    }
                }

                // Skip the first dive tick: collideX/Y still describe the rear, which may have backed it into a wall.
                bool pastFirstDiveTick = AttackTimer > DiveRearTicks + 1;
                bool hitTerrain = NPC.collideX || NPC.collideY;
                if (pastFirstDiveTick && hitTerrain && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    AttackTimer = diveEnd;
                    NPC.netUpdate = true;
                }

                return;
            }

            // Recovery: coasts to a stop, level again — the window to punish it.
            NPC.velocity *= 0.92f;
            NPC.rotation *= 0.85f;

            if (AttackTimer >= diveEnd + DiveRecoveryTicks)
            {
                EndAttack();
            }
        }

        // Face a world X. The art faces left, and vanilla flips the sprite when spriteDirection == 1, so spriteDirection = facing.
        void FaceTowards(float worldX)
        {
            if (worldX > NPC.Center.X)
            {
                NPC.direction = 1;
            }
            else
            {
                NPC.direction = -1;
            }

            NPC.spriteDirection = NPC.direction;
        }

        void PlaySound(SoundStyle sound)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(sound, NPC.Center);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            // Telegraphs and the dive flap twice as fast — the sprite has no other pose to tell with.
            int flapRate = 1;
            if (IsTelegraphing || Diving)
            {
                flapRate = 2;
            }

            NPC.frameCounter += flapRate;

            if (NPC.frameCounter >= FrameTicks)
            {
                NPC.frameCounter = 0;
                int frame = NPC.frame.Y / frameHeight + 1;

                if (frame >= Main.npcFrameCount[NPC.type])
                {
                    frame = 0;
                }

                NPC.frame.Y = frame * frameHeight;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Venom, 6 * 60, false);
        }

        // Gore and death dust go in HitEffect: it runs on every client, while OnKill runs only on the server in multiplayer.
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Corrupt Hornet Gore 1").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Corrupt Hornet Gore 2").Type, 1.1f);

            for (int i = 0; i < 3; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TintableDust, 0.2f, 0.2f, 100, default, 1f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<FlameOfTheAbyss>(), 2));
        }
    }
}
