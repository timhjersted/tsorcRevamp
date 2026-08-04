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
    class QuaraHydromancer : ModNPC, IStaggerable
    {
        enum AttackState : byte
        {
            None = 0,
            BubbleBarrage, // three soaking bubbles, each bobbing on its own phase
            TidalCrest,    // the breaking wave — jump it or roll through
            BurstBubble,   // long staggerable cast → big drifting bubble, pops into droplets
            InkGeyser,     // the payoff: ink cloud at the player's position (Wet doubles its stick)
            TideRush,      // repositioning surge: retreat, or flank to the player's far side
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

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;
        int rushDir = 1;          //surge direction, locked at dissolve end
        float rushDestX;          //where the surge re-forms
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

        ///<summary>Its coral staff's tip — every cast's color language originates here.</summary>
        Vector2 StaffTip => NPC.Center + new Vector2(NPC.direction * 16f, -10f);

        bool InkUnlocked => tsorcRevampWorld.SuperHardMode
            || tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WaterFiendKraken>()));

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
                (AttackState.TideRush,      distTiles < 6f ? 1.5f : 0.15f),
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

        void RunTideRush(tsorcRevampGlobalNPC g, Player player)
        {
            if (AttackTimer <= RushDissolveTicks)
            {
                g.AttackCommitted = true;
                NPC.velocity.X *= 0.7f;
                if (AttackTimer == 1)
                {
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
                    //Retreat, or flank to the player's far side — make them turn around
                    int towardPlayer = player.Center.X > NPC.Center.X ? 1 : -1;
                    bool flank = Main.rand.NextBool();
                    rushDestX = flank
                        ? player.Center.X + towardPlayer * 12 * 16f  //past the player, far side
                        : NPC.Center.X - towardPlayer * 12 * 16f;    //straight back to kiting range
                    rushDir = Math.Sign(rushDestX - NPC.Center.X);
                    savedKnockBackResist = NPC.knockBackResist;
                    NPC.knockBackResist = 0f; //a rushing puddle can't be shoved off course
                    NPC.netUpdate = true;
                }
            }
            else if (AttackTimer <= RushDissolveTicks + RushMaxSurgeTicks)
            {
                //The surge: a low racing puddle — damageable but unshovable, harmless except for the soak
                g.AttackCommitted = true;
                NPC.velocity.X = rushDir * 9f;
                //Splash trail
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - Main.rand.NextFloat(14f));
                    int splash = Dust.NewDust(pos, 4, 4, DustID.Water, -rushDir * 2f, -1.5f, 60, default, 1.2f);
                    Main.dust[splash].noGravity = true;
                }
                //Soaks anyone it flows through (no contact damage — NPC.damage is 0)
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
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
                if (AttackTimer >= RushDissolveTicks + RushMaxSurgeTicks + RushReformTicks)
                {
                    EndAttack(180);
                }
            }
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
