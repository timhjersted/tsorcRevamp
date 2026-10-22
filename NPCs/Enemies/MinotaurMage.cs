using Terraria.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Minotaur;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies{
	// Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
	//
	// Fire-staff caster, rebuilt with the Gigas Method (see tsorcDocs/EnemyRedesignSkillGuide.md)
	// at common-enemy scale: 6 attacks (2 gated below half health), short telegraphs, brisk cooldowns.
	// All attack language is dust and projectiles — the sprite only has walk/idle/jump frames, but it
	// holds a staff, so casts read from a glowing staff tip. Its historic identity (the old
	// MNPC.teleporterAI) survives as the Blink Volley. Below half health the beast under the robes
	// comes out: a hyper-armored horn charge and a desperation cinder burst.
	//
	// Poise contract at small scale: standard-fighter tier (35f in PoiseProfiles). Short telegraphs
	// are hyper-armored; the one long cast (Ring of Cinders) is staggerable for its first two thirds.
	// Recovery windows have no armor — same rules as the giants, much smaller numbers.
	public class MinotaurMage : ModNPC, IStaggerable
	{
        enum AttackState : byte
        {
            None = 0,
            EmberBolts,     // staff glint → three quick fire bolts
            FlameWave,      // staff slam → ground-crawling fire waves both ways
            BlinkVolley,    // blink to the player's flank, gather fire for 30t, two aimed bolts
            BullhornCharge, // below 50%: snorting windup → hyper-armored horn rush, big punish recovery
            RingOfCinders,  // below 50%: long staggerable cast → radial arcing embers
            FlankingFlames, // burning floor patches six tiles either side of the player
        }

        //Timings (ticks)
        const int BoltsTelegraphTicks = 25;
        const int WaveTelegraphTicks = 55;
        const int BlinkTelegraphTicks = 18;
        const int BlinkShotDelay = 30;
        const int FloorTelegraphTicks = 45;
        const int ChargeTelegraphTicks = 35;
        const int ChargeTicks = 55;
        const int ChargeRecoveryTicks = 45;
        const int CindersTelegraphTicks = 40;
        const int CindersStaggerableTicks = 26; //first two thirds: a poise break cancels the cast

        // True Expert hit damage, before defense, damage variation and world-progress scaling.
        // Includes the +40 attack increase. Apply the SHM multiplier before converting once at spawn.
        const int BoltDamage = 88;
        const int WaveDamage = 96;
        const int CinderDamage = 84;
        const int ContactDamage = 58;
        const int ChargeContactDamage = 76;
        const float SuperHardmodeDamageMultiplier = 1.25f;
        const float ChargeSpeed = 5.5f;

        static int StageDamage(int expertDamage) => tsorcRevampWorld.SuperHardMode
            ? (int)Math.Round(expertDamage * SuperHardmodeDamageMultiplier, MidpointRounding.AwayFromZero)
            : expertDamage;

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;
        int lockedDir = 1;          //horn-charge direction, locked at charge start
        bool AngrySnortDone;        //one-time below-half flourish
        readonly List<AttackState> attackBag = new(); // selection is server-only
        Vector2 floorCastAnchor;

        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailCacheLength[NPC.type] = 6; //afterimages for the horn charge
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            // Defense is explicitly authored per stage; do not add vanilla's hidden Hardmode stat budget.
            NPCID.Sets.DontDoHardmodeScaling[Type] = true;
        }

        public override void SetDefaults()
		{
			NPC.width = 20;
			NPC.height = 40;
            EnemyDamage.SetContact(NPC, StageDamage(ContactDamage));
            NPC.defense = tsorcRevampWorld.SuperHardMode ? 44 : Main.hardMode ? 12 : 4;
			NPC.lifeMax = 155;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 5000f;
			NPC.npcSlots = 3;
            NPC.scale = 1.1f;
			NPC.knockBackResist = 0.6f; //overridden by the PoiseProfiles entry
			Main.npcFrameCount[NPC.type] = 15;
			AnimationType = 21;
		}

        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write((sbyte)lockedDir);
            writer.Write(AngrySnortDone);
            writer.Write(floorCastAnchor.X);
            writer.Write(floorCastAnchor.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            lockedDir = reader.ReadSByte();
            AngrySnortDone = reader.ReadBoolean();
            floorCastAnchor = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            if (State == AttackState.BlinkVolley && AttackTimer == BlinkTelegraphTicks)
                BlinkFlash();
        }

        ///<summary>The glowing tip of its staff — where every cast's dust language originates.</summary>
        Vector2 StaffTip => NPC.Center + new Vector2(NPC.direction * 18f, -14f);

        ///<summary>Poise break (neutral or the Ring of Cinders staggerable window): the cast fizzles.</summary>
        public void OnStagger(NPC npc)
        {
            if (State != AttackState.None && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, vel.X, vel.Y, 100, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                State = AttackState.None;
                AttackTimer = 0;
                AttackCooldown = Math.Max(AttackCooldown, 60);
                SetContactDamage(false);
                NPC.netUpdate = true;
            }
        }

		public override void AI()
		{
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NPC.TargetClosest();
            Player player = Main.player[NPC.target];
            g.AttackTelegraphing = false;
            g.AttackCommitted = false;

            //Staggered: rooted by the global system; slumped lean
            if (g.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.2f, 0.1f);
                return;
            }
            NPC.rotation *= 0.85f;

            //One-time below-half flourish: an angry snort, and the phase attacks unlock
            if (!AngrySnortDone && NPC.life < NPC.lifeMax / 2)
            {
                AngrySnortDone = true;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.35f, Pitch = 0.5f }, NPC.Center);
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 nose = NPC.Center + new Vector2(NPC.direction * 12f, -10f);
                        int puff = Dust.NewDust(nose, 8, 8, DustID.Smoke, NPC.direction * 2f, 0f, 120, default, 1.1f);
                        Main.dust[puff].velocity *= 0.5f;
                    }
                }
                if (Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
            }

            if (State == AttackState.None)
            {
                SetContactDamage(false);
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 1.3f, acceleration: 0.7f, canTeleport: false, canPounce: false);

                //Idle staff glow — its element reads at a glance
                if (!Main.dedServ && Main.rand.NextBool(4))
                {
                    int glow = Dust.NewDust(StaffTip, 4, 4, DustID.Torch, 0f, -0.5f, 120, default, 0.8f);
                    Main.dust[glow].noGravity = true;
                    Main.dust[glow].velocity *= 0.2f;
                }
                Lighting.AddLight(StaffTip, 0.25f, 0.15f, 0.05f);

                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackCooldown <= 0 && NPC.velocity.Y == 0f
                    && !player.dead && player.active && NPC.Distance(player.Center) < 1000f)
                {
                    PickAttack(player);
                }
            }
            else
            {
                RunAttack(g, player);
            }
		}

        void PickAttack(Player player)
        {
            float distTiles = NPC.Distance(player.Center) / 16f;
            bool sameLevel = Math.Abs(player.Bottom.Y - NPC.Bottom.Y) < 4 * 16f;
            bool belowHalf = NPC.life < NPC.lifeMax / 2;

            bool Eligible(AttackState attack) => attack switch
            {
                AttackState.FlameWave => sameLevel && distTiles < 34f,
                AttackState.BullhornCharge => belowHalf && distTiles > 6f && distTiles < 30f && sameLevel,
                AttackState.RingOfCinders => belowHalf,
                _ => true,
            };
            // Deal every eligible attack once before refilling; never immediately repeat.
            attackBag.RemoveAll(attack => !Eligible(attack));
            if (attackBag.Count == 0)
            {
                foreach (AttackState attack in Enum.GetValues<AttackState>())
                    if (attack != AttackState.None && Eligible(attack)) attackBag.Add(attack);
                for (int i = attackBag.Count - 1; i > 0; i--)
                {
                    int j = Main.rand.Next(i + 1);
                    (attackBag[i], attackBag[j]) = (attackBag[j], attackBag[i]);
                }
            }
            int pick = attackBag.Count - 1;
            if (attackBag[pick] == LastAttack && pick > 0) pick--;
            AttackState chosen = attackBag[pick];
            attackBag.RemoveAt(pick);
            State = chosen;
            AttackTimer = 0;
            if (chosen == AttackState.FlankingFlames)
            {
                floorCastAnchor = player.Bottom;
                SpawnFlankingFlames();
            }
            NPC.netUpdate = true;
        }

        void EndAttack(int cooldown)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            LastAttack = State;
            State = AttackState.None;
            AttackTimer = 0;
            //Safety: restore anything an attack changed
            SetContactDamage(false);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = cooldown + Main.rand.Next(40);
                NPC.netUpdate = true;
            }
        }

        void RunAttack(tsorcRevampGlobalNPC g, Player player)
        {
            if (player.dead || !player.active)
            {
                EndAttack(60);
                return;
            }
            //Face the player, except mid-charge (direction locked at its start)
            if (!(State == AttackState.BullhornCharge && AttackTimer > ChargeTelegraphTicks))
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            }
            NPC.spriteDirection = NPC.direction;

            AttackTimer++;
            switch (State)
            {
                case AttackState.EmberBolts: RunEmberBolts(g, player); break;
                case AttackState.FlameWave: RunFlameWave(g); break;
                case AttackState.BlinkVolley: RunBlinkVolley(g, player); break;
                case AttackState.BullhornCharge: RunBullhornCharge(g); break;
                case AttackState.RingOfCinders: RunRingOfCinders(g); break;
                case AttackState.FlankingFlames: RunFlankingFlames(g); break;
            }
        }

        void RunEmberBolts(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= BoltsTelegraphTicks + 16;
            NPC.velocity.X *= 0.85f;

            if (AttackTimer <= BoltsTelegraphTicks + 16 && !Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.2f }, NPC.Center);
                }
                //Staff tip ramps up: fire gathering into the cast point
                float progress = MathHelper.Clamp(AttackTimer / (float)BoltsTelegraphTicks, 0f, 1f);
                for (int i = 0; i < 1 + (int)(progress * 2f); i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = StaffTip + angle.ToRotationVector2() * Main.rand.NextFloat(8f, 24f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Torch, 0f, 0f, 100, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (StaffTip - pos) * 0.15f;
                }
                Lighting.AddLight(StaffTip, 0.5f * progress, 0.3f * progress, 0.1f * progress);
            }
            //Three quick bolts, re-aimed each shot with capped intercept prediction.
            if ((AttackTimer == BoltsTelegraphTicks || AttackTimer == BoltsTelegraphTicks + 8 || AttackTimer == BoltsTelegraphTicks + 16)
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = AimBolt(player, 9f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, vel,
                    ModContent.ProjectileType<MinotaurEmberBolt>(), EnemyDamage.Projectile(StageDamage(BoltDamage)), 1f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.3f }, NPC.Center);
            }
            if (AttackTimer >= BoltsTelegraphTicks + 16 + 24) //short recovery, no armor
            {
                EndAttack(140);
            }
        }

        void RunFlameWave(tsorcRevampGlobalNPC g)
        {
            g.AttackCommitted = AttackTimer <= WaveTelegraphTicks;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= WaveTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.5f }, NPC.Center);
                }
                //Fire builds at the staff tip while the ground at its feet simmers — the wave's read
                if (!Main.dedServ)
                {
                    int dust = Dust.NewDust(StaffTip, 4, 4, DustID.Torch, 0f, -1f, 90, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                    MinotaurFireVFX.Embers(NPC.Bottom, 20f, 3);
                }
                if (AttackTimer == WaveTelegraphTicks)
                {
                    //Staff slam: both waves, armed a beat after the impact dust
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.2f }, NPC.Bottom);
                    UsefulFunctions.ScreenShake(NPC.Bottom, 3f, 10);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int dir = -1; dir <= 1; dir += 2)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom + new Vector2(dir * 16f, -16f), Vector2.Zero,
                                ModContent.ProjectileType<MinotaurFlameWave>(), EnemyDamage.Projectile(StageDamage(WaveDamage)), 2f, Main.myPlayer, dir, 8f);
                        }
                    }
                }
            }
            else
            {
                NPC.velocity.X *= 0.7f; //recovery, no armor
                if (AttackTimer >= WaveTelegraphTicks + 30)
                {
                    EndAttack(160);
                }
            }
        }

        Vector2 AimBolt(Player player, float speed)
        {
            Vector2 delta = player.Center - StaffTip;
            Vector2 motion = player.velocity;
            float a = motion.LengthSquared() - speed * speed;
            float b = 2f * Vector2.Dot(delta, motion);
            float c = delta.LengthSquared();
            float time = delta.Length() / speed;
            float discriminant = b * b - 4f * a * c;
            if (Math.Abs(a) > 0.001f && discriminant >= 0f)
            {
                float root = (float)Math.Sqrt(discriminant);
                float t1 = (-b - root) / (2f * a);
                float t2 = (-b + root) / (2f * a);
                if (t1 > 0f || t2 > 0f)
                    time = t1 > 0f && t2 > 0f ? Math.Min(t1, t2) : Math.Max(t1, t2);
            }
            // Short prediction improves accuracy without aiming far past sudden turns.
            return (delta + motion * MathHelper.Clamp(time, 0f, 24f))
                .SafeNormalize(new Vector2(NPC.direction, 0f)) * speed;
        }

        void SpawnFlankingFlames()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            foreach (Player target in Main.player)
            {
                if (!target.active || target.dead || NPC.Distance(target.Center) > 1000f) continue;
                // Three tiles per patch, centered six tiles either side. Leave the player's floor open.
                foreach (int offset in new[] { 0, -1, 1 })
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float x = target.Center.X + (side * 6 + offset) * 16f;
                        float y = FindGroundY(new Vector2(x, target.Bottom.Y - 48f), 15);
                        if (y < 0f) continue;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(x, y - 14f), Vector2.Zero,
                            ModContent.ProjectileType<MinotaurFloorFlame>(), EnemyDamage.Projectile(StageDamage(WaveDamage)), 1f, Main.myPlayer,
                            FloorTelegraphTicks + Math.Abs(offset) * 3);
                    }
                }
            }
        }

        void RunFlankingFlames(tsorcRevampGlobalNPC g)
        {
            NPC.velocity.X *= 0.8f;
            g.AttackCommitted = AttackTimer <= FloorTelegraphTicks;
            if (!Main.dedServ && AttackTimer <= FloorTelegraphTicks)
            {
                MinotaurFireVFX.Embers(StaffTip, 5f, 2);
                if (AttackTimer == 1)
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.4f }, floorCastAnchor);
            }
            if (AttackTimer >= FloorTelegraphTicks + 30) EndAttack(200);
        }

        void RunBlinkVolley(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= BlinkTelegraphTicks + BlinkShotDelay + 8;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= BlinkTelegraphTicks)
            {
                //Winding up the blink: the classic white teleport shimmer, in fire
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, 0f, 120, Color.White, 1.2f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity = (NPC.Center - Main.dust[dust].position) * 0.1f;
                    }
                }
                if (AttackTimer == BlinkTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //Blink to the player's flank, ground-snapped; stay put if there's nowhere to go
                    int side = Main.rand.NextBool() ? 1 : -1;
                    float destX = player.Center.X + side * Main.rand.NextFloat(10f, 14f) * 16f;
                    float groundY = FindGroundY(new Vector2(destX, player.Bottom.Y - 48f), 15);
                    if (groundY > 0f && !Collision.SolidCollision(new Vector2(destX - NPC.width / 2f, groundY - NPC.height), NPC.width, NPC.height))
                    {
                        BlinkFlash();
                        NPC.Bottom = new Vector2(destX, groundY);
                        NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
                        NPC.spriteDirection = NPC.direction;
                        NPC.velocity = Vector2.Zero;
                        BlinkFlash();
                        NPC.netUpdate = true;
                    }
                }
            }
            // Hold a full 30 ticks after teleporting; continue gathering through both shots.
            if (AttackTimer > BlinkTelegraphTicks && AttackTimer <= BlinkTelegraphTicks + BlinkShotDelay + 8)
                MinotaurFireVFX.Embers(StaffTip, 5f, 2);
            if ((AttackTimer == BlinkTelegraphTicks + BlinkShotDelay || AttackTimer == BlinkTelegraphTicks + BlinkShotDelay + 8)
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = AimBolt(player, 9.5f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, vel,
                    ModContent.ProjectileType<MinotaurEmberBolt>(), EnemyDamage.Projectile(StageDamage(BoltDamage)), 1f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.4f }, NPC.Center);
            }
            if (AttackTimer >= BlinkTelegraphTicks + BlinkShotDelay + 8 + 22) //recovery, no armor
            {
                EndAttack(150);
            }
        }

        void SetContactDamage(bool charging)
        {
            int expertDamage = StageDamage(charging ? ChargeContactDamage : ContactDamage);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                EnemyDamage.SetContact(NPC, expertDamage);
            else
            {
                // Mirror the synced attack and world stage with the same difficulty conversion.
                // The rush has its own +40 baseline; it no longer deals twice normal contact.
                NPC.damage = EnemyDamage.ScaleContact(expertDamage);
                NPC.defDamage = NPC.damage;
            }
        }

        void RunBullhornCharge(tsorcRevampGlobalNPC g)
        {
            if (AttackTimer <= ChargeTelegraphTicks)
            {
                g.AttackCommitted = true;
                NPC.velocity.X *= 0.8f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    lockedDir = NPC.direction;
                    if (AttackTimer == ChargeTelegraphTicks) NPC.netUpdate = true;
                }
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.4f, Pitch = 0.5f }, NPC.Center);
                }
                //Pawing the ground + snorting smoke — unmistakably a bull about to run you down
                if (!Main.dedServ && AttackTimer % 12 < 3)
                {
                    Vector2 hoof = NPC.Bottom + new Vector2(NPC.direction * 8f, -4f);
                    int dust = Dust.NewDust(hoof, 8, 4, DustID.Smoke, -NPC.direction * 2f, -1f, 100, default, 1f);
                    Main.dust[dust].velocity *= 0.5f;
                }
                if (!Main.dedServ && AttackTimer % 15 == 0)
                {
                    Vector2 nose = NPC.Center + new Vector2(NPC.direction * 12f, -10f);
                    int puff = Dust.NewDust(nose, 8, 8, DustID.Smoke, NPC.direction * 2.5f, 0f, 120, default, 1.1f);
                    Main.dust[puff].velocity *= 0.6f;
                }
            }
            else if (AttackTimer <= ChargeTelegraphTicks + ChargeTicks)
            {
                //The rush: hyper-armored, harder contact hit, afterimage trail (PreDraw)
                g.AttackCommitted = true;
                SetContactDamage(true);
                NPC.velocity.X = lockedDir * ChargeSpeed;
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(NPC.position + new Vector2(0f, NPC.height - 8f), NPC.width, 8, DustID.Smoke, -lockedDir * 2f, -0.5f, 120, default, 1f);
                    Main.dust[dust].velocity *= 0.4f;
                }
                //Hit a wall: skip straight to the recovery
                if (Main.netMode != NetmodeID.MultiplayerClient && NPC.collideX && AttackTimer > ChargeTelegraphTicks + 8)
                {
                    AttackTimer = ChargeTelegraphTicks + ChargeTicks;
                    NPC.netUpdate = true;
                }
            }
            else
            {
                //Blown past you and winded: the punish window
                SetContactDamage(false);
                NPC.velocity.X *= 0.8f;
                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    Vector2 nose = NPC.Center + new Vector2(NPC.direction * 12f, -10f);
                    int puff = Dust.NewDust(nose, 8, 8, DustID.Smoke, NPC.direction * 1.5f, 0.3f, 140, default, 0.9f);
                    Main.dust[puff].velocity *= 0.4f;
                }
                if (AttackTimer >= ChargeTelegraphTicks + ChargeTicks + ChargeRecoveryTicks)
                {
                    EndAttack(220);
                }
            }
        }

        void RunRingOfCinders(tsorcRevampGlobalNPC g)
        {
            if (AttackTimer <= CindersTelegraphTicks)
            {
                NPC.velocity.X *= 0.8f;
                //The one long cast: staggerable for its first two thirds, then committed
                if (AttackTimer < CindersStaggerableTicks)
                {
                    g.AttackTelegraphing = true;
                }
                else
                {
                    g.AttackCommitted = true;
                }
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.6f }, NPC.Center);
                }
                if (AttackTimer == CindersStaggerableTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center); //commit cue
                }
                //Fire spiralling inward to the staff
                if (!Main.dedServ)
                {
                    float progress = AttackTimer / (float)CindersTelegraphTicks;
                    for (int i = 0; i < 2; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        float radius = MathHelper.Lerp(60f, 10f, progress) + Main.rand.NextFloat(10f);
                        Vector2 pos = StaffTip + angle.ToRotationVector2() * radius;
                        int dust = Dust.NewDust(pos, 4, 4, DustID.Torch, 0f, 0f, 100, default, 1.1f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity = (StaffTip - pos) * 0.08f;
                    }
                    Lighting.AddLight(StaffTip, 0.6f * progress, 0.35f * progress, 0.1f * progress);
                }

                if (AttackTimer == CindersTelegraphTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = 0.1f }, NPC.Center);
                    if (!Main.dedServ)
                    {
                        for (int i = 0; i < 18; i++)
                        {
                            Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                            int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, vel.X, vel.Y, 60, default, 1.5f);
                            Main.dust[dust].noGravity = true;
                        }
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        //Radial arcing embers — the gaps between arcs are the dodge
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 vel = (MathHelper.TwoPi * i / 8f + Main.rand.NextFloat(0.2f)).ToRotationVector2() * 7f;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                ModContent.ProjectileType<MinotaurCinder>(), EnemyDamage.Projectile(StageDamage(CinderDamage)), 1f, Main.myPlayer, 0.2f);
                        }
                    }
                }
            }
            else
            {
                NPC.velocity.X *= 0.7f; //recovery, no armor
                if (AttackTimer >= CindersTelegraphTicks + 35)
                {
                    EndAttack(200);
                }
            }
        }

        void BlinkFlash()
        {
            if (Main.dedServ)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            for (int m = 0; m < 20; m++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0, 0, 100, Color.White, 1.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Main.rand.NextVector2Circular(6f, 6f);
            }
        }

        ///<summary>World Y of the first solid tile top under the point (scanning down), or -1 if none in range.</summary>
        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int d = 0; d <= maxTilesDown; d++)
            {
                int y = ty + d;
                if (y < 5 || y >= Main.maxTilesY - 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                {
                    return y * 16f;
                }
            }
            return -1f;
        }

        ///<summary>Smoke-and-ember afterimage trail during the horn charge.</summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (State == AttackState.FlameWave && AttackTimer <= WaveTelegraphTicks)
            {
                float progress = AttackTimer / (float)WaveTelegraphTicks;
                MinotaurFireVFX.Draw(NPC.Bottom - new Vector2(0f, 12f), new Vector2(52f, 28f),
                    0.25f + progress * 0.35f, NPC.whoAmI * 0.17f);
            }
            bool aimingBolts = State == AttackState.EmberBolts && AttackTimer <= BoltsTelegraphTicks + 16;
            bool aimingBlink = State == AttackState.BlinkVolley && AttackTimer > BlinkTelegraphTicks
                && AttackTimer <= BlinkTelegraphTicks + BlinkShotDelay + 8;
            if (aimingBolts || aimingBlink || (State == AttackState.FlankingFlames && AttackTimer <= FloorTelegraphTicks))
            {
                MinotaurFireVFX.Draw(StaffTip, new Vector2(18f, 24f), 0.55f, NPC.whoAmI * 0.17f);
                if (aimingBolts || aimingBlink)
                {
                    Vector2 aim = AimBolt(Main.player[NPC.target], aimingBlink ? 9.5f : 9f).SafeNormalize(Vector2.UnitX);
                    for (int step = 1; step <= 18; step++)
                    {
                        Vector2 point = StaffTip + aim * step * 8f - screenPos;
                        spriteBatch.Draw(TextureAssets.MagicPixel.Value, point, null,
                            new Color(255, 100, 20, 0) * (0.25f * (1f - step / 19f)), 0f,
                            Vector2.Zero, new Vector2(2f), SpriteEffects.None, 0f);
                    }
                }
            }
            bool charging = State == AttackState.BullhornCharge && AttackTimer > ChargeTelegraphTicks && AttackTimer <= ChargeTelegraphTicks + ChargeTicks;
            if (charging || NPC.velocity.Length() > 7f)
            {
                Texture2D texture = TextureAssets.Npc[NPC.type].Value;
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height); //bottom-center
                for (int k = NPC.oldPos.Length - 1; k > 0; k -= 2)
                {
                    Vector2 drawPos = NPC.oldPos[k] + new Vector2(NPC.width / 2f, NPC.height + NPC.gfxOffY + 4f) - screenPos;
                    Color color = new Color(255, 140, 60, 0) * ((NPC.oldPos.Length - k) / (float)NPC.oldPos.Length) * 0.5f;
                    spriteBatch.Draw(texture, drawPos, NPC.frame, color, NPC.rotation, origin, NPC.scale, effects, 0f);
                }
            }
            return true;
        }

        public override void OnKill()
        {
            if (Main.dedServ) return;
            //Its fire escapes it: the classic torch-dust burst, kept from the original
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0, 0, 100, default, 1.5f);
                Main.dust[dust].noGravity = false;
            }
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore3").Type, 1f);
        }
    }
}
