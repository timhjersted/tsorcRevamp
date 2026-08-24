using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    //
    // Rebuilt with the Gigas Method (Documentation/EnemyRedesignSkillGuide.md). Structural template =
    // QuaraHydromancer; amphibious movement template = ClericOfSorrow's hover kit.
    //
    // Fantasy: an ambush-predator crab and BROOD-LEADER. Its whole kit runs on one rule — GOO -> IGNITE:
    // it douses the player (or the floor) in flammable black ichor, then lobs an ember to light it off.
    //   - Ichor Spit   : arcing goo globs; apply Doused (Slow + bleed + explosive-prime); misses splat a
    //                    floor patch that is harmless until ignited.
    //   - Ember Lob     : fire globe; ignites a Doused player (-> ichor detonation) and floor patches.
    //   - Burrow Ambush : dive -> travel underground/underwater -> erupt at the player's flank.
    //   - Pincer Leap   : leap + claw swipe with a white Slash VFX (Broodfather only).
    //   - Brood Call    : the one long STAGGERABLE channel -> clutch-crabs erupt to top up the brood.
    //
    // Amphibious: land-adept FighterAI PLUS a buoyant submerged pursuit (bubble jets, no swim anim needed).
    // Spawns in the Ocean and the Underground Desert. Aggressive bruiser (kiting off); the clutch-crab
    // copies kite. Rarity variants (Light / Medium / Elite "Quara Broodfather") scale HP, brood cap,
    // npcSlots and value.
    //
    // Poise contract (0.3/60 in PoiseProfiles): short casts hyper-armored; Brood Call's long channel is
    // staggerable for its first two thirds so a poise break cancels the summon.
    //
    // NOTE (build phase 1): movement + state skeleton + spawn + stats + rarity scaffold are LIVE; the five
    // attacks are dust-telegraph STUBS that fall straight through EndAttack. Real attacks + projectiles +
    // the Doused buff + QuaraClutchCrab + Brood Call land in later phases. See memory project_quara_pincher_rework.
    public class QuaraPincher : ModNPC, IStaggerable
    {
        enum AttackState : byte
        {
            None = 0,
            IchorSpit,     // b&b ranged: arcing goo globs (Doused) + a floor ichor patch on a miss
            EmberLob,      // the igniter: fire globe that lights Doused players and ichor patches
            BurrowAmbush,  // dive -> travel -> erupt at the player's flank
            PincerLeap,    // leap + claw swipe (white Slash VFX); Broodfather only
            BroodCall,     // long staggerable channel -> clutch-crabs erupt to top up the brood
        }

        // Rarity tiers: rolled once on spawn, scale HP / brood cap / npcSlots / value. Elite = "Quara Broodfather".
        enum Variant : byte { Light = 0, Medium = 1, Elite = 2 }

        //Timings (ticks). Placeholders for the stub attacks; real values arrive with each attack's build.
        const int IchorTelegraphTicks = 25;
        const int EmberTelegraphTicks = 40;
        const int BurrowTelegraphTicks = 30;
        const int LeapTelegraphTicks = 30;
        const int BroodCastTicks = 90;
        const int BroodStaggerableTicks = 60; //first two thirds: a poise break cancels the summon

        //Burrow sub-phase machine (dive -> travel -> erupt -> recovery)
        const int BurrowDiveTicks = 22;    //sink + fade out
        const int BurrowTravelMax = 70;    //force-erupt if the path takes too long
        const int BurrowEruptTicks = 16;   //fade in + burst
        const int BurrowRecoveryTicks = 28;

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;

        //Burrow state
        int burrowPhase;         //0=dive 1=travel 2=erupt 3=recovery
        Vector2 emergeTarget;    //where it bursts out
        bool emergeInWater;      //bubble dust vs dirt dust
        int savedDamage = -1;    //contact damage restored on erupt / at cleanup
        int evadeCooldown;       //throttles the on-hit burrow-to-flank reaction

        //Pincer Leap state
        int leapPhase;           //0=telegraph 1=airborne 2=swipe 3=recovery
        const int LeapSwipeTicks = 16;
        const int LeapRecoveryTicks = 28;

        bool statsInitialized;
        bool HM;
        bool SHM;

        Variant variant = Variant.Light;
        int broodCap = 1;        //how many clutch-crabs this leader may keep alive (set by variant)
        float HoverBob;          //drives the submerged bob

        bool Submerged => NPC.wet;
        bool IsBroodfather => variant == Variant.Elite;

        //Damage tiers (authored HALF; hostile projectiles deal 2x on hit). Used by the real attacks later.
        int GlobDamage
        {
            get
            {
                if (SHM)
                {
                    return 40;
                }

                if (HM)
                {
                    return 33;
                }

                return 28;
            }
        }

        int EmberDamage
        {
            get
            {
                if (SHM)
                {
                    return 45;
                }

                if (HM)
                {
                    return 38;
                }

                return 30;
            }
        }

        int ClawDamage
        {
            get
            {
                if (SHM)
                {
                    return 50;
                }

                if (HM)
                {
                    return 42;
                }

                return 34;
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
        }

        public override void SetDefaults()
        {
            AnimationType = 21;          //copies NPCID 21's fighter animation (idle=0, walk cycle, jump frame)
            NPC.aiStyle = -1;
            NPC.width = 18;
            NPC.height = 45;
            NPC.scale = 1.2f;
            NPC.damage = 56;
            NPC.defense = 12;
            NPC.lifeMax = 1200;          //base PreHM; scaled by tier + rarity in InitializeStats
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 2600f;
            NPC.npcSlots = 40f;          //bumped for rarer variants (crowds out other spawns)
            NPC.knockBackResist = 0.3f;  //overridden by the PoiseProfiles entry
            NPC.lavaImmune = false;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 60;      //seek high ground / ledges to spit from
            //Aggressive bruiser: no kite band (the clutch-crab copies kite, not the leader).
            globalNPC.MaxJumpPower = 10.5f;      //high, deliberate crab leap
            globalNPC.MaxJumpBoost = 5f;
            //On-hit evasion is bespoke: it burrows and re-emerges at the player's flank (see OnHitByX -> TryBurrowEvade).
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;
            if (Main.pumpkinMoon || Main.snowMoon || player.townNPCs > 0f || player.ZoneDungeon)
            {
                return 0f;
            }

            //Ocean (Beach zone) — its home. Also displaced into the Underground Desert.
            bool ocean = player.ZoneBeach;
            bool undergroundDesert = player.ZoneUndergroundDesert;

            if (ocean)
            {
                if (tsorcRevampWorld.SuperHardMode && Main.rand.NextBool(18))
                {
                    return 1f;
                }
                if (Main.hardMode && Main.rand.NextBool(30))
                {
                    return 1f;
                }
                if (Main.rand.NextBool(140))
                {
                    return 1f;
                }
            }
            if (undergroundDesert)
            {
                if (tsorcRevampWorld.SuperHardMode && Main.rand.NextBool(22))
                {
                    return 1f;
                }
                if (Main.hardMode && Main.rand.NextBool(45))
                {
                    return 1f;
                }
            }
            return 0f;
        }
        #endregion

        #region Netcode
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write(HM);
            writer.Write(SHM);
            writer.Write((byte)variant);
            writer.Write((byte)broodCap);
            writer.Write((byte)burrowPhase);
            writer.Write(emergeInWater);
            writer.WriteVector2(emergeTarget);
            writer.Write((byte)leapPhase);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            HM = reader.ReadBoolean();
            SHM = reader.ReadBoolean();
            variant = (Variant)reader.ReadByte();
            broodCap = reader.ReadByte();
            burrowPhase = reader.ReadByte();
            emergeInWater = reader.ReadBoolean();
            emergeTarget = reader.ReadVector2();
            leapPhase = reader.ReadByte();
        }
        #endregion

        ///<summary>Its mouth — where goo/ember casts originate and where inhale/telegraph dust converges.</summary>
        Vector2 Maw => NPC.Center + new Vector2(NPC.direction * 14f, -6f);

        ///<summary>Poise break (neutral or Brood Call's staggerable window): the cast fizzles, the brood
        ///summon is cancelled, and the crab sheds a spray of harmless goo.</summary>
        public void OnStagger(NPC npc)
        {
            if (State != AttackState.None && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Ichor, vel.X, vel.Y, 120, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
            State = AttackState.None;
            AttackTimer = 0;
            ResetBurrow();
            leapPhase = 0;
            AttackCooldown = Math.Max(AttackCooldown, 70);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player player = Main.player[NPC.target];
            InitializeStats();

            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;

            if (globalNPC.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.2f, 0.1f);
                if (Submerged)
                {
                    NPC.noGravity = true;
                }
                return;
            }
            NPC.rotation *= 0.85f;
            if (evadeCooldown > 0)
            {
                evadeCooldown--;
            }

            //Idle identity: dripping black ichor, faint amber glow at the maw, the occasional wet clack.
            if (Main.rand.NextBool(6))
            {
                Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height * 0.6f));
                int drip = Dust.NewDust(pos, 4, 4, DustID.Ichor, 0f, 1f, 120, default, 0.7f);
                Main.dust[drip].velocity *= 0.3f;
            }
            Lighting.AddLight(Maw, 0.18f, 0.12f, 0.02f);

            //Elite "Broodfather" reads as bigger + wreathed in ember light with rising cinders.
            if (IsBroodfather)
            {
                Lighting.AddLight(NPC.Center, 0.35f, 0.14f, 0.03f);
                if (Main.rand.NextBool(10))
                {
                    int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, -1.5f, 120, default, 1f);
                    Main.dust[dustIndex].noGravity = true;
                }
            }

            if (State == AttackState.None)
            {
                Move(globalNPC, player);

                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }
                bool grounded = Submerged || NPC.velocity.Y == 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackCooldown <= 0 && grounded
                    && !player.dead && player.active && NPC.Distance(player.Center) < 1100f)
                {
                    PickAttack(player);
                }
            }
            else
            {
                RunAttack(globalNPC, player);
            }
        }

        ///<summary>Amphibious locomotion. Dry: the aggressive-bruiser FighterAI (high leap, seeks high
        ///ground). Wet: a buoyant pursuit that can rise to the surface or dive after the player, bubbles
        ///jetting at the feet — no swim animation needed (see ClericOfSorrow).</summary>
        void Move(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            if (Submerged)
            {
                NPC.noGravity = true;
                HoverBob += 0.09f;

                float dx = player.Center.X - NPC.Center.X;
                int sign = dx >= 0 ? 1 : -1;
                //Aggressive: close the horizontal gap; only ease off at point-blank.
                float desiredX = Math.Abs(dx) < 3f * 16f ? sign * 0.8f : sign * 2.6f;

                //Chase the player's depth with a slow bob; clamp so it never rockets out of the water.
                float targetY = player.Center.Y - 24f + (float)Math.Sin(HoverBob) * 16f;
                float desiredY = MathHelper.Clamp((targetY - NPC.Center.Y) * 0.03f, -2.4f, 2.6f);

                NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(desiredX, desiredY), 0.08f);
                NPC.direction = NPC.spriteDirection = sign;

                //Bubble propulsion at the feet
                if (Main.rand.NextBool(2))
                {
                    int bub = Dust.NewDust(NPC.Bottom - new Vector2(NPC.width / 2f, 2f), NPC.width, 6, DustID.BubbleBurst_White, 0f, 1.2f, 120, default, 1f);
                    Main.dust[bub].noGravity = true;
                    Main.dust[bub].velocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(0.6f, 1.6f));
                }
            }
            else
            {
                NPC.noGravity = false;
                tsorcRevampAIs.FighterAI(NPC, 2f, 0.09f, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: true, canWalkBackwards: false);
            }
        }

        void PickAttack(Player player)
        {
            float distTiles = NPC.Distance(player.Center) / 16f;
            bool sameLevel = Math.Abs(player.Bottom.Y - NPC.Bottom.Y) < 4 * 16f;

            Span<(AttackState state, float weight)> pool = stackalloc (AttackState, float)[]
            {
                (AttackState.IchorSpit,    1f),
                (AttackState.EmberLob,     distTiles < 26f ? 0.7f : 0f),
                (AttackState.BurrowAmbush, distTiles > 6f ? 0.6f : 0.2f),
                (AttackState.PincerLeap,   distTiles < 12f && sameLevel ? 0.8f : 0f), //the main crab's melee (any variant)
                (AttackState.BroodCall,    broodCap > 0 && CountClutchCrabs() < broodCap ? 0.5f : 0f),
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
            if (total <= 0f)
            {
                return;
            }
            float roll = Main.rand.NextFloat(total);
            AttackState chosen = AttackState.IchorSpit;
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

        //Phase 1: every attack is a dust-telegraph STUB. It faces the player, poses/telegraphs, then bails
        //through EndAttack. The real behavior (projectiles, burrow machine, brood spawn) replaces each body
        //in later phases; the skeleton, poise flags and pacing are already correct.
        void RunAttack(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            //Burrow drives its own facing/motion (it's mid-travel); the others face the player.
            if (State != AttackState.PincerLeap && State != AttackState.BurrowAmbush)
            {
                NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
                if (!Submerged)
                {
                    NPC.velocity.X *= 0.8f; //plant while casting (dry)
                }
            }
            AttackTimer++;

            switch (State)
            {
                case AttackState.IchorSpit:
                    RunIchorSpit(globalNPC, player);
                    break;

                case AttackState.EmberLob:
                    RunEmberLob(globalNPC, player);
                    break;

                case AttackState.BurrowAmbush:
                    RunBurrow(globalNPC, player);
                    break;

                case AttackState.PincerLeap:
                    RunPincerLeap(globalNPC, player);
                    break;

                case AttackState.BroodCall:
                    RunBroodCall(globalNPC);
                    break;
            }
        }

        ///<summary>Converging-ichor windup at the maw, then 1–3 arcing globs. A hit douses; a miss splats a
        ///floor patch. Hyper-armored.</summary>
        void RunIchorSpit(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;
            if (AttackTimer <= IchorTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = 0.4f }, NPC.Center);
                }
                ConvergeMawDust(DustID.Ichor, AttackTimer / (float)IchorTelegraphTicks);
            }
            else if (AttackTimer == IchorTelegraphTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.NPCHit20 with { Pitch = -0.2f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int count = Main.rand.Next(1, 4);
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 lobVelocity = LobVelocity(Maw, player.Center, 9.5f, 3.2f).RotatedByRandom(0.18f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), Maw, lobVelocity,
                            ModContent.ProjectileType<Projectiles.Enemy.QuaraIchorGlob>(), GlobDamage, 1f, Main.myPlayer, 0.18f);
                    }
                }
            }
            else if (AttackTimer >= IchorTelegraphTicks + 24)
            {
                EndAttack(260);
            }
        }

        ///<summary>The igniter: a longer windup with fire dust gathering at the maw, then a single arcing
        ///ember at the player (lights doused players and ichor patches on impact). Hyper-armored.</summary>
        void RunEmberLob(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = true;
            if (AttackTimer <= EmberTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Pitch = 0.2f }, NPC.Center);
                }
                Lighting.AddLight(Maw, 0.5f, 0.3f, 0.08f);
                ConvergeMawDust(DustID.Torch, AttackTimer / (float)EmberTelegraphTicks);
            }
            else if (AttackTimer == EmberTelegraphTicks + 1)
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 lobVelocity = LobVelocity(Maw, player.Center, 10f, 3.5f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), Maw, lobVelocity,
                        ModContent.ProjectileType<Projectiles.Enemy.QuaraEmber>(), EmberDamage, 1f, Main.myPlayer, 0.2f);
                }
            }
            else if (AttackTimer >= EmberTelegraphTicks + 26)
            {
                EndAttack(300);
            }
        }

        ///<summary>Crouch → leap toward the player → claw swipe with a white swoosh on the descent → open
        ///recovery. Sub-phases via leapPhase; hyper-armored through the swipe, punishable on recovery.</summary>
        void RunPincerLeap(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = leapPhase < 3;

            switch (leapPhase)
            {
                case 0: //TELEGRAPH — crouch and brace
                    NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
                    NPC.velocity.X *= 0.8f;
                    if (AttackTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Zombie1 with { Pitch = 0.3f }, NPC.Center);
                    }
                    if (Main.rand.NextBool(2))
                    {
                        int dustIndex = Dust.NewDust(NPC.Bottom - new Vector2(NPC.width / 2f, 2f), NPC.width, 4, DustID.Ichor, 0f, -1f, 120, default, 1.1f);
                        Main.dust[dustIndex].noGravity = true;
                    }
                    if (AttackTimer >= LeapTelegraphTicks)
                    {
                        int dir = player.Center.X >= NPC.Center.X ? 1 : -1;
                        NPC.direction = NPC.spriteDirection = dir;
                        NPC.velocity = new Vector2(dir * 7f, -11f);
                        NPC.noGravity = false;
                        SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = -0.2f }, NPC.Center);
                        leapPhase = 1;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                case 1: //AIRBORNE — arc toward the player, steer slightly
                    NPC.velocity.Y += 0.35f;
                    float dx = player.Center.X - NPC.Center.X;
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(dx) * 6f, 0.03f);
                    NPC.direction = NPC.spriteDirection = Math.Sign(dx) == 0 ? NPC.direction : Math.Sign(dx);
                    if (Main.rand.NextBool(2))
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Ichor, 0f, 0f, 150, default, 0.9f);
                    }
                    bool landed = AttackTimer > 4 && NPC.velocity.Y == 0f;
                    bool reachedPlayer = NPC.velocity.Y > 0f && NPC.Distance(player.Center) < 56f;
                    if (landed || reachedPlayer || AttackTimer > 90)
                    {
                        StartSwipe(player);
                    }
                    break;

                case 2: //SWIPE — the claw arc (spawned at StartSwipe); settle in place
                    NPC.velocity.X *= 0.8f;
                    if (AttackTimer >= LeapSwipeTicks)
                    {
                        leapPhase = 3;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                default: //RECOVERY — no armor
                    NPC.velocity.X *= 0.9f;
                    if (AttackTimer >= LeapRecoveryTicks)
                    {
                        leapPhase = 0;
                        EndAttack(320);
                    }
                    break;
            }
        }

        void StartSwipe(Player player)
        {
            leapPhase = 2;
            AttackTimer = 0;
            NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = 0.2f }, NPC.Center); //swoosh
            UsefulFunctions.ScreenShake(NPC.Center, 3f, 12, 6f, 400f);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.QuaraClawSwipe>(), ClawDamage, 2f, Main.myPlayer, NPC.direction, NPC.whoAmI);
                NPC.netUpdate = true;
            }
        }

        ///<summary>Dust drawn inward toward the maw over a telegraph — the shared "a cast is charging" read.</summary>
        void ConvergeMawDust(int dustType, float progress)
        {
            if (!Main.rand.NextBool(2))
            {
                return;
            }
            Vector2 from = Maw + Main.rand.NextVector2Circular(30f, 30f) * (1.2f - progress);
            int dustIndex = Dust.NewDust(from, 2, 2, dustType, 0f, 0f, 120, default, 1.1f);
            Main.dust[dustIndex].noGravity = true;
            Main.dust[dustIndex].velocity = (Maw - from) * 0.08f;
        }

        ///<summary>A simple ballistic-ish lob: aim at the target, biased upward so gravity arcs it down.</summary>
        static Vector2 LobVelocity(Vector2 from, Vector2 target, float speed, float arc)
        {
            Vector2 dir = target - from;
            if (dir != Vector2.Zero)
            {
                dir.Normalize();
            }
            Vector2 launchVelocity = dir * speed;
            launchVelocity.Y -= arc;
            return launchVelocity;
        }

        int CountClutchCrabs()
        {
            int count = 0;
            int type = ModContent.NPCType<QuaraClutchCrab>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.type == type && (int)other.ai[0] == NPC.whoAmI)
                {
                    count++;
                }
            }
            return count;
        }

        ///<summary>Burrow Ambush: dive out of sight, rush underground/underwater to the player's flank, and
        ///burst back out with a lunging contact hit. Dirt dust on land, bubbles in water. Hyper-armored and
        ///untargetable while submerged; the short erupt recovery is the punish window. Doubles as the on-hit
        ///evasion (TryBurrowEvade). AttackTimer is reset to 0 at each phase transition and used per-phase.</summary>
        void RunBurrow(tsorcRevampGlobalNPC globalNPC, Player player)
        {
            globalNPC.AttackCommitted = burrowPhase < 3; //committed through the burst; recovery is open

            switch (burrowPhase)
            {
                case 0: //DIVE — pick the emerge point, sink, fade out
                    if (AttackTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.3f }, NPC.Center);
                        savedDamage = NPC.damage;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (!TryFindEmerge(player, out emergeTarget, out emergeInWater))
                            {
                                burrowPhase = 3; //no valid spot: think better of it, go to recovery
                                AttackTimer = 0;
                                NPC.netUpdate = true;
                                return;
                            }
                            NPC.netUpdate = true;
                        }
                    }
                    NPC.velocity.X *= 0.8f;
                    NPC.alpha = (int)MathHelper.Clamp(AttackTimer / (float)BurrowDiveTicks * 255f, 0, 255);
                    SpawnBurrowDust(NPC.Bottom, Submerged);
                    if (AttackTimer >= BurrowDiveTicks)
                    {
                        NPC.alpha = 255;
                        NPC.damage = 0;
                        NPC.dontTakeDamage = true; //untargetable while underground
                        NPC.noTileCollide = true;
                        NPC.noGravity = true;
                        burrowPhase = 1;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                case 1: //TRAVEL — invisible rush to the emerge point
                    NPC.alpha = 255;
                    Vector2 toTarget = emergeTarget - NPC.Center;
                    if (toTarget != Vector2.Zero)
                    {
                        NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Normalize(toTarget) * 12f, 0.2f);
                    }
                    NPC.direction = NPC.spriteDirection = emergeTarget.X >= NPC.Center.X ? 1 : -1;
                    if (Main.rand.NextBool(3))
                    {
                        SpawnBurrowDust(NPC.Center, emergeInWater);
                    }
                    if ((Math.Abs(NPC.Center.X - emergeTarget.X) < 20f && NPC.Center.Y >= emergeTarget.Y - 8 * 16f)
                        || AttackTimer >= BurrowTravelMax)
                    {
                        if (emergeInWater)
                        {
                            NPC.Center = emergeTarget;
                        }
                        else
                        {
                            NPC.position = new Vector2(emergeTarget.X - NPC.width / 2f, emergeTarget.Y - NPC.height);
                        }
                        burrowPhase = 2;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                case 2: //ERUPT — burst out, restore contact, lunge up toward the player
                    if (AttackTimer == 1)
                    {
                        NPC.noTileCollide = false;
                        NPC.dontTakeDamage = false;
                        NPC.damage = savedDamage > 0 ? savedDamage : 56;
                        NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
                        NPC.velocity = new Vector2(NPC.direction * 2f, -6f);
                        UsefulFunctions.ScreenShake(NPC.Center, 5f, 18, 6f, 500f);
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = 0.1f }, NPC.Center);
                        for (int i = 0; i < 34; i++)
                        {
                            int dustIndex = Dust.NewDust(NPC.Bottom - new Vector2(NPC.width / 2f, 4f), NPC.width, 8,
                                emergeInWater ? DustID.BubbleBurst_White : DustID.Dirt,
                                Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-6f, -1f), 100, default, 1.4f);
                            Main.dust[dustIndex].noGravity = emergeInWater;
                        }
                    }
                    NPC.alpha = (int)MathHelper.Clamp(255f - AttackTimer / (float)BurrowEruptTicks * 255f, 0, 255);
                    NPC.noGravity = Submerged;
                    if (AttackTimer >= BurrowEruptTicks)
                    {
                        NPC.alpha = 0;
                        burrowPhase = 3;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                default: //RECOVERY — no armor: the punish window
                    NPC.alpha = 0;
                    if (!Submerged)
                    {
                        NPC.velocity.X *= 0.9f;
                    }
                    if (AttackTimer >= BurrowRecoveryTicks)
                    {
                        ResetBurrow();
                        EndAttack(300);
                    }
                    break;
            }
        }

        ///<summary>Find a flanking emerge point near the player: prefer behind them, then their front, then a
        ///random side. Water spots emerge at the player's depth; land spots need >=4 tiles of solid below the
        ///surface with open space above to burst into. Returns false if nothing valid is near.</summary>
        bool TryFindEmerge(Player player, out Vector2 result, out bool water)
        {
            result = default;
            water = false;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                // Try behind the player first, then in front, then a coin flip.
                int sign;

                if (attempt == 0)
                {
                    sign = -player.direction;
                }
                else if (attempt == 1)
                {
                    sign = player.direction;
                }
                else
                {
                    sign = Main.rand.NextBool() ? 1 : -1;
                }

                if (sign == 0)
                {
                    sign = 1;
                }
                float targetX = player.Center.X + sign * Main.rand.Next(5, 9) * 16f;

                //Water emerge: burst up right at the player's depth if that spot is open liquid
                Vector2 atDepth = new(targetX, player.Center.Y);
                if (TileLiquidAt(atDepth) && !IsSolidAt(atDepth))
                {
                    result = atDepth;
                    water = true;
                    return true;
                }

                //Land emerge: scan down for the first solid surface, require >=4 thick + open space above
                Vector2 scan = new(targetX, player.Center.Y - 3 * 16f);
                for (int i = 0; i < 50; i++)
                {
                    if (IsSolidAt(scan))
                    {
                        bool thick = true;
                        for (int tier = 1; tier <= 4; tier++)
                        {
                            if (!IsSolidAt(scan + new Vector2(0, tier * 16f)))
                            {
                                thick = false;
                                break;
                            }
                        }
                        if (thick && !IsSolidAt(scan - new Vector2(0, 16f)))
                        {
                            result = new Vector2(targetX, (int)(scan.Y / 16f) * 16f); //top of the solid tile
                            water = false;
                            return true;
                        }
                        break; //surface not thick enough here; try another side
                    }
                    scan.Y += 16f;
                }
            }
            return false;
        }

        void SpawnBurrowDust(Vector2 pos, bool water)
        {
            if (!Main.rand.NextBool(2))
            {
                return;
            }
            int dustIndex = Dust.NewDust(pos - new Vector2(NPC.width / 2f, 2f), NPC.width, 6,
                water ? DustID.BubbleBurst_White : DustID.Dirt, 0f, water ? 1.2f : 0f, 100, default, 1.1f);
            Main.dust[dustIndex].noGravity = water;
            if (water)
            {
                Main.dust[dustIndex].velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(0.5f, 1.5f));
            }
        }

        ///<summary>Restore everything the burrow changed — the safety net for any exit path (incl. the player
        ///dying mid-travel while it's invisible and untargetable).</summary>
        void ResetBurrow()
        {
            burrowPhase = 0;
            NPC.alpha = 0;
            NPC.noTileCollide = false;
            NPC.dontTakeDamage = false;
            if (savedDamage > 0)
            {
                NPC.damage = savedDamage;
                savedDamage = -1;
            }
        }

        static bool IsSolidAt(Vector2 worldPos)
        {
            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 5)
            {
                return true;
            }
            Tile tile = Main.tile[tileX, tileY];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }

        static bool TileLiquidAt(Vector2 worldPos)
        {
            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 5)
            {
                return false;
            }
            return Main.tile[tileX, tileY].LiquidAmount > 0;
        }

        ///<summary>On-hit reaction (replaces the shared evasion): from true neutral, a chance to dive and
        ///re-emerge at the player's flank. Throttled so it can't chain-escape.</summary>
        void TryBurrowEvade()
        {
            if (State == AttackState.None && evadeCooldown <= 0 && Main.netMode != NetmodeID.MultiplayerClient
                && NPC.target >= 0 && !Main.player[NPC.target].dead && Main.rand.NextBool(3))
            {
                State = AttackState.BurrowAmbush;
                AttackTimer = 0;
                burrowPhase = 0;
                evadeCooldown = 240;
                NPC.netUpdate = true;
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            TryBurrowEvade();
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            TryBurrowEvade();
        }

        ///<summary>The one long channel: staggerable for its first two thirds (a poise break cancels the
        ///summon), hyper-armored tail with a commit cue at the flip. On the fire tick it tops the brood back
        ///up to broodCap — clutch-crabs erupt from the ground/water around the leader.</summary>
        void RunBroodCall(tsorcRevampGlobalNPC globalNPC)
        {
            NPC.velocity.X *= 0.85f;

            if (AttackTimer <= BroodStaggerableTicks)
            {
                globalNPC.AttackTelegraphing = true; //interruptible: break poise to deny the reinforcements
            }
            else
            {
                globalNPC.AttackCommitted = true;
            }
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Zombie1 with { Pitch = -0.4f }, NPC.Center);
            }
            if (AttackTimer == BroodStaggerableTicks) //commit cue at the flip
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Pitch = 0.3f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(new Color(150, 110, 40)));
                }
            }

            //rising rumble dust at the leader's feet — the "something's coming up" read
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = NPC.Bottom + new Vector2(Main.rand.NextFloat(-48f, 48f), 0f);
                int dustIndex = Dust.NewDust(pos, 2, 2, Submerged ? DustID.BubbleBurst_White : DustID.Dirt, 0f, -2.5f, 100, default, 1.2f);
                Main.dust[dustIndex].noGravity = true;
            }

            //Fire: spawn the missing brood members (server-authoritative).
            if (AttackTimer == BroodCastTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int needed = broodCap - CountClutchCrabs();
                if (needed > 0)
                {
                    SpawnClutchCrabs(needed);
                }
            }
            if (AttackTimer >= BroodCastTicks + 24)
            {
                EndAttack(360);
            }
        }

        void SpawnClutchCrabs(int amount)
        {
            int type = ModContent.NPCType<QuaraClutchCrab>();
            for (int spawnIndex = 0; spawnIndex < amount; spawnIndex++)
            {
                float originX = NPC.Center.X + Main.rand.Next(-7, 8) * 16f;
                FindCrabSpawn(originX, out Vector2 spot, out bool inWater);
                int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spot.X, (int)spot.Y, type);
                if (idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].ModNPC is QuaraClutchCrab crab)
                {
                    crab.ConfigureSpawn(NPC.whoAmI, inWater);
                    Main.npc[idx].target = NPC.target;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, idx);
                    }
                }
            }
        }

        ///<summary>A ground surface (bottom-center world pos) or an open-water spot near the leader for a
        ///clutch-crab to erupt from; falls back to the leader's feet if nothing is found.</summary>
        void FindCrabSpawn(float originX, out Vector2 spot, out bool inWater)
        {
            inWater = false;
            Vector2 atDepth = new(originX, NPC.Center.Y);
            if (TileLiquidAt(atDepth) && !IsSolidAt(atDepth))
            {
                spot = new Vector2(originX, NPC.Center.Y + NPC.height / 2f);
                inWater = true;
                return;
            }
            Vector2 scan = new(originX, NPC.Center.Y - 2 * 16f);
            for (int i = 0; i < 40; i++)
            {
                if (IsSolidAt(scan))
                {
                    spot = new Vector2(originX, (int)(scan.Y / 16f) * 16f);
                    return;
                }
                scan.Y += 16f;
            }
            spot = NPC.Bottom;
        }

        void EndAttack(int cooldown)
        {
            ResetBurrow(); //safety net: restore alpha / tile-collide / contact damage on every exit
            leapPhase = 0;
            LastAttack = State;
            State = AttackState.None;
            AttackTimer = 0;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = cooldown + Main.rand.Next(60);
                NPC.netUpdate = true;
            }
        }

        #region Stats & rarity
        void InitializeStats()
        {
            if (statsInitialized)
            {
                return;
            }
            statsInitialized = true;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return; //clients receive tier + variant via sync
            }

            //Rarity roll: Light ~70% / Medium ~25% / Elite ~5%. Sets brood cap, npcSlots, HP + value mult.
            float rarityRoll = Main.rand.NextFloat();
            float hpMult, valueMult;
            if (rarityRoll < 0.05f)
            {
                variant = Variant.Elite;
                broodCap = 4;
                NPC.npcSlots = 110f;
                hpMult = 2.2f;
                valueMult = 3.0f;
                NPC.scale = 1.45f;
            }
            else if (rarityRoll < 0.30f)
            {
                variant = Variant.Medium;
                broodCap = 2;
                NPC.npcSlots = 70f;
                hpMult = 1.5f;
                valueMult = 1.8f;
                NPC.scale = 1.3f;
            }
            else
            {
                variant = Variant.Light;
                broodCap = 1;
                NPC.npcSlots = 40f;
                hpMult = 1.0f;
                valueMult = 1.0f;
                NPC.scale = 1.2f;
            }

            //Tier base HP (PreHM 1200 / HM 2200 / SHM 4200), anchored above the Quara family.
            int baseHp = 1200;
            if (tsorcRevampWorld.SuperHardMode)
            {
                SHM = true;
                baseHp = 4200;
                NPC.defense = 24;
            }
            else if (Main.hardMode)
            {
                HM = true;
                baseHp = 2200;
                NPC.defense = 18;
            }

            NPC.lifeMax = (int)(baseHp * hpMult);
            float difficultyValueMult = 1f;

            if (SHM)
            {
                difficultyValueMult = 1.6f;
            }
            else if (HM)
            {
                difficultyValueMult = 1.25f;
            }

            NPC.value = 2600f * valueMult * difficultyValueMult;
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
        }
        #endregion

        ///<summary>Tier tell without new art: Medium darkens the shell, the Elite Broodfather takes an
        ///ember-scorched red cast. Respects the burrow fade (NPC.alpha).</summary>
        public override Color? GetAlpha(Color drawColor)
        {
            float fade = 1f - NPC.alpha / 255f;
            if (variant == Variant.Elite)
            {
                return Color.Lerp(drawColor, new Color(190, 95, 60), 0.45f) * fade;
            }
            if (variant == Variant.Medium)
            {
                return Color.Lerp(drawColor, new Color(120, 105, 80), 0.3f) * fade;
            }
            return null;
        }

        #region Gore
        public override void OnKill()
        {
            Color color = new();
            Rectangle rectangle = new((int)NPC.position.X, (int)(NPC.position.Y + ((NPC.height - NPC.width) / 2)), NPC.width, NPC.width);
            for (int i = 1; i <= 30; i++)
            {
                Dust.NewDust(NPC.position, rectangle.Width, rectangle.Height, DustID.Ichor, 0, 0, 120, color, 1.5f);
            }
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore1").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore2").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore2").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore3").Type, 1.2f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore3").Type, 1.2f);
        }
        #endregion
    }
}
