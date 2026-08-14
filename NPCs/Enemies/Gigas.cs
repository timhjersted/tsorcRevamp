using Terraria.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies{
	// Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
	//
	// Golden-magic caster giant ("holy" themed mini-boss). All attack language lives in dusts, light and
	// projectiles because the sprite only has walk/idle/jump frames: the body plants (idle) or jumps and the
	// spell does the talking. Slow, heavy, never runs — its answer to a fleeing player is magic at the
	// player's position, not legs.
	//
	// Poise contract (per design): HIGH poise (boss-tier in PoiseProfiles). Most telegraphs are fully
	// hyper-armored (AttackCommitted from the first windup frame). The one LONG telegraph — Wrath of Gold —
	// is staggerable (AttackTelegraphing) for its first two thirds, then commits; a normal poise break in
	// that window cancels the cast (IStaggerable.OnStagger) and scatters any orbiting halo suns.
	public class Gigas : ModNPC, IStaggerable, IDebugAttackLabel
	{
        enum AttackState : byte
        {
            None = 0,
            SunlightSlam,   // high jump → apex hang → trailing dive → ground slam + shockwaves
            WrathApproach,  // committed leap into threat range before Wrath of Gold can telegraph
            WrathOfGold,    // long standing channel → expanding nova ring (the staggerable cast)
            HeavenlySpears, // walking cast: spears materialize around the player and lance in
            SunPillars,     // detached 2/4/6/8-pillar judgment sequence, layered over the next move
            HaloVolley,     // summons the orbiting sun halo, which detaches sun by sun
            LightningSweep, // low horizontal blade of golden lightning — jump or roll through
            SolarBoulder,   // huge lobbed sun → bouncing embers + consecrated ground
            SolarSlabs,     // two solar monoliths hold wide, then converge in a nova clap
            LedgeLeap,      // anti-camping: telegraphed super-jump to a perch, lands as a slam
        }

        //Attack timings (ticks)
        const int SlamWindupTicks = 40;
        const int SlamApexTicks = 16;
        const int SlamRecoveryTicks = 35;
        const int WrathTelegraphTicks = 100;
        const int WrathApproachWindupTicks = 24;
        const int WrathStaggerableTicks = 66;  //first two thirds: hyper-armor OFF, a poise break cancels the cast
        const int WrathRecoveryTicks = 30;
        const int SpearsCastTicks = 132;
        const int PillarsCastTicks = 12;
        const int HaloCastTicks = 30;
        const int SweepTelegraphTicks = 70;
        const int SweepActiveTicks = 120;
        const int SweepCastTicks = SweepTelegraphTicks + SweepActiveTicks;
        const int BoulderCastTicks = 45;
        const int SolarSlabsCastTicks = 30;
        const int LeapWindupTicks = 45;

        //Projectile damage, tuned for a LATE-HARDMODE boss event (hostile projectiles deal 2x this
        //on hit in normal mode). SuperHardMode encounters additionally run every value through
        //ScaleDamage (×1.3 × SHMScale, the ArcherAI convention for SHM enemies).
        int ShockwaveDamage => ScaleDamage(60);
        int NovaDamage => ScaleDamage(70);
        int SpearDamage => ScaleDamage(45);
        int PillarDamage => ScaleDamage(55);
        int HaloSunDamage => ScaleDamage(38);
        int SweepDamage => ScaleDamage(70);
        int BoulderDamage => ScaleDamage(60);
        int SolarSlabsDamage => ScaleDamage(70);
        int CometDamage => ScaleDamage(40);
        int ConsecratedDamage => ScaleDamage(25);

        int ScaleDamage(int baseDamage) => SHM ? (int)(baseDamage * 1.3f * tsorcRevampWorld.SHMScale) : baseDamage;

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;

        /// <summary>DebugMode above-head readout (see IDebugAttackLabel).</summary>
        public string DebugAttackLabel => NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer > 0
            ? "Staggered"
            : State == AttackState.None ? "Idle" : DebugLabels.Humanize(State.ToString());

        int AttackTimer;
        int AttackCooldown = 150;
        int SlamPhase;          //sub-phase for SunlightSlam / LedgeLeap: 0 windup, 1 rising, 2 apex, 3 dive, 4 recovery
        float DiveDriftX;       //horizontal drift locked at the start of the dive
        int lockedSweepDir = 1; //facing locked when the lightning sweep telegraph starts
        bool TeleportUsed;      //the once-per-fight surprise blink (below 50% health, opening Wrath of Gold)
        bool SunfallActive;     //enrage below 20%: comets rain continuously
        int SunfallTimer;
        int LedgeCampTimer;     //counts up while the player perches out of reach above
        int FootstepTimer;
        bool SHM;               //SuperHardMode encounter: escalated stats + projectile damage
        bool statsInitialized;  //one-shot world-state stat application on the first AI tick
        bool HolyShieldActive;  //independent distance ability, never an attack state
        float HolyShieldVisualProgress;
        readonly GigasTerrainNavigation TerrainNavigation = new GigasTerrainNavigation();
        bool WrathApproachLaunched;
        float WrathApproachVelocityX;
        //Navigation-only long leap. SmartFighter4 handles normal routes and local jumps; this is the
        //giant's deliberate answer to a whole house / wall separating it from a valid landing near the player.
        int TerrainLeapTicks;
        int TerrainLeapCooldown;
        float TerrainLeapVelocityX;
        float TerrainLeapLandingX;

        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailCacheLength[NPC.type] = 8; //afterimages for the slam dive (Leonhard-style trail)
            NPCID.Sets.TrailingMode[NPC.type] = 0;
        }

        public override void SetDefaults()
		{
			NPC.width = 52;
			NPC.height = 110;
			//Late-Hardmode boss-event tier (SHM rare-event stats are applied on the first AI tick
			//from the world flag — see InitializeStats)
			NPC.damage = 110;
			NPC.defense = 38;
			NPC.lifeMax = 32000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 200000f; //Dark Souls = value / 25 (GlobalNPC.OnKill) = 8000 — a bit under TheHunter's 8800
			NPC.npcSlots = 100;
            NPC.scale = 1f;
			DrawOffsetY = 4;
			NPC.knockBackResist = 0.1f;
			Main.npcFrameCount[NPC.type] = 16;
			AnimationType = 28;

			// Phase 2: SmartFighter4AI movement + beast levers. minSurfaceWidth (in AI) keeps it off 1-tile
			// ledges; NavSearchRadius enables A* (small — it's slow). MaxJumpPower modestly above the 8 default
			// for a strong jump (NPC.gravity is read-only, so a true "weighty" heavy fall isn't available — feel
			// comes from the slow walk + strong jump). Tuning values — adjust to taste.
			tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
			g.NavSearchRadius = 60; // larger window so A* can find valid flat ledges above/across to jump to
			g.MaxJumpPower = 9f;
			g.MaxJumpBoost = 5f;
			// A two-tile support core may straddle exactly one tile of descent. This keeps the
			// giant visually centered over a stair-step instead of treating it as a ledge.
			g.MaxSurfaceStep = 1;
			// On-hit evasion: lumber back to reset spacing, or telegraph a hyper-armored charge back in.
			EvasiveProfile.HeavyBeast(g);
			// Phase 1 (beast positioner): never stand still — when it can't path to you it falls back to a large
			// band and oscillates in/out (using its ranged throw); wanders off if it can't reach you AND you stop
			// hitting it for ~10s. Tune the band to taste.
			g.KiteRangeMin = 0f;
			g.KiteRangeMax = 30f;
			g.KiteLooseness = 0.8f;
			g.PatrolMode = NPCs.PatrolMode.Wander;
		}

		//On-hit evasion only from true neutral: attack recovery frames (e.g. after the slam) are a
		//deliberate punish window, so the HeavyBeast reaction must not bail it out of them.
		public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
		{
			if (State == AttackState.None)
			{
				tsorcRevampAIs.EvasiveOnHit(NPC, true);
			}
		}
		public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
		{
			if (State == AttackState.None)
			{
				tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
			}
		}
        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }

        ///<summary>Unique weapon: the Wrath of Gold tome — guaranteed on the first kill, 25% on repeat
        ///kills, never in SHM (those pay out in Dark Souls via NPC.value). OnKill registers NewSlain.</summary>
        public override void ModifyNPCLoot(Terraria.ModLoader.NPCLoot npcLoot)
        {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.ByCondition(
                new NonSHMFirstKillRule(), ModContent.ItemType<Items.Weapons.Magic.WrathOfGold>()));
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write((byte)SlamPhase);
            writer.Write(AttackCooldown);
            writer.Write(DiveDriftX);
            writer.Write(TeleportUsed);
            writer.Write(SunfallActive);
            writer.Write(SHM);
            writer.Write(HolyShieldActive);
            writer.Write(WrathApproachLaunched);
            writer.Write(WrathApproachVelocityX);
            TerrainNavigation.Send(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            SlamPhase = reader.ReadByte();
            AttackCooldown = reader.ReadInt32();
            DiveDriftX = reader.ReadSingle();
            TeleportUsed = reader.ReadBoolean();
            SunfallActive = reader.ReadBoolean();
            SHM = reader.ReadBoolean();
            HolyShieldActive = reader.ReadBoolean();
            WrathApproachLaunched = reader.ReadBoolean();
            WrathApproachVelocityX = reader.ReadSingle();
            TerrainNavigation.Receive(reader);
        }

        ///<summary>Poise break (only reachable in neutral or the Wrath of Gold staggerable window): the cast
        ///visibly disperses and the giant returns to neutral. Orbiting halo suns scatter on their own by
        ///reading StaggerTimer.</summary>
        public void OnStagger(NPC npc)
        {
            if (State != AttackState.None && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.8f, Pitch = -0.5f }, NPC.Center);
                for (int i = 0; i < 26; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldFlame, vel.X, vel.Y, 120, default, 1.4f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].fadeIn = 0.3f;
                }
            }
            State = AttackState.None;
            AttackTimer = 0;
            SlamPhase = 0;
            AttackCooldown = Math.Max(AttackCooldown, 120);
        }

		public override void AI()
		{
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player player = Main.player[NPC.target];
            InitializeStats();

            //Attack-phase flags are re-set every tick by whichever attack is running
            g.AttackTelegraphing = false;
            g.AttackCommitted = false;
            UpdateHolyShieldAbility(player);

            //Staggered (poise broken): the global system roots it; sell it with a slumped lean + falling motes
            if (g.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.18f, 0.08f);
                if (Main.rand.NextBool(3))
                {
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldCoin, 0f, 1f, 0, default, 0.9f);
                    Main.dust[dust].velocity *= 0.3f;
                }
                UpdateAura();
                return;
            }
            NPC.rotation *= 0.9f;

            if (State == AttackState.None)
            {
                //A committed terrain leap owns horizontal motion until it reaches the landing that was
                //validated at launch. Do not let the regular fighter mover sand its velocity away mid-arc.
                if (TerrainNavigation.Update(NPC))
                {
                    FootstepEffects();
                }
                else
                {
                //Slow, weighty walk — one speed, always. It never runs; its reach is its magic.
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.5f, acceleration: 0.5f, canTeleport: false, minSurfaceWidth: 2, canWalkBackwards: true); // Two-tile flat support core; wide sprite edges use the bounded ground sink.
                bool recoveringFromNarrowPerch = TerrainNavigation.RecoverFromNarrowPerch(NPC, g, player);
                if (NPC.lavaWet)
                {
                    NPC.velocity.Y -= 2;
                }
                FootstepEffects();
                LedgeCampCheck(player);

                //Only launch across terrain after the ordinary SmartFighter mover has had a chance to
                //solve a local step. A rejected leap is harmless: it has a fully clear arc and a flat,
                //body-width landing as hard requirements.
                bool terrainLeapStarted = !recoveringFromNarrowPerch && TerrainNavigation.TryBegin(NPC, player);
                if (!terrainLeapStarted && AttackCooldown > 0)
                {
                    AttackCooldown--;
                }
                if (!terrainLeapStarted && !recoveringFromNarrowPerch && Main.netMode != NetmodeID.MultiplayerClient && AttackCooldown <= 0 && NPC.velocity.Y == 0f
                    && !player.dead && player.active && NPC.Distance(player.Center) < 1100f)
                {
                    PickAttack(player);
                }
                }
            }
            else
            {
                RunAttack(g, player);
            }

            UpdateAura();
            SunfallTick(player);
		}

        ///<summary>One-shot on the first AI tick: SuperHardMode rare-event encounters get escalated
        ///stats — double HP, much harder hits (the world flag isn't reliable in SetDefaults).</summary>
        void InitializeStats()
        {
            if (statsInitialized)
            {
                return;
            }
            statsInitialized = true;
            if (tsorcRevampWorld.SuperHardMode)
            {
                SHM = true;
                NPC.lifeMax = 64000; //double the late-HM baseline
                NPC.life = NPC.lifeMax;
                NPC.damage = 200;
                NPC.defense = 55;
                NPC.netUpdate = true;
            }
        }

        #region Attack selection

        void PickAttack(Player player)
        {
            //The once-per-fight surprise: the first cast after crossing half health is a Wrath of Gold
            //opened by blinking behind the player.
            if (!TeleportUsed && NPC.life < NPC.lifeMax / 2)
            {
                TeleportUsed = true;
                StartAttack(AttackState.WrathApproach);
                return;
            }

            float distTiles = NPC.Distance(player.Center) / 16f;
            bool sameLevel = Math.Abs(player.Bottom.Y - NPC.Bottom.Y) < 4 * 16f;

            //Weighted pool by range; repeating the previous attack is halved
            Span<(AttackState state, float weight)> pool = stackalloc (AttackState, float)[]
            {
                (AttackState.SunlightSlam,   distTiles < 40f ? 1f : 0.3f),
                (AttackState.WrathOfGold,    distTiles < 16f ? 1.2f : 0.2f),
                (AttackState.LightningSweep, distTiles < 40f && sameLevel ? 0.9f : 0f),
                (AttackState.SolarSlabs,     distTiles < 45f ? 0.8f : 0f),
                (AttackState.HeavenlySpears, 0.8f),
                (AttackState.SunPillars,     0.8f),
                (AttackState.HaloVolley,     0.7f),
                (AttackState.SolarBoulder,   distTiles > 15f ? 0.9f : 0.2f),
            };
            float total = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].state == LastAttack)
                {
                    pool[i].weight *= 0.5f;
                }
                total += pool[i].weight;
            }
            float roll = Main.rand.NextFloat(total);
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= pool[i].weight;
                if (roll <= 0f)
                {
                    StartAttack(pool[i].state == AttackState.WrathOfGold ? AttackState.WrathApproach : pool[i].state);
                    return;
                }
            }
            StartAttack(AttackState.SunlightSlam);
        }

        void StartAttack(AttackState state)
        {
            State = state;
            AttackTimer = 0;
            SlamPhase = 0;
            WrathApproachLaunched = false;
            WrathApproachVelocityX = 0f;
            NPC.netUpdate = true;
        }

        void EndAttack(int cooldown, bool immediateFollowup = false)
        {
            LastAttack = State;
            State = AttackState.None;
            AttackTimer = 0;
            SlamPhase = 0;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = immediateFollowup ? 0 : cooldown + Main.rand.Next(60);
                NPC.netUpdate = true;
            }
        }

        ///<summary>Golden blink to ~25 tiles behind the player (opposite their facing), ground-snapped.
        ///Skipped silently if no stand-able ground is found there.</summary>
        void TeleportBehind(Player player)
        {
            Vector2 target = player.Center + new Vector2(-player.direction * 25 * 16f, 0f);
            float groundY = FindGroundY(target, 25);
            if (groundY < 0f)
            {
                return;
            }
            TeleportFlash();
            NPC.Bottom = new Vector2(target.X, groundY);
            NPC.velocity = Vector2.Zero;
            TeleportFlash();
            NPC.netUpdate = true;
        }

        void TeleportFlash()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            for (int m = 0; m < 25; m++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldFlame, 0, 0, 100, default, 2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Main.rand.NextVector2Circular(7f, 7f);
            }
        }

        #endregion

        #region Attack execution

        void RunAttack(tsorcRevampGlobalNPC g, Player player)
        {
            if (player.dead || !player.active)
            {
                EndAttack(60);
                return;
            }

            //Casts face the player; the sweep locks its facing once its telegraph has started (tick 1)
            if (State == AttackState.LightningSweep && AttackTimer > 0)
            {
                NPC.direction = lockedSweepDir;
            }
            else if (SlamPhase < 3) //the dive keeps its committed facing
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            }
            NPC.spriteDirection = NPC.direction;

            AttackTimer++;
            switch (State)
            {
                case AttackState.SunlightSlam: RunSlam(g, player, LeapVariant: false); break;
                case AttackState.LedgeLeap: RunSlam(g, player, LeapVariant: true); break;
                case AttackState.WrathApproach: RunWrathApproach(g, player); break;
                case AttackState.WrathOfGold: RunWrathOfGold(g); break;
                case AttackState.HeavenlySpears: RunHeavenlySpears(g, player); break;
                case AttackState.SunPillars: RunSunPillars(g, player); break;
                case AttackState.HaloVolley: RunHaloVolley(g); break;
                case AttackState.LightningSweep: RunLightningSweep(g); break;
                case AttackState.SolarBoulder: RunSolarBoulder(g, player); break;
                case AttackState.SolarSlabs: RunSolarSlabs(g, player); break;
            }
        }

        ///<summary>SunlightSlam and its anti-camping cousin LedgeLeap share the jump→dive→slam machine;
        ///the leap variant launches much higher/farther and aims the whole arc at the perched player.</summary>
        void RunSlam(tsorcRevampGlobalNPC g, Player player, bool LeapVariant)
        {
            int windup = LeapVariant ? LeapWindupTicks : SlamWindupTicks;
            switch (SlamPhase)
            {
                case 0: //crouch: dust gathers at its feet, hyper-armored
                    g.AttackCommitted = true;
                    NPC.velocity.X *= 0.8f;
                    if (AttackTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 0.8f, Pitch = -0.3f }, NPC.Bottom);
                        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.6f }, NPC.Center);
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 pos = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - Main.rand.NextFloat(20f));
                        int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -2f, 100, default, 1.4f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (AttackTimer >= windup)
                    {
                        float dx = player.Center.X - NPC.Center.X;
                        NPC.velocity.Y = LeapVariant ? -16f : -14f;
                        NPC.velocity.X = MathHelper.Clamp(dx / (LeapVariant ? 55f : 50f), LeapVariant ? -10f : -7f, LeapVariant ? 10f : 7f);
                        SlamPhase = 1;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                case 1: //rising
                    g.AttackCommitted = true;
                    if (NPC.velocity.Y >= 0f)
                    {
                        SlamPhase = 2;
                        AttackTimer = 0;
                    }
                    break;

                case 2: //apex hang: "gathering" beat before the drop
                    g.AttackCommitted = true;
                    NPC.velocity.X = 0f;
                    NPC.velocity.Y = -0.3f; //roughly cancels the gravity added after AI, so it hangs
                    for (int i = 0; i < 2; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 pos = NPC.Center + angle.ToRotationVector2() * Main.rand.NextFloat(40f, 70f);
                        int dust = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, 0f, 0, default, 1f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity = (NPC.Center - pos) * 0.1f;
                    }
                    if (AttackTimer >= SlamApexTicks)
                    {
                        DiveDriftX = MathHelper.Clamp((player.Center.X - NPC.Center.X) / 40f, -4f, 4f);
                        SlamPhase = 3;
                        AttackTimer = 0;
                        SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.7f, Pitch = -0.4f }, NPC.Center);
                        NPC.netUpdate = true;
                    }
                    break;

                case 3: //dive: afterimage trail (PreDraw) + heavy dust, lands as the slam
                    g.AttackCommitted = true;
                    NPC.velocity.Y = 24f; //re-set every tick to beat the terminal-velocity clamp
                    NPC.velocity.X = DiveDriftX;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 pos = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.position.Y + Main.rand.NextFloat(NPC.height));
                        int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -6f, 80, default, 1.6f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (NPC.collideY || (AttackTimer > 2 && NPC.velocity.Y == 0f))
                    {
                        LandSlam(LeapVariant ? 9f : 12f);
                        SlamPhase = 4;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    else if (AttackTimer > 300) //fell into a pit — bail out
                    {
                        EndAttack(120);
                    }
                    break;

                case 4: //recovery: deliberately NO hyper-armor — the punish window after the spectacle
                    NPC.velocity.X *= 0.7f;
                    if (AttackTimer >= SlamRecoveryTicks)
                    {
                        EndAttack(LeapVariant ? 240 : 300);
                    }
                    break;
            }
        }

        ///<summary>Landing impact shared by the slam and the ledge leap: screen shake, a dust-geyser ring
        ///out to the blast radius (the radius IS the telegraph), then the armed shockwaves follow it.</summary>
        void LandSlam(float shakeStrength)
        {
            UsefulFunctions.ScreenShake(NPC.Center, shakeStrength, 25);
            SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 1f, Pitch = -0.2f }, NPC.Bottom);

            //Geyser ring erupting outward along the ground on both sides
            for (int dir = -1; dir <= 1; dir += 2)
            {
                for (int tile = 1; tile <= 14; tile++)
                {
                    Vector2 basePos = new Vector2(NPC.Center.X + dir * tile * 16f, NPC.Bottom.Y);
                    float groundY = FindGroundY(basePos - new Vector2(0f, 48f), 7);
                    if (groundY < 0f)
                    {
                        continue;
                    }
                    float strength = 1f - tile / 16f;
                    // Taller 152px flames need a little more material, not larger square motes:
                    // 4 smaller plumes per tile replace the previous 3 oversized ones.
                    for (int i = 0; i < 4; i++)
                    {
                        int dust = Dust.NewDust(new Vector2(basePos.X - 6f, groundY - 6f), 12, 4, DustID.GoldFlame, 0f, 0f, 80, default, 0.72f + strength * 0.58f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity = new Vector2(dir * 0.5f, Main.rand.NextFloat(-7f, -3f) * (0.4f + strength));
                    }
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int dir = -1; dir <= 1; dir += 2)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom + new Vector2(dir * 24f, -21f), Vector2.Zero,
                        ModContent.ProjectileType<GigasShockwave>(), ShockwaveDamage, 4f, Main.myPlayer, dir, 12f);
                }
                // The lingering field follows the slam's impact outward: 0, left, right, then
                // progressively farther terrain tiles. Every module anchors to its own floor.
                Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X, NPC.Bottom.Y), Vector2.Zero,
                    ModContent.ProjectileType<GigasSolarBoulderGroundSpread>(), ConsecratedDamage, 0f, Main.myPlayer,
                    GigasConsecratedGround.SlamVariant, GigasConsecratedGround.SlamSpanTiles, 7f);
            }
        }

        // Wrath is an area-denial threat, not a distant spell turret. It gets one unmistakable
        // heavy leap into the player's space first; the existing 100-tick telegraph only begins
        // after that leap lands, so its timing and damage window remain unchanged.
        void RunWrathApproach(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            if (!WrathApproachLaunched)
            {
                NPC.velocity.X *= 0.70f;
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 0.75f, Pitch = -0.22f }, NPC.Bottom);
                }
                if (AttackTimer >= WrathApproachWindupTicks)
                {
                    float dx = player.Center.X - NPC.Center.X;
                    WrathApproachVelocityX = MathHelper.Clamp(dx / 52f, -11f, 11f);
                    NPC.velocity.Y = -15f;
                    NPC.velocity.X = WrathApproachVelocityX;
                    NPC.direction = Math.Sign(WrathApproachVelocityX);
                    NPC.spriteDirection = NPC.direction;
                    WrathApproachLaunched = true;
                    NPC.netUpdate = true;
                }
                return;
            }

            // Lock the horizontal impulse during the rise and fall; without this the normal fighter
            // steering can sand off the leap and leave Wrath safely out of range.
            NPC.velocity.X = WrathApproachVelocityX;
            if ((NPC.collideY && NPC.velocity.Y >= 0f) || (AttackTimer > WrathApproachWindupTicks + 6 && NPC.velocity.Y == 0f))
            {
                StartAttack(AttackState.WrathOfGold);
            }
            else if (AttackTimer > WrathApproachWindupTicks + 150)
            {
                //A cramped ceiling or pit cannot hold the encounter hostage; cancel cleanly and let
                //navigation pick a different position rather than firing the nova from long range.
                EndAttack(90);
            }
        }

        void RunWrathOfGold(tsorcRevampGlobalNPC g)
        {
            if (AttackTimer <= WrathTelegraphTicks)
            {
                NPC.velocity.X *= 0.75f;
                //THE long telegraph: staggerable for the first two thirds (poise builds normally and a
                //break cancels the nova via OnStagger), hyper-armored for the final stretch.
                if (AttackTimer < WrathStaggerableTicks)
                {
                    g.AttackTelegraphing = true;
                }
                else
                {
                    g.AttackCommitted = true;
                }

                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = -0.6f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<GigasNovaTelegraph>(), 0, 0f, Main.myPlayer, 300f, NPC.whoAmI);
                    }
                }
                if (AttackTimer == WrathStaggerableTicks)
                {
                    //Commit cue: the flash means "too late to interrupt"
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.9f, Pitch = 0.2f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Gold));
                    }
                }

                //Inward-spiralling gold converging on its chest — classic "get away NOW" language
                float progress = AttackTimer / (float)WrathTelegraphTicks;
                int count = 2 + (int)(progress * 3f);
                for (int i = 0; i < count; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = MathHelper.Lerp(160f, 20f, progress) + Main.rand.NextFloat(20f);
                    Vector2 pos = NPC.Center + angle.ToRotationVector2() * radius;
                    int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 100, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (NPC.Center - pos) * 0.06f;
                }

                if (AttackTimer == WrathTelegraphTicks)
                {
                    UsefulFunctions.ScreenShake(NPC.Center, 10f, 20);
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f, Pitch = -0.2f }, NPC.Center);
                    SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.5f }, NPC.Center);
                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(8f, 8f);
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldFlame, vel.X, vel.Y, 60, default, 2f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<GigasNovaRing>(), NovaDamage, 8f, Main.myPlayer, 300f);
                    }
                }
            }
            else
            {
                //Post-burst recovery: vulnerable
                NPC.velocity.X = 0f;
                if (AttackTimer >= WrathTelegraphTicks + WrathRecoveryTicks)
                {
                    EndAttack(360);
                }
            }
        }

        void RunHeavenlySpears(tsorcRevampGlobalNPC g, Player player)
        {
            //Walking cast: the giant keeps lumbering while the threat forms at the player's position
            //(no dodgeroll/pounce — the cast should read as stately, not acrobatic)
            tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.5f, acceleration: 0.5f, canTeleport: false, canDodgeroll: false, canPounce: false, minSurfaceWidth: 2, canWalkBackwards: true);
            TerrainNavigation.RecoverFromNarrowPerch(NPC, g, player);
            FootstepEffects();
            g.AttackCommitted = true;

            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
            }
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldCoin, 0f, -1.5f, 0, default, 1f);
                Main.dust[dust].noGravity = true;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                switch (AttackTimer)
                {
                    case 12: SpawnHeavenlySpearWave(player, 3, 140f, 300f, 45, 15, 0); break;
                    case 42: SpawnHeavenlySpearWave(player, 5, 240f, 335f, 42, 10, 1); break;
                    case 72: SpawnHeavenlySpearWave(player, 7, 340f, 370f, 38, 8, 2); break;
                    case 102: SpawnHeavenlySpearWave(player, 9, 450f, 405f, 34, 6, 3); break;
                }
            }
            if (AttackTimer >= SpearsCastTicks)
            {
                EndAttack(0, immediateFollowup: true);
            }
        }

        void RunSunPillars(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            NPC.velocity.X *= 0.8f;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.4f }, NPC.Center);
            }
            // Each pillar owns its delayed telegraph and 90-tick active window. Scheduling the whole
            // sequence here leaves Gigas free to choose a new attack while the judgment continues.
            if (AttackTimer == 8 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int pillarCount = NPC.life < NPC.lifeMax / 2
                    ? Main.rand.Next(2, 5) * 2 // phase two: 4, 6, or 8
                    : Main.rand.Next(1, 4) * 2; // phase one: 2, 4, or 6
                for (int i = 0; i < pillarCount; i++)
                {
                    int side = i % 2 == 0 ? 1 : -1;
                    float lane = i == 0 ? 0f : side * ((i + 1) / 2) * 72f;
                    float x = player.Center.X + lane + player.velocity.X * i * 10f;
                    float groundY = FindGroundY(new Vector2(x, player.Bottom.Y - 8f), 25);
                    if (groundY > 0f)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(x, groundY - GigasSunPillar.PillarHeight / 2f), Vector2.Zero,
                            ModContent.ProjectileType<GigasSunPillar>(), PillarDamage, 2f, Main.myPlayer, 45f, i * 24f);
                    }
                }
            }
            if (AttackTimer >= PillarsCastTicks)
            {
                EndAttack(0, immediateFollowup: true);
            }
        }

        void RunHaloVolley(tsorcRevampGlobalNPC g)
        {
            g.AttackCommitted = true;
            NPC.velocity.X *= 0.8f;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = 0.1f }, NPC.Center);
            }
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(NPC.position - new Vector2(0f, 40f), NPC.width, 40, DustID.GoldCoin, 0f, -1f, 0, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
            if (AttackTimer == 15 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 6; i++)
                {
                    //ai[1] = detach tick + slot/10 (fraction encodes the orbit angle slot)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0f, -70f), Vector2.Zero,
                        ModContent.ProjectileType<GigasHaloSun>(), HaloSunDamage, 2f, Main.myPlayer, NPC.whoAmI, (90 + i * 18) + i / 10f);
                }
            }
            if (AttackTimer >= HaloCastTicks)
            {
                EndAttack(420);
            }
        }

        void RunLightningSweep(tsorcRevampGlobalNPC g)
        {
            g.AttackCommitted = true;
            NPC.velocity.X *= 0.8f;
            if (AttackTimer == 1)
            {
                lockedSweepDir = NPC.direction;
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 beamCenter = new Vector2(NPC.Center.X + lockedSweepDir * (24f + GigasSweepBeam.BeamLength / 2f), NPC.Bottom.Y - GigasSweepBeam.BeamHeight / 2f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), beamCenter, Vector2.Zero,
                        ModContent.ProjectileType<GigasSweepBeam>(), SweepDamage, 6f, Main.myPlayer, SweepTelegraphTicks, lockedSweepDir);
                }
            }
            //Crackle building at the torso while the line telegraphs
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = NPC.Center + new Vector2(lockedSweepDir * Main.rand.NextFloat(10f, 30f), Main.rand.NextFloat(-20f, 20f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, lockedSweepDir * 2f, 0f, 80, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
            if (AttackTimer >= SweepCastTicks)
            {
                EndAttack(300);
            }
        }

        void RunSolarBoulder(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            NPC.velocity.X *= 0.8f;
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.5f }, NPC.Center);
            }
            //The sun forms above its head: converging gather for the first 30 ticks
            if (AttackTimer <= 30)
            {
                Vector2 focus = NPC.Center + new Vector2(0f, -100f);
                for (int i = 0; i < 2; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = focus + angle.ToRotationVector2() * Main.rand.NextFloat(20f, 60f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 80, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (focus - pos) * 0.1f;
                }
                Lighting.AddLight(focus, 0.6f * AttackTimer / 30f, 0.5f * AttackTimer / 30f, 0.2f * AttackTimer / 30f);
            }
            if (AttackTimer == 30)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.8f, Pitch = -0.6f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 origin = NPC.Center + new Vector2(0f, -90f);
                    Vector2 vel = UsefulFunctions.BallisticTrajectory(origin, player.Center, 12f, 0.25f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                        ModContent.ProjectileType<GigasSolarBoulder>(), BoulderDamage, 6f, Main.myPlayer, 0.25f);
                }
            }
            if (AttackTimer >= BoulderCastTicks)
            {
                EndAttack(270);
            }
        }

        void RunSolarSlabs(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = true;
            NPC.velocity.X *= 0.8f;
            if (AttackTimer == 8 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center, Vector2.Zero,
                    ModContent.ProjectileType<GigasLightHand>(), SolarSlabsDamage, 6f, Main.myPlayer, 50f);
            }
            if (AttackTimer >= SolarSlabsCastTicks)
            {
                EndAttack(330);
            }
        }

        ///<summary>Hydra-style distance ward, independent of the attack state. Hydra begins at 500px;
        ///Gigas begins at 600px and drops only after the player returns inside 540px, preventing flicker
        ///at the threshold while any normal attack is free to run.</summary>
        void UpdateHolyShieldAbility(Player player)
        {
            if (player?.active != true || player.dead)
            {
                HolyShieldActive = false;
                HolyShieldVisualProgress = Math.Max(0f, HolyShieldVisualProgress - 0.12f);
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float distance = NPC.Distance(player.Center);
                bool shouldActivate = HolyShieldActive ? distance > 540f : distance > 600f;
                if (HolyShieldActive != shouldActivate)
                {
                    HolyShieldActive = shouldActivate;
                    NPC.netUpdate = true;
                    if (shouldActivate && Main.netMode != NetmodeID.Server)
                    {
                        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.72f, Pitch = 0.18f }, NPC.Center);
                    }
                }
            }

            HolyShieldVisualProgress = HolyShieldActive
                ? Math.Min(1f, HolyShieldVisualProgress + 1f / 45f)
                : Math.Max(0f, HolyShieldVisualProgress - 0.08f);

            if (HolyShieldVisualProgress > 0f && Main.rand.NextBool(2))
            {
                Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                Dust mote = Dust.NewDustPerfect(pos, DustID.GoldCoin, new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.6f)),
                    60, default, Main.rand.NextFloat(0.55f, 0.9f));
                mote.noGravity = true;
            }
            if (HolyShieldVisualProgress > 0f)
            {
                Lighting.AddLight(NPC.Center, 0.8f * HolyShieldVisualProgress, 0.66f * HolyShieldVisualProgress,
                    0.2f * HolyShieldVisualProgress);
            }
            if (HolyShieldActive && Main.netMode != NetmodeID.MultiplayerClient)
            {
                AbsorbShieldProjectiles(player);
            }
        }

        void AbsorbShieldProjectiles(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile incoming = Main.projectile[i];
                if (!incoming.active || !incoming.friendly || incoming.hostile || incoming.damage <= 0
                    || !incoming.Hitbox.Intersects(NPC.Hitbox))
                {
                    continue;
                }

                Vector2 impact = incoming.Center;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), impact, Vector2.Zero,
                    ModContent.ProjectileType<GigasHolyShieldVortex>(), 0, 0f, Main.myPlayer, -1f, 0f);

                if (TryFindShieldVortexPosition(player, impact, out Vector2 output))
                {
                    int damage = Math.Max(1, (int)(player.statLifeMax2 * 0.20f));
                    // Keep the real player projectile alive but frozen and hidden in the output
                    // vortex. At release it is converted exactly as Hydra's shield does, preserving
                    // the original projectile type/visual while making it hostile and wall-piercing.
                    float returnSpeed = Math.Max(incoming.velocity.Length(), 14f);
                    incoming.friendly = false;
                    incoming.hostile = false;
                    incoming.hide = true;
                    incoming.tileCollide = false;
                    incoming.velocity = Vector2.Zero;
                    incoming.damage = damage;
                    // The output begins 400px behind the player; retain ample post-telegraph life
                    // for even a slow reflected projectile to reach its target through terrain.
                    incoming.timeLeft = Math.Max(incoming.timeLeft, 180);
                    incoming.netUpdate = true;

                    Projectile outputVortex = Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), output, Vector2.Zero,
                        ModContent.ProjectileType<GigasHolyShieldVortex>(), 0, 0f, player.whoAmI, incoming.whoAmI,
                        GigasHolyShieldVortex.OutputMode);
                    outputVortex.localAI[1] = returnSpeed;
                    outputVortex.netUpdate = true;
                }
                else
                {
                    incoming.Kill();
                }
            }
        }

        bool TryFindShieldVortexPosition(Player player, Vector2 impact, out Vector2 position)
        {
            int facing = player.direction == 0 ? NPC.direction : player.direction;
            Vector2 behindPlayer = player.Center - new Vector2(facing * 400f, 0f);
            float heightT = MathHelper.Clamp((impact.Y - NPC.Top.Y) / NPC.height, 0f, 1f);
            float desiredY = behindPlayer.Y + MathHelper.Lerp(-90f, 90f, heightT);
            float bestScore = float.MaxValue;
            position = Vector2.Zero;

            // Eight staggered slots fill a 60px-spaced 200x100px envelope. A new portal is only
            // allowed into a vacant slot, so rapid-fire weapons never make unreadable stacked vortices.
            float[] verticalSlots = { -90f, -30f, 30f, 90f };
            float[] lateralSlots = { -30f, 30f };
            int vortexType = ModContent.ProjectileType<GigasHolyShieldVortex>();
            foreach (float yOffset in verticalSlots)
            {
                foreach (float xOffset in lateralSlots)
                {
                    Vector2 candidate = behindPlayer + new Vector2(xOffset, yOffset);
                    bool occupied = false;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile portal = Main.projectile[i];
                        if (portal.active && portal.type == vortexType && portal.owner == player.whoAmI
                            && portal.ai[1] == GigasHolyShieldVortex.OutputMode
                            && Vector2.DistanceSquared(portal.Center, candidate) < 54f * 54f)
                        {
                            occupied = true;
                            break;
                        }
                    }
                    if (!occupied)
                    {
                        float score = Math.Abs(candidate.Y - desiredY) + Math.Abs(xOffset) * 0.1f;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            position = candidate;
                        }
                    }
                }
            }
            return bestScore < float.MaxValue;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (HolyShieldActive && projectile.friendly && !projectile.hostile)
            {
                return false;
            }
            return base.CanBeHitByProjectile(projectile);
        }

        void SpawnHeavenlySpearWave(Player player, int count, float halfWidth, float apexHeight,
            int hoverTicks, int hoverStep, int lengthTier)
        {
            for (int i = 0; i < count; i++)
            {
                float fraction = count == 1 ? 0.5f : i / (float)(count - 1);
                float x = MathHelper.Lerp(-halfWidth, halfWidth, fraction);
                // A shallow arch keeps the long outer spears inside a readable shared canopy.
                float y = -apexHeight + Math.Abs(fraction - 0.5f) * 80f;
                Vector2 pos = player.Center + new Vector2(x, y);
                for (int tries = 0; tries < 14 && IsSolidAt(pos); tries++)
                {
                    pos.Y += 16f;
                }
                if (IsSolidAt(pos))
                {
                    continue;
                }
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, Vector2.Zero,
                    ModContent.ProjectileType<GigasHeavenlySpear>(), SpearDamage, 3f, Main.myPlayer,
                    hoverTicks + i * hoverStep, lengthTier);
            }
        }

        #endregion

        #region Presence: enrage, footsteps, anti-camping, aura

        ///<summary>Enrage below 20% health: the sky opens — comets of light rain near the player for the
        ///rest of the fight, layered under whatever else it's doing. Pressure, not bullet hell.</summary>
        void SunfallTick(Player player)
        {
            if (NPC.life > NPC.lifeMax / 5)
            {
                return;
            }
            if (!SunfallActive)
            {
                SunfallActive = true;
                SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
                for (int i = 0; i < 30; i++)
                {
                    Vector2 pos = NPC.position - new Vector2(0f, 40f) + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(40f));
                    int dust = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, -3f, 0, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
                NPC.netUpdate = true;
            }
            SunfallTimer++;
            if (SunfallTimer >= 25)
            {
                SunfallTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient && !player.dead && player.active)
                {
                    Vector2 spawn = player.Center + new Vector2(Main.rand.NextFloat(-320f, 320f), -560f - Main.rand.NextFloat(120f));
                    if (spawn.Y < 100f)
                    {
                        spawn.Y = 100f;
                    }
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, new Vector2(Main.rand.NextFloat(-1f, 1f), 8f),
                        ModContent.ProjectileType<GigasSunfallComet>(), CometDamage, 2f, Main.myPlayer);
                }
            }
        }

        ///<summary>Weight through effects, not physics: each footfall thuds, puffs dust and nudges the
        ///screen a little when the player is close.</summary>
        void FootstepEffects()
        {
            if (NPC.velocity.Y == 0f && Math.Abs(NPC.velocity.X) > 0.15f)
            {
                FootstepTimer++;
                if (FootstepTimer >= 26)
                {
                    FootstepTimer = 0;
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.5f, Pitch = -0.15f }, NPC.Bottom);
                    UsefulFunctions.ScreenShake(NPC.Bottom, 1.5f, 8, 6f, 350f);
                    for (int i = 0; i < 4; i++)
                    {
                        int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.Bottom.Y - 6f), NPC.width, 6, DustID.Smoke, NPC.velocity.X * 0.3f, -0.5f, 120, default, 0.9f);
                        Main.dust[dust].velocity *= 0.4f;
                    }
                }
            }
        }

        ///<summary>If the player perches on a ledge out of reach for ~4s, answer with the telegraphed
        ///super-jump (LedgeLeap) instead of pacing below forever.</summary>
        void LedgeCampCheck(Player player)
        {
            bool camping = !player.dead && player.active
                && player.Bottom.Y < NPC.position.Y - 6 * 16f
                && Math.Abs(player.Center.X - NPC.Center.X) < 480f
                && NPC.velocity.Y == 0f;
            if (camping)
            {
                LedgeCampTimer++;
            }
            else if (LedgeCampTimer > 0)
            {
                LedgeCampTimer -= 2;
            }
            if (LedgeCampTimer > 240 && Main.netMode != NetmodeID.MultiplayerClient && AttackCooldown <= 60)
            {
                LedgeCampTimer = 0;
                StartAttack(AttackState.LedgeLeap);
            }
        }

        ///<summary>The chest-light ramp: brighter as it nears death, with a sharp pulse in the opening
        ///frames of every telegraph — the universal "an attack is coming" read.</summary>
        void UpdateAura()
        {
            float intensity = 0.35f + 0.65f * (1f - NPC.life / (float)NPC.lifeMax);
            if (State != AttackState.None && AttackTimer < 20)
            {
                intensity += 0.6f;
            }
            Lighting.AddLight(NPC.Center, intensity, intensity * 0.85f, intensity * 0.35f);
            if (State != AttackState.None && Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldCoin, 0f, -1f, 0, default, 0.9f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
        }

        #endregion

        #region Helpers

        // A* intentionally keeps its calibrated jumps conservative (the shared table tops out at ten tiles).
        // Gigas is the exception: it may leap an entire building, but only after proving that its actual full
        // rectangle can follow the arc and end on a flat four-tile landing. This stays navigation-only: no
        // hitbox, attack state, or VFX is attached to it.
        bool UpdateTerrainLeap(Player player)
        {
            if (TerrainLeapCooldown > 0)
            {
                TerrainLeapCooldown--;
            }
            if (TerrainLeapTicks <= 0)
            {
                return false;
            }

            TerrainLeapTicks--;
            NPC.velocity.X = TerrainLeapVelocityX;
            int direction = Math.Sign(TerrainLeapVelocityX);
            NPC.direction = direction;
            NPC.spriteDirection = direction;

            //The collision frame comes back with zero vertical velocity after Terraria resolves the landing.
            if ((NPC.collideY && NPC.velocity.Y >= 0f) || (NPC.velocity.Y == 0f && TerrainLeapTicks < 100))
            {
                LogTerrainLeap("landed", $"x={NPC.Center.X / 16f:F1} target={TerrainLeapLandingX / 16f:F1}");
                TerrainLeapTicks = 0;
                TerrainLeapCooldown = 90;
                NPC.netUpdate = true;
                return false;
            }
            if (TerrainLeapTicks <= 0)
            {
                LogTerrainLeap("timeout", $"x={NPC.Center.X / 16f:F1} target={TerrainLeapLandingX / 16f:F1}");
                TerrainLeapCooldown = 120;
                NPC.netUpdate = true;
                return false;
            }
            return true;
        }

        bool TryStartTerrainLeap(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || TerrainLeapCooldown > 0 || NPC.velocity.Y != 0f
                || !player.active || player.dead)
            {
                return false;
            }

            float horizontalDistance = player.Center.X - NPC.Center.X;
            if (Math.Abs(horizontalDistance) < 240f || Math.Abs(horizontalDistance) > 1120f)
            {
                return false;
            }

            //Do not replace ordinary walking just because a wall happens to be in the way. This specifically
            //answers a collision at the body's leading edge, or a substantial solid structure between combatants.
            bool blocked = NPC.collideX || !Collision.CanHitLine(NPC.position, NPC.width, NPC.height, player.position, player.width, player.height);
            if (!blocked)
            {
                return false;
            }

            int direction = Math.Sign(horizontalDistance);
            if (!TryFindTerrainLeapLanding(player, direction, out Vector2 landingPosition, out float flightTime, out float launchVelocityY))
            {
                TerrainLeapCooldown = 45;
                LogTerrainLeap("rejected", "blocked but no clear full-body arc / flat landing");
                return false;
            }

            TerrainLeapVelocityX = (landingPosition.X - NPC.position.X) / flightTime;
            TerrainLeapLandingX = landingPosition.X + NPC.width * 0.5f;
            TerrainLeapTicks = (int)Math.Ceiling(flightTime + 24f);
            TerrainLeapCooldown = 0;
            NPC.velocity.Y = launchVelocityY;
            NPC.velocity.X = TerrainLeapVelocityX;
            NPC.direction = direction;
            NPC.spriteDirection = direction;
            NPC.netUpdate = true;
            LogTerrainLeap("launch", $"from=({NPC.Center.X / 16f:F1},{NPC.Bottom.Y / 16f:F1}) to=({TerrainLeapLandingX / 16f:F1},{(landingPosition.Y + NPC.height) / 16f:F1}) t={flightTime:F0} vel=({TerrainLeapVelocityX:F2},{launchVelocityY:F2})");
            return true;
        }

        bool TryFindTerrainLeapLanding(Player player, int direction, out Vector2 landingPosition, out float flightTime, out float launchVelocityY)
        {
            landingPosition = Vector2.Zero;
            flightTime = 0f;
            launchVelocityY = 0f;

            //Search near the player, beginning on the side closest to Gigas. It makes the move read as a chase
            //instead of an unfair overshoot, while the existing two-tile support rule keeps the giant off
            //the one-tile roof edges that make its large body look as though it is floating.
            int playerTileX = (int)(player.Center.X / 16f);
            const int footprintWidth = 2;
            float searchStartY = player.Bottom.Y - 20f * 16f;
            for (int offset = 0; offset <= 16; offset++)
            {
                int signedOffset = offset == 0 ? 0 : -direction * offset;
                int landingTileX = playerTileX + signedOffset;
                float groundY = FindGroundY(new Vector2(landingTileX * 16f + 8f, searchStartY), 48);
                if (groundY <= 0f || !UsefulFunctions.IsFootprintSupported(landingTileX, (int)(groundY / 16f), footprintWidth))
                {
                    continue;
                }

                Vector2 candidate = new Vector2(landingTileX * 16f + 8f - NPC.width * 0.5f, groundY - NPC.height);
                float dx = candidate.X - NPC.position.X;
                if (Math.Sign(dx) != direction || Math.Abs(dx) < 192f || Math.Abs(dx) > 1120f
                    || Collision.SolidCollision(candidate, NPC.width, NPC.height))
                {
                    continue;
                }

                //A high, slow arc clears the igloo-sized structures the generic jump table intentionally does
                //not cover. Its height is still physically simulated against the Gigas body before acceptance.
                float time = MathHelper.Clamp(Math.Abs(dx) / 10f, 82f, 110f);
                float gravity = NPC.gravity > 0f ? NPC.gravity : 0.3f;
                float velocityY = (candidate.Y - NPC.position.Y - gravity * time * (time - 1f) * 0.5f) / time;
                float velocityX = dx / time;
                if (velocityY < -18f || velocityY > -10f || Math.Abs(velocityX) > 12f)
                {
                    continue;
                }
                if (HasTerrainLeapClearance(candidate, velocityX, velocityY, gravity, (int)Math.Ceiling(time)))
                {
                    landingPosition = candidate;
                    flightTime = time;
                    launchVelocityY = velocityY;
                    return true;
                }
            }
            return false;
        }

        bool HasTerrainLeapClearance(Vector2 landingPosition, float velocityX, float velocityY, float gravity, int flightTicks)
        {
            Vector2 simulatedPosition = NPC.position;
            Vector2 simulatedVelocity = new Vector2(velocityX, velocityY);
            for (int tick = 0; tick < flightTicks; tick++)
            {
                simulatedPosition += simulatedVelocity;
                simulatedVelocity.Y += gravity;
                //The intended final rectangle rests exactly above its supporting tiles; test the approach through
                //the previous frame, then test the final landing rectangle separately to avoid rejecting its floor.
                if (tick < flightTicks - 1 && Collision.SolidCollision(simulatedPosition, NPC.width, NPC.height))
                {
                    return false;
                }
            }
            return Vector2.DistanceSquared(simulatedPosition, landingPosition) < 20f * 20f
                && !Collision.SolidCollision(landingPosition, NPC.width, NPC.height);
        }

        void LogTerrainLeap(string action, string detail)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().DebugMode || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            try
            {
                string directory = Main.SavePath + Path.DirectorySeparatorChar + "Logs";
                Directory.CreateDirectory(directory);
                string line = $"[{DateTime.Now:HH:mm:ss}] {NPC.TypeName}#{NPC.whoAmI} [gigas-leap] {action} {detail}";
                File.AppendAllText(directory + Path.DirectorySeparatorChar + "tsorcRevamp-smartfighter4.log", line + Environment.NewLine);
            }
            catch { }
        }

        // Navigation rejects narrow landing surfaces, but collision can still leave Gigas physically
        // balanced on one after a jump. Do not make it ghost through tiles: its existing bounded
        // ground-sink owns normal slopes. Instead hop toward the nearest genuine three-tile flat
        // landing, so a one-tile perch is always temporary and never reads as floating.
        bool RecoverFromNarrowPerch(tsorcRevampGlobalNPC g, Player player)
        {
            if (NPC.velocity.Y != 0f)
            {
                return false;
            }
            int supportX = (int)(NPC.Center.X / 16f);
            int supportY = (int)((NPC.Bottom.Y + 4f) / 16f);
            if (UsefulFunctions.IsFootprintSupported(supportX, supportY, 2))
            {
                return false;
            }

            int playerDirection = player.active && !player.dead ? Math.Sign(player.Center.X - NPC.Center.X) : NPC.direction;
            if (playerDirection == 0)
            {
                playerDirection = NPC.direction == 0 ? 1 : NPC.direction;
            }

            int direction = FindNearestFlatLandingDirection(supportX, supportY, playerDirection);
            NPC.velocity.Y = -g.MaxJumpPower * 0.85f;
            NPC.velocity.X = direction * g.MaxJumpBoost * 0.75f;
            NPC.direction = direction;
            NPC.spriteDirection = direction;
            NPC.netUpdate = true;
            return true;
        }

        int FindNearestFlatLandingDirection(int supportX, int supportY, int preferredDirection)
        {
            // Search the preferred side first, then the other side. A landing may be up to three
            // tiles lower, but every accepted candidate is still a fully flat two-tile footprint.
            for (int pass = 0; pass < 2; pass++)
            {
                int direction = pass == 0 ? preferredDirection : -preferredDirection;
                for (int distance = 3; distance <= 10; distance++)
                {
                    for (int stepDown = 0; stepDown <= 3; stepDown++)
                    {
                        if (UsefulFunctions.IsFootprintSupported(supportX + direction * distance, supportY + stepDown, 2))
                        {
                            return direction;
                        }
                    }
                }
            }
            return preferredDirection;
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
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }
            return -1f;
        }

        static bool IsSolidAt(Vector2 worldPos)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5 || ty < 5 || ty > Main.maxTilesY - 5)
            {
                return true;
            }
            Tile tile = Main.tile[tx, ty];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }

        #endregion

        ///<summary>Golden afterimage trail during the slam dive (and any other high-speed moment, e.g. the
        ///HeavyBeast charge) — same oldPos technique as Leonhard's dash.</summary>
        public override Color? GetAlpha(Color drawColor)
        {
            return HolyShieldVisualProgress > 0f
                ? Color.Lerp(drawColor, new Color(255, 218, 112), HolyShieldVisualProgress * 0.56f)
                : null;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool diving = (State == AttackState.SunlightSlam || State == AttackState.LedgeLeap) && SlamPhase == 3;
            if (diving || NPC.velocity.Length() > 8f)
            {
                Texture2D texture = TextureAssets.Npc[NPC.type].Value;
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height); //bottom-center
                for (int k = NPC.oldPos.Length - 1; k > 0; k -= 2)
                {
                    Vector2 drawPos = NPC.oldPos[k] + new Vector2(NPC.width / 2f, NPC.height + NPC.gfxOffY + 6f) - screenPos;
                    Color color = new Color(255, 210, 90, 0) * ((NPC.oldPos.Length - k) / (float)NPC.oldPos.Length) * 0.6f;
                    spriteBatch.Draw(texture, drawPos, NPC.frame, color, NPC.rotation, origin, NPC.scale, effects, 0f);
                }
            }

            if (HolyShieldVisualProgress > 0f)
            {
                // Hydra's Magic Shield language, rebuilt around Gigas's fixed sprite as a warm radial
                // halo and echoes. The standard NPC draw remains untouched, preserving its ground anchor.
                Texture2D texture = TextureAssets.Npc[NPC.type].Value;
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 origin = NPC.frame.Size() * 0.5f;
                Vector2 drawPos = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY + 6f);
                float intensity = HolyShieldVisualProgress;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 haloOffset = new Vector2(18f * intensity, 0f).RotatedBy(MathHelper.TwoPi * i / 12f);
                    spriteBatch.Draw(texture, drawPos + haloOffset, NPC.frame, new Color(255, 187, 44, 0) * (0.12f + intensity * 0.18f),
                        NPC.rotation, origin, NPC.scale * (1f + intensity * 0.035f), effects, 0f);
                }
                for (int k = NPC.oldPos.Length - 1; k >= 1; k -= 2)
                {
                    if (NPC.oldPos[k] == Vector2.Zero)
                    {
                        continue;
                    }
                    Vector2 echo = NPC.oldPos[k] + new Vector2(NPC.width * 0.5f, NPC.height + NPC.gfxOffY + 6f) - screenPos;
                    float alpha = (NPC.oldPos.Length - k) / (float)NPC.oldPos.Length * 0.25f * intensity;
                    spriteBatch.Draw(texture, echo, NPC.frame, new Color(255, 179, 36, 0) * alpha, NPC.rotation, origin, NPC.scale, effects, 0f);
                }
            }
            return true;
        }

        ///<summary>Death spectacle: light pours out of it before the gore flies.</summary>
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.netMode == NetmodeID.Server)
            {
                return;
            }
            UsefulFunctions.ScreenShake(NPC.Center, 10f, 20);
            SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.5f, Pitch = 0.2f }, NPC.Center);
            for (int i = 0; i < 40; i++)
            {
                Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 60, default, Main.rand.NextFloat(1.5f, 2.4f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-9f, -4f));
            }
            for (int i = 0; i < 12; i++)
            {
                int sparkle = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldCoin, 0f, -3f, 0, default, 1.2f);
                Main.dust[sparkle].noGravity = true;
            }
        }

		public override void OnKill()
		{
            //Register the first kill (server-side) so the guaranteed-tome rule flips to the 25% repeat rate
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Terraria.ModLoader.Config.NPCDefinition definition = new(NPC.type);
                if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
                {
                    tsorcRevampWorld.NewSlain.Add(definition, 1);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.WorldData);
                    }
                }
            }
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GigasGore1").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GigasGore2").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GigasGore3").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GigasGore2").Type, 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("GigasGore3").Type, 1.1f);
        }
    }
}
