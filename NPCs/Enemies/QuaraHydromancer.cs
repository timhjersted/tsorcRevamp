using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Projectiles.Enemy.Quara;

namespace tsorcRevamp.NPCs.Enemies
{
    // Rebuilt with the Gigas Method (Documentation/EnemyRedesignSkillGuide.md): a kiting water-mage
    // displaced into the Hallow, with an actual gameplan — SOAK the target (bubbles / wave / droplets
    // all apply Wet), because ink sticks to a wet target: the Ink Geyser's Blackout and Broken Armor
    // last TWICE as long against a soaked player. Respect the "harmless" bubbles or eat a long blind.
    //
    // Kit: Bubble Barrage (Wet), Tidal Crest (jumpable ground wave, Wet + Chilled), Burst Bubble
    // (its long staggerable cast → drifting bubble that pops into droplets), Ink Geyser (now lands at
    // the PLAYER's position; gated on the Water Fiend Kraken kill, always available in SHM), and
    // Tide Rush — its fish-man repositioning: it collapses into a rushing puddle-surge and re-forms
    // at kiting distance, either retreating or flanking to the player's far side.
    //
    // Poise contract (0.35/30 in PoiseProfiles): short casts hyper-armored; Burst Bubble's long cast
    // is staggerable for its first two thirds. The old justHit hop spaghetti is replaced by the
    // shared evasion system (retreat-jump / quick-step), gated to neutral.
    class QuaraHydromancer : ModNPC, IStaggerable, IDebugAttackLabel
    {
        enum AttackState : byte
        {
            None = 0,
            BubbleBarrage, // three soaking bubbles, each bobbing on its own phase
            TidalCrest,    // the breaking wave — jump it or roll through
            BurstBubble,   // long staggerable cast → big drifting bubble, pops into droplets
            InkGeyser,     // the payoff: ink cloud at the player's position (Wet doubles its stick)
            TideRush,      // repositioning surge: retreat, or flank to the player's far side
            IceArcVolley,  // half-ring gather -> six upward arcing, briefly guided frost shards
            IceOverflight, // spiral gather -> shards route over and behind before their pursuit line
            FrostSprites,  // delayed, terrain-safe frost sprites seed light-homing shard clumps
        }

        //Timings (ticks)
        const int BarrageTelegraphTicks = 25;
        const int CrestTelegraphTicks = 30;
        const int BurstCastTicks = 60;
        const int BurstStaggerableTicks = 40; //first two thirds: a poise break pops the cast
        const int InkTelegraphTicks = 35;
        const int RushDissolveTicks = 15;
        const int RushMaxSurgeTicks = 60;
        const int RushReformTicks = 10;
        const int IceTelegraphTicks = 40;
        const int IceRecoveryTicks = 20;
        const float IceGuidedThirty = 1f;
        const float IceOverflightThirty = 2f;
        const float IceGuidedSixty = 3f;
        const float IceWaterIgniter = 4f;

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;
        int rushDir = 1;          //surge direction, locked at dissolve end
        float rushDestX;          //where the surge re-forms
        Vector2 rushStartGround;  //server-authoritative target for Tide Rush's returning ice volley
        int lastRushWaterTileX = int.MinValue;
        bool rushIceVolleyFired;
        bool statsInitialized;
        bool HM;
        bool SHM;
        float savedKnockBackResist = -1f; //restored after the surge's brief knockback immunity

        //Damage tiers preserved from the old SetDefaults blocks (now applied on the first AI tick —
        //world flags aren't reliable in SetDefaults). Hostile projectiles deal 2x these on hit.
        int BubbleDamage => SHM ? 55 : HM ? 45 : 33;
        int CrestDamage => SHM ? 50 : HM ? 40 : 30;
        int BurstDamage => SHM ? 55 : HM ? 45 : 35;
        int InkDamage => SHM ? 55 : HM ? 45 : 33;
        int IceDamage => SHM ? 38 : HM ? 30 : 22;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
        }

        public override void SetDefaults()
        {
            AnimationType = 21;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 22;
            NPC.height = 45;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 1500;
            NPC.width = 18;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.25f; //overridden by the PoiseProfiles entry
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.QuaraHydromancerBanner>();

            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 60; // Phase 2: SmartFighter4AI movement
            //A caster keeps its distance and backpedals while it works
            g.KiteRangeMin = 8f;
            g.KiteRangeMax = 16f;
            g.KiteLooseness = 0.25f;
            //Slippery: the shared on-hit evasion replaces the old justHit hop spaghetti
            g.EvasiveRetreatJump = true;
            g.EvasiveQuickStep = true;
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

            if (spawnInfo.Water) return 0f;

            //now spawns in hallow, since jungle was getting crowded
            //spawns more before the rage is defeated

            if (Main.hardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && !Main.dayTime && P.ZoneHallow && P.ZoneOverworldHeight && Main.rand.NextBool(30)) return 1;
            if (Main.hardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && !Main.dayTime && P.ZoneHallow && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.NextBool(25)) return 1;
            if (Main.hardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && Main.dayTime && P.ZoneHallow && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.NextBool(35)) return 1;
            if (Main.hardMode && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && P.ZoneHallow && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.NextBool(10)) return 1;
            if (Main.hardMode && spawnInfo.Lihzahrd && Main.rand.NextBool(45)) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneDesert && Main.rand.NextBool(45)) return 1;
            if (tsorcRevampWorld.SuperHardMode && P.ZoneHallow && Main.rand.NextBool(10)) return 1;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneGlowshroom && Main.rand.NextBool(5)) return 1;
            return 0;
        }
        #endregion

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write((sbyte)rushDir);
            writer.Write(rushDestX);
            writer.Write(HM);
            writer.Write(SHM);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            rushDir = reader.ReadSByte();
            rushDestX = reader.ReadSingle();
            HM = reader.ReadBoolean();
            SHM = reader.ReadBoolean();
        }

        ///<summary>Its coral staff's tip — every cast's color language originates here at top crescent head.</summary>
        Vector2 StaffTip => NPC.Center + new Vector2(NPC.direction * 14f, -32f);

        bool InkUnlocked => tsorcRevampWorld.SuperHardMode
            || tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WaterFiendKraken>()));

        public string DebugAttackLabel => State switch
        {
            AttackState.BubbleBarrage => "Bubble Barrage",
            AttackState.TidalCrest => "Tidal Crest",
            AttackState.BurstBubble => "Burst Bubble",
            AttackState.InkGeyser => "Ink Geyser",
            AttackState.TideRush => "Tide Rush",
            AttackState.IceArcVolley => "Ice Arc Volley",
            AttackState.IceOverflight => "Ice Overflight",
            AttackState.FrostSprites => "Frost Sprite Barrage",
            _ => "Neutral"
        };

        ///<summary>Poise break (neutral or the Burst Bubble's staggerable window): the cast pops early
        ///and harmlessly in its hands.</summary>
        public void OnStagger(NPC npc)
        {
            if (State != AttackState.None && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.7f, Pitch = 0.3f }, NPC.Center);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Water, vel.X, vel.Y, 60, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
            State = AttackState.None;
            AttackTimer = 0;
            RestoreKnockback();
            AttackCooldown = Math.Max(AttackCooldown, 70);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player player = Main.player[NPC.target];
            InitializeStats();

            g.AttackTelegraphing = false;
            g.AttackCommitted = false;

            if (g.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.2f, 0.1f);
                return;
            }
            NPC.rotation *= 0.85f;

            //Idle identity: dripping wet, faint blue staff glow, the occasional gurgle
            if (Main.rand.NextBool(8))
            {
                Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height * 0.6f));
                int drip = Dust.NewDust(pos, 4, 4, DustID.Water, 0f, 1f, 100, default, 0.8f);
                Main.dust[drip].velocity *= 0.3f;
            }
            Lighting.AddLight(StaffTip, 0.1f, 0.18f, 0.3f);
            if (Main.rand.NextBool(1000))
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.3f, Pitch = -0.3f }, NPC.Center); // water sound
            }

            if (State == AttackState.None)
            {
                tsorcRevampAIs.FighterAI(NPC, 2, 0.05f, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false, canWalkBackwards: true);

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

        void InitializeStats()
        {
            if (statsInitialized)
            {
                return;
            }
            statsInitialized = true;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return; //clients receive the tier flags via sync
            }
            //The old SetDefaults world-flag blocks, moved where the flags are actually reliable
            if (tsorcRevampWorld.SuperHardMode)
            {
                SHM = true;
                NPC.lifeMax = 1500;
                NPC.defense = 50;
                NPC.value = 3600;
            }
            else if (Main.hardMode)
            {
                HM = true;
                NPC.lifeMax = 500;
            }
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
        }

        void PickAttack(Player player)
        {
            float distTiles = NPC.Distance(player.Center) / 16f;
            bool sameLevel = Math.Abs(player.Bottom.Y - NPC.Bottom.Y) < 4 * 16f;

            Span<(AttackState state, float weight)> pool = stackalloc (AttackState, float)[]
            {
                (AttackState.BubbleBarrage, 1f),
                (AttackState.TidalCrest,    distTiles < 18f && sameLevel ? 0.9f : 0f),
                (AttackState.BurstBubble,   0.6f),
                (AttackState.InkGeyser,     InkUnlocked ? 0.7f : 0f),
                (AttackState.TideRush,      distTiles < 10f ? 2.5f : 1.2f),
                (AttackState.IceArcVolley,  distTiles < 34f ? 0.9f : 0.35f),
                (AttackState.IceOverflight, distTiles < 38f ? 0.7f : 0.25f),
                (AttackState.FrostSprites,  distTiles < 32f ? 0.45f : 0f),
            };
            float total = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].state == LastAttack && pool[i].state != AttackState.TideRush)
                {
                    pool[i].weight *= 0.5f;
                }
                total += pool[i].weight;
            }
            float roll = Main.rand.NextFloat(total);
            AttackState chosen = AttackState.BubbleBarrage;
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= pool[i].weight;
                if (roll <= 0f)
                {
                    chosen = pool[i].state;
                    break;
                }
            }
            State = chosen;
            AttackTimer = 0;
            NPC.netUpdate = true;
        }

        void EndAttack(int cooldown)
        {
            LastAttack = State;
            State = AttackState.None;
            AttackTimer = 0;
            RestoreKnockback();
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = cooldown + Main.rand.Next(40);
                NPC.netUpdate = true;
            }
        }

        void RestoreKnockback()
        {
            if (savedKnockBackResist >= 0f)
            {
                NPC.knockBackResist = savedKnockBackResist;
                savedKnockBackResist = -1f;
            }
        }

        void RunAttack(tsorcRevampGlobalNPC g, Player player)
        {
            if (player.dead || !player.active)
            {
                EndAttack(60);
                return;
            }
            //Face the player, except mid-surge (locked at dissolve end)
            if (!(State == AttackState.TideRush && AttackTimer > RushDissolveTicks))
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            }
            NPC.spriteDirection = NPC.direction;

            AttackTimer++;
            switch (State)
            {
                case AttackState.BubbleBarrage: RunBubbleBarrage(g, player); break;
                case AttackState.TidalCrest: RunTidalCrest(g); break;
                case AttackState.BurstBubble: RunBurstBubble(g); break;
                case AttackState.InkGeyser: RunInkGeyser(g, player); break;
                case AttackState.TideRush: RunTideRush(g, player); break;
                case AttackState.IceArcVolley: RunIceArcVolley(g, player); break;
                case AttackState.IceOverflight: RunIceOverflight(g, player); break;
                case AttackState.FrostSprites: RunFrostSprites(g, player); break;
            }
        }

        void RunBubbleBarrage(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= BarrageTelegraphTicks + 12;
            NPC.velocity.X *= 0.85f;

            if (AttackTimer <= BarrageTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
                }
                //BLUE: water gathering into the staff
                float progress = AttackTimer / (float)BarrageTelegraphTicks;
                for (int i = 0; i < 1 + (int)progress; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = StaffTip + angle.ToRotationVector2() * Main.rand.NextFloat(8f, 24f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Water, 0f, 0f, 80, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (StaffTip - pos) * 0.15f;
                }
                Lighting.AddLight(StaffTip, 0.15f * progress, 0.3f * progress, 0.5f * progress);
            }
            //Three soaking bubbles in a loose spread, re-aimed per shot
            if ((AttackTimer == BarrageTelegraphTicks || AttackTimer == BarrageTelegraphTicks + 6 || AttackTimer == BarrageTelegraphTicks + 12)
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = (player.Center - StaffTip).SafeNormalize(new Vector2(NPC.direction, 0f)).RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f)) * 6f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, vel,
                    ModContent.ProjectileType<Bubble>(), BubbleDamage, 1f, Main.myPlayer, 0f, 1f); //ai[1]=1: soaking mode
                SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.5f, Pitch = 0.2f }, NPC.Center);
            }
            if (AttackTimer >= BarrageTelegraphTicks + 12 + 20)
            {
                EndAttack(140);
            }
        }

        void RunTidalCrest(tsorcRevampGlobalNPC g)
        {
            g.AttackCommitted = AttackTimer <= CrestTelegraphTicks;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= CrestTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                }
                //CYAN at the staff while water visibly mounds at its feet — the wave forming
                if (Main.rand.NextBool(3))
                {
                    int staff = Dust.NewDust(StaffTip, 4, 4, DustID.Water, 0f, -1f, 60, default, 1.2f);
                    Main.dust[staff].noGravity = true;
                    Vector2 pos = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width) + NPC.direction * 12f, NPC.Bottom.Y - 6f);
                    int mound = Dust.NewDust(pos, 4, 4, DustID.Water, 0f, -2f, 80, default, 1.2f);
                    Main.dust[mound].noGravity = true;
                }
                if (AttackTimer == CrestTelegraphTicks)
                {
                    SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.7f, Pitch = -0.3f }, NPC.Bottom);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom + new Vector2(NPC.direction * 20f, -22f), Vector2.Zero,
                            ModContent.ProjectileType<QuaraTidalCrest>(), CrestDamage, 6f, Main.myPlayer, NPC.direction);
                    }
                }
            }
            else if (AttackTimer >= CrestTelegraphTicks + 25) //recovery, no armor
            {
                EndAttack(200);
            }
        }

        void RunBurstBubble(tsorcRevampGlobalNPC g)
        {
            if (AttackTimer <= BurstCastTicks)
            {
                NPC.velocity.X *= 0.8f;
                //The long cast: staggerable for the first two thirds — pop it before it pops you
                if (AttackTimer < BurstStaggerableTicks)
                {
                    g.AttackTelegraphing = true;
                }
                else
                {
                    g.AttackCommitted = true;
                }
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center);
                }
                if (AttackTimer == BurstStaggerableTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center); //commit cue
                }
                //The bubble visibly inflating at the staff
                float progress = AttackTimer / (float)BurstCastTicks;
                for (int i = 0; i < 1; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = StaffTip + angle.ToRotationVector2() * (6f + progress * 14f);
                    int dust = Dust.NewDust(pos, 2, 2, DustID.Water, 0f, 0f, 70, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.15f;
                }
                if (AttackTimer == BurstCastTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, new Vector2(NPC.direction * 2f, -0.5f),
                        ModContent.ProjectileType<QuaraBurstBubble>(), BurstDamage, 2f, Main.myPlayer);
                }
            }
            else if (AttackTimer >= BurstCastTicks + 20)
            {
                EndAttack(300);
            }
        }

        void RunInkGeyser(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= InkTelegraphTicks;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= InkTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                }
                //BLACK: ink gathering at the staff — the dark spell in the color language
                if (Main.rand.NextBool(2))
                {
                    int ink = Dust.NewDust(StaffTip - new Vector2(8f, 8f), 16, 16, DustID.Asphalt, 0f, -1f, 60, default, 1.4f);
                    Main.dust[ink].noGravity = true;
                }
                if (AttackTimer == InkTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //The cloud forms ON the player's position now (its own 120t build-up is the escape window)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center, Vector2.Zero,
                        ModContent.ProjectileType<InkGeyser>(), InkDamage, 1f, Main.myPlayer, NPC.target);
                }
            }
            else if (AttackTimer >= InkTelegraphTicks + 25)
            {
                EndAttack(420);
            }
        }

        void RunIceArcVolley(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= IceTelegraphTicks;
            NPC.velocity.X *= 0.8f;
            if (AttackTimer <= IceTelegraphTicks)
            {
                EmitIceTelegraph(halfRing: true, spiral: false, AttackTimer);
                if (AttackTimer == IceTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aim = (player.Center - StaffTip).SafeNormalize(new Vector2(NPC.direction, 0f));
                    for (int i = 0; i < 6; i++)
                    {
                        float spread = i - 2.5f;
                        // Middle shards climb the highest; the six trajectories read as an upside-down U
                        // before their short 30-tick course correction begins.
                        Vector2 velocity = new Vector2(aim.X * 6f + spread * 1.15f,
                            -8.2f + Math.Abs(spread) * 1.25f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity,
                            ModContent.ProjectileType<GigasIceShard>(), IceDamage, 1f, Main.myPlayer,
                            0.10f, IceGuidedThirty, NPC.target);
                    }
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.65f, Pitch = 0.15f }, StaffTip);
                }
            }
            else if (AttackTimer >= IceTelegraphTicks + IceRecoveryTicks)
            {
                EndAttack(210);
            }
        }

        void RunIceOverflight(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= IceTelegraphTicks;
            NPC.velocity.X *= 0.8f;
            if (AttackTimer <= IceTelegraphTicks)
            {
                EmitIceTelegraph(halfRing: false, spiral: true, AttackTimer);
                if (AttackTimer == IceTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int towardPlayer = Math.Sign(player.Center.X - StaffTip.X);
                    for (int i = 0; i < 4; i++)
                    {
                        float spread = i - 1.5f;
                        Vector2 velocity = new Vector2(towardPlayer * (6.2f + spread * 0.45f), -9f + Math.Abs(spread));
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity,
                            ModContent.ProjectileType<GigasIceShard>(), IceDamage, 1f, Main.myPlayer,
                            0f, IceOverflightThirty, NPC.target);
                    }
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.65f, Pitch = -0.05f }, StaffTip);
                }
            }
            else if (AttackTimer >= IceTelegraphTicks + IceRecoveryTicks)
            {
                EndAttack(230);
            }
        }

        void RunFrostSprites(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= IceTelegraphTicks;
            NPC.velocity.X *= 0.82f;
            if (AttackTimer <= IceTelegraphTicks)
            {
                EmitIceTelegraph(halfRing: false, spiral: false, AttackTimer);
                if (AttackTimer == IceTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int spawned = 0;
                    for (int attempt = 0; attempt < 32 && spawned < 8; attempt++)
                    {
                        if (!TryFindFrostSpritePosition(player, out Vector2 spawn))
                        {
                            continue;
                        }
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, Vector2.Zero,
                            ModContent.ProjectileType<QuaraFrostSprite>(), IceDamage, 1f, Main.myPlayer,
                            spawned * 15f, NPC.target);
                        spawned++;
                    }
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = -0.35f }, StaffTip);
                }
            }
            // The seeded frost sprites own their own 3-second warning and delayed shots, so Quara
            // may return to its normal move pool after this short recovery.
            else if (AttackTimer >= IceTelegraphTicks + IceRecoveryTicks)
            {
                EndAttack(260);
            }
        }

        void EmitIceTelegraph(bool halfRing, bool spiral, int timer)
        {
            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.5f, Pitch = -0.55f }, StaffTip);
            }
            float progress = timer / (float)IceTelegraphTicks;
            int count = 1 + (int)(progress * 3f);
            for (int i = 0; i < count; i++)
            {
                float angle = spiral
                    ? progress * MathHelper.TwoPi * 2.5f + Main.rand.NextFloat(-0.55f, 0.55f)
                    : halfRing
                        ? Main.rand.NextFloat(MathHelper.Pi, MathHelper.TwoPi)
                        : Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 position = StaffTip + angle.ToRotationVector2() * Main.rand.NextFloat(10f, 34f);
                Dust dust = Dust.NewDustPerfect(position, DustID.Frost, (StaffTip - position) * (0.10f + progress * 0.10f), 80, default,
                    Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
                if (i == 0)
                {
                    Dust glint = Dust.NewDustPerfect(position, DustID.IceTorch, dust.velocity * 0.55f, 100, default,
                        Main.rand.NextFloat(0.45f, 0.75f));
                    glint.noGravity = true;
                }
            }
            Lighting.AddLight(StaffTip, 0.18f * progress, 0.30f * progress, 0.52f * progress);
        }

        static bool TryFindFrostSpritePosition(Player player, out Vector2 spawn)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int tileX = (int)(player.Center.X / 16f) + Main.rand.Next(-12, 13);
                // A seven-tile (112px) tall band high above the player leaves the requested 15
                // clear tiles underneath it in ordinary ground arenas, instead of rejecting every
                // location merely because the player is standing on a floor.
                int tileY = (int)(player.Center.Y / 16f) - Main.rand.Next(16, 23);
                if (!WorldGen.InWorld(tileX, tileY, 16) || !HasClearTilesBelow(tileX, tileY, 15))
                {
                    continue;
                }
                spawn = new Vector2(tileX * 16f + 8f, tileY * 16f + 8f);
                return true;
            }
            spawn = Vector2.Zero;
            return false;
        }

        static bool HasClearTilesBelow(int tileX, int tileY, int tiles)
        {
            for (int y = tileY; y <= tileY + tiles; y++)
            {
                if (!WorldGen.InWorld(tileX, y, 16) || WorldGen.SolidTile(tileX, y))
                {
                    return false;
                }
            }
            return true;
        }

        void RunTideRush(tsorcRevampGlobalNPC g, Player player)
        {
            if (AttackTimer <= RushDissolveTicks)
            {
                g.AttackCommitted = true;
                NPC.velocity.X *= 0.7f;
                if (AttackTimer == 1)
                {
                    rushStartGround = NPC.Bottom;
                    lastRushWaterTileX = int.MinValue;
                    rushIceVolleyFired = false;
                    SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.7f }, NPC.Center);
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Water, vel.X, vel.Y - 1f, 60, default, 1.3f);
                        Main.dust[dust].noGravity = true;
                    }
                }
                if (AttackTimer == RushDissolveTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //Retreat, or flank to the player's far side with 6 tiles space
                    int towardPlayer = player.Center.X > NPC.Center.X ? 1 : -1;
                    bool flank = Main.rand.NextBool();
                    rushDestX = flank
                        ? player.Center.X + towardPlayer * 6 * 16f  //flanking past player with 6 tiles space
                        : NPC.Center.X - towardPlayer * 8 * 16f;    //retreat away 8 tiles
                    rushDir = Math.Sign(rushDestX - NPC.Center.X);
                    savedKnockBackResist = NPC.knockBackResist;
                    NPC.knockBackResist = 0f; //a rushing puddle can't be shoved off course
                    NPC.netUpdate = true;
                }
            }
            else if (AttackTimer <= RushDissolveTicks + RushMaxSurgeTicks)
            {
                //The surge: a low racing puddle — damageable but unshovable
                g.AttackCommitted = true;
                NPC.velocity.X = rushDir * 9f;
                //Splash trail - increased water dust density
                for (int i = 0; i < 4; i++)
                {
                    Vector2 pos = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - Main.rand.NextFloat(14f));
                    int splash = Dust.NewDust(pos, 4, 4, DustID.Water, -rushDir * 2.5f, -2f, 60, default, 1.4f);
                    Main.dust[splash].noGravity = true;
                }
                //Soaks anyone it flows through (no contact damage — NPC.damage is 0)
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnRushWaterMarker();
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player soakedPlayer = Main.player[i];
                        if (soakedPlayer.active && !soakedPlayer.dead && NPC.Hitbox.Intersects(soakedPlayer.Hitbox))
                        {
                            soakedPlayer.AddBuff(BuffID.Wet, 5 * 60);
                        }
                    }
                }
                bool arrived = (rushDir > 0 && NPC.Center.X >= rushDestX) || (rushDir < 0 && NPC.Center.X <= rushDestX);
                if (arrived || (NPC.collideX && AttackTimer > RushDissolveTicks + 8))
                {
                    AttackTimer = RushDissolveTicks + RushMaxSurgeTicks; //skip to reform
                    NPC.netUpdate = true;
                }
            }
            else
            {
                //Re-forming: vulnerable
                NPC.velocity.X *= 0.6f;
                RestoreKnockback();
                if (AttackTimer == RushDissolveTicks + RushMaxSurgeTicks + 1)
                {
                    SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
                    for (int i = 0; i < 12; i++)
                    {
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Water, 0f, -2f, 60, default, 1.2f);
                        Main.dust[dust].noGravity = true;
                    }
                }
                int iceTelegraphStart = RushDissolveTicks + RushMaxSurgeTicks + RushReformTicks;
                if (AttackTimer > iceTelegraphStart && AttackTimer <= iceTelegraphStart + IceTelegraphTicks)
                {
                    g.AttackCommitted = true;
                    EmitIceTelegraph(halfRing: false, spiral: false, AttackTimer - iceTelegraphStart);
                    if (AttackTimer == iceTelegraphStart + IceTelegraphTicks && !rushIceVolleyFired
                        && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        rushIceVolleyFired = true;
                        Vector2 target = FindGroundPoint(rushStartGround, 8);
                        if (target == Vector2.Zero)
                        {
                            target = rushStartGround;
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 velocity = (target - StaffTip).SafeNormalize(new Vector2(NPC.direction, 0f))
                                .RotatedBy((i - 1.5f) * 0.10f) * Main.rand.NextFloat(8.5f, 10.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity,
                                ModContent.ProjectileType<GigasIceShard>(), IceDamage, 1f, Main.myPlayer,
                                0.12f, IceWaterIgniter, -1f);
                        }
                        SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.72f, Pitch = -0.15f }, StaffTip);
                    }
                }
                else if (AttackTimer >= iceTelegraphStart + IceTelegraphTicks + IceRecoveryTicks)
                {
                    EndAttack(250);
                }
            }
        }

        void SpawnRushWaterMarker()
        {
            int tileX = (int)(NPC.Center.X / 16f);
            if (tileX == lastRushWaterTileX)
            {
                return;
            }
            lastRushWaterTileX = tileX;
            Vector2 ground = FindGroundPoint(NPC.Bottom, 4);
            if (ground == Vector2.Zero)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromAI(), ground - new Vector2(0f, 8f), Vector2.Zero,
                ModContent.ProjectileType<QuaraWaterResidue>(), IceDamage, 0f, Main.myPlayer);
        }

        static Vector2 FindGroundPoint(Vector2 worldPosition, int maxTilesDown)
        {
            int tileX = (int)(worldPosition.X / 16f);
            int startY = (int)(worldPosition.Y / 16f);
            for (int y = startY; y <= startY + maxTilesDown; y++)
            {
                if (WorldGen.InWorld(tileX, y, 16) && WorldGen.SolidTile(tileX, y))
                {
                    return new Vector2(tileX * 16f + 8f, y * 16f);
                }
            }
            return Vector2.Zero;
        }

        //On-hit evasion only from true neutral — recovery frames stay punishable
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

        public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (State == AttackState.TideRush && AttackTimer > RushDissolveTicks && AttackTimer <= RushDissolveTicks + RushMaxSurgeTicks)
            {
                NPC.alpha = 217; // 85% transparent during water surge dash
            }
            else
            {
                NPC.alpha = 0; // 100% opaque
            }
            return true;
        }

        public override void PostDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (State == AttackState.None)
            {
                return;
            }

            if (State == AttackState.TideRush)
            {
                if (AttackTimer <= RushDissolveTicks)
                {
                    Projectiles.Enemy.EnemyVFX.DrawQuaraTideRush(NPC.Center,
                        new Vector2(NPC.width, NPC.height), AttackTimer / (float)RushDissolveTicks, false, NPC.direction);
                }
                else if (AttackTimer <= RushDissolveTicks + RushMaxSurgeTicks)
                {
                    Projectiles.Enemy.EnemyVFX.DrawQuaraTideRush(NPC.Center,
                        new Vector2(NPC.width, NPC.height), 1f, false, rushDir);
                }
                else
                {
                    float reform = MathHelper.Clamp((AttackTimer - RushDissolveTicks - RushMaxSurgeTicks) / (float)RushReformTicks, 0f, 1f);
                    Projectiles.Enemy.EnemyVFX.DrawQuaraTideRush(NPC.Center,
                        new Vector2(NPC.width, NPC.height), reform, true, rushDir);
                }
                return;
            }

            float progress;
            int pattern;
            switch (State)
            {
                case AttackState.BubbleBarrage:
                    if (AttackTimer > BarrageTelegraphTicks + 12)
                        return;
                    progress = MathHelper.Clamp(AttackTimer / (float)BarrageTelegraphTicks, 0f, 1f);
                    pattern = 0;
                    break;
                case AttackState.TidalCrest:
                    if (AttackTimer > CrestTelegraphTicks)
                        return;
                    progress = MathHelper.Clamp(AttackTimer / (float)CrestTelegraphTicks, 0f, 1f);
                    pattern = 1;
                    break;
                case AttackState.BurstBubble:
                    if (AttackTimer > BurstCastTicks)
                        return;
                    progress = MathHelper.Clamp(AttackTimer / (float)BurstCastTicks, 0f, 1f);
                    pattern = 2;
                    break;
                case AttackState.InkGeyser:
                    if (AttackTimer > InkTelegraphTicks)
                        return;
                    progress = MathHelper.Clamp(AttackTimer / (float)InkTelegraphTicks, 0f, 1f);
                    pattern = 3;
                    break;
                default:
                    return;
            }
            Projectiles.Enemy.EnemyVFX.DrawQuaraCast(StaffTip, progress, pattern);
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                //The water in it escapes: a burst of spray before the body falls
                SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
                for (int i = 0; i < 20; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Water, vel.X, vel.Y - 2f, 50, default, 1.4f);
                    Main.dust[dust].noGravity = true;
                }
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 1").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 2").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 3").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 2").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 3").Type, 1.2f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofLight, 2));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 100, 1, 1, 8));
        }
    }
}
