using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Accessories.Defensive.Rings;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.Lifegem;
using tsorcRevamp.Content.Items.Weapons.Magic;
using tsorcRevamp.Content.Items.Weapons.Magic.Wands;
using tsorcRevamp.Content.Items.Weapons.Melee.Spears;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.Systems;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Pinwheel
{
    [AutoloadBossHead]

    //Current bugs:
    // (Fixed: clones spawning without line of sight - GenerateTeleportPosition passed tile coords to the pixel-space Collision.CanHit.
    //  Fixed: spawning in the ceiling from the Boss Rematch Tome - BossSelectVisuals spawned every boss 400px above the player.)
    // - There is sometimes some jankiness with pinwheel sliding/moving when it shouldnt.
    // - Sometimes pinwheels CreateClones() animation plays weirdly, still happens rarely despite my best efforts. 
    // - Pinwheel takes damage from sword slashes when NPC.dontTakeDamage is set to true. It doesn't take damage from projectiles of any sort though. I don't know how
    //      our swords work so I don't know how to fix this. This leads to other rare bugs like boss going invis during phase transitions.

    internal class Pinwheel : BossBase
    {

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 23;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }


        public override void SetDefaults()
        {
            //Calling base.SetDefaults takes care of a lot of variables that all bosses share
            //Things like making them hostile, marking them as a boss, etc
            base.SetDefaults();

            //The rest are unique to this specific boss, and we have to set here:
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.height = 135;
            NPC.width = 60;
            NPC.damage = 0;
            NPC.defense = 6;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = (int)(2000 * HealthScale);
            NPC.timeLeft = 180;
            NPC.value = 187500;  //7500 DS
            NPC.rarity = 4;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Pinwheel.DespawnHandler1"), Color.Firebrick, 6);


            //You can also specify BossBase specific values here
            introDuration = 40;
            attackTransitionDuration = 180;
            phaseTransitionDuration = 1000;
            deathAnimationDuration = 180;
            randomAttacks = true;
        }

        public bool isClone = false; //Used to separate true boss from clones
        bool cloneSpawned = false; //Used to set clones HP and Max HP to 200 right after they spawn
        bool introFinished = false; //Check if intro has finished, used to pick movesBeforeTeleport early

        public int mainBossIndex; //Main boss index in Main.NPC, used by clones in order to behave relative to things relating to the main boss such as dying when the boss does, etc.
        bool gotMainBossIndex = false; //This gets set to true once clones have the main bosses index

        Vector2? cloneSpawnLocation1; //Used when selecting the spawn location of a clone
        Vector2? cloneSpawnLocation2; //Used when selecting the spawn location of the second clone
        float transparency = 0.5f; //Used for "clones" texture when boss creates clones
        bool justTeleported = false; //Used to restore texture opacity after teleporting, reset back to false once fully opaque
        int opacityTimer = 30; //Used when removing and restoring texture opacity
        double opacity; //Used in conjunction with opacityTimer
        int movesBeforeTeleport = 0; //How many moves until next teleport, re-rolled at the end of the intro and after each teleport
        int moveCount = 0; //How many moves perforned, used in conjuntion with movesBeforeTeleport
        float damageModifier = 1; //Multiplicator for damage, changed based on phase

        //Teleport bookkeeping is decided on the server only (AttackTransition / ExecuteQueuedTeleport), but clients read it to drive
        //the fade-out before a warp and the fade-in after one. base sends MoveIndex + transition timers first.
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(moveCount);
            writer.Write(movesBeforeTeleport);
            writer.Write(justTeleported);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            moveCount = reader.ReadInt32();
            movesBeforeTeleport = reader.ReadInt32();
            justTeleported = reader.ReadBoolean();
        }

        public override bool PreAI()
        {
            if (NPC.ai[3] == 1)
            {
                NPC.BossBar = Main.BigBossProgressBar.NeverValid; //Prevents clones from having boss health bars
            }
            return base.PreAI();
        }

        public override void AI()
        {
            NPC.dontTakeDamage = false;
            if (NPC.ai[3] == 1)
            {
                introDuration = 30;

                NPC.BossBar = Main.BigBossProgressBar.NeverValid; //Prevents clones from having boss health bars
                isClone = true;
                NPC.DeathSound = SoundID.NPCDeath6;
                NPC.timeLeft = 180;
                NPC.value = 0;
                NPC.boss = false;
                despawnHandler = new NPCDespawnHandler(null, Color.Black, 6);
                //Clone HP follows the same New Enemy Balance multipliers as the real boss's two life bars
                //(bar 1 via LifeMultiplier, bar 2 via PhaseLifeMultiplier), so the clone:boss ratio holds with the toggle on.
                if (Phase == 0) //This allows us to bring current hp down to 200 as soon as it spawns
                {
                    NPC.lifeMax = EnemyBalance.ScaleLife(NPC.type, (int)(200 * HealthScale));

                    if (!cloneSpawned)
                    {
                        NPC.life = NPC.lifeMax;
                        cloneSpawned = true;
                    }
                }
                if (Phase >= 1)
                {
                    NPC.lifeMax = (int)(400 * HealthScale * EnemyBalance.PhaseLifeMultiplier(NPC.type, 2));

                    if (!cloneSpawned)
                    {
                        NPC.life = NPC.lifeMax;
                        cloneSpawned = true;
                    }
                }
            }

            //Fade in/out for teleports, intro and death. Runs here rather than in PreDraw because AI gates invulnerability and
            //movement on it: PreDraw never runs on a dedicated server (opacity stuck at 0 = permanently invulnerable) or while off-screen.
            //It runs before the early returns below so it ticks during death and phase transitions too, exactly as the draw call did.
            #region Opacity

            bool teleportFadeOut = introFinished && !justTeleported && attackTransitionTimeRemaining > 0 && attackTransitionTimeRemaining <= 30 && moveCount >= movesBeforeTeleport;
            bool cloneSplitFadeOut = MoveIndex == PinwheelAttackID.CreateClonesID && MoveTimer > 190 && MoveTimer <= 220;
            bool arenaWarpWindow = phaseTransitionTimeRemaining > phaseTransitionDuration - 90 && Phase == 1;

            //Grouping kept identical to the old one-line condition: the arena-warp exception only ever applied to the clone split.
            if (teleportFadeOut || (cloneSplitFadeOut && !arenaWarpWindow))
            {
                opacityTimer--;
            }
            if (opacityTimer < 30 && justTeleported)
            {
                opacityTimer++;
            }

            //"Death" during the phase 1 transition: fade out, warp to the arena centre at -110, fade back in
            if (phaseTransitionTimeRemaining < phaseTransitionDuration - 80 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 110)
            {
                opacityTimer--;
            }
            if (phaseTransitionTimeRemaining == phaseTransitionDuration - 110)
            {
                opacityTimer = 0;
            }
            if (phaseTransitionTimeRemaining < phaseTransitionDuration - 110 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 140)
            {
                opacityTimer++;
            }

            opacityTimer = Math.Clamp(opacityTimer, 0, 30);
            opacity = opacityTimer / 30.0;

            //The intro fades in over 40 ticks for the boss, 30 for clones (their introDuration)
            if (introTimer < introDuration && !isClone)
            {
                opacity = introTimer / 40.0;
            }
            if (introTimer < introDuration && isClone)
            {
                opacity = introTimer / 30.0;
            }

            //Death fade over the second half of the death animation
            if (deathAnimationProgress > deathAnimationDuration / 2)
            {
                opacity = 2 - deathAnimationProgress / 90.0;
            }
            if (deathAnimationProgress > deathAnimationDuration / 2 && Phase == 6 && !isClone)
            {
                opacity = 2 - deathAnimationProgress / 150.0;
            }

            #endregion

            if (Phase >= 1)
            {
                attackTransitionDuration = 120;
                NPC.defense = 10;
                damageModifier = 2f;

                #region Losing Lanterns

                Vector2 lanternBottomLeft = new Vector2(NPC.position.X - 55, NPC.position.Y + 88);
                Vector2 lanternMiddleLeft = new Vector2(NPC.position.X - 58, NPC.position.Y + 16);
                Vector2 lanternTopLeft = new Vector2(NPC.position.X - 18, NPC.position.Y - 40);
                Vector2 lanternTopRight = new Vector2(NPC.position.X + 68, NPC.position.Y - 40);
                Vector2 lanternMiddleRight = new Vector2(NPC.position.X + 106, NPC.position.Y + 16);
                Vector2 lanternBottomRight = new Vector2(NPC.position.X + 105, NPC.position.Y + 88);

                if (MoveTimer > 20)
                {
                    lanternBottomLeft = new Vector2(NPC.position.X - 59, NPC.position.Y + 64);
                    lanternMiddleLeft = new Vector2(NPC.position.X - 54, NPC.position.Y + 6);
                    lanternTopLeft = new Vector2(NPC.position.X - 12, NPC.position.Y - 50);
                    lanternTopRight = new Vector2(NPC.position.X + 62, NPC.position.Y - 50);
                    lanternMiddleRight = new Vector2(NPC.position.X + 104, NPC.position.Y + 6);
                    lanternBottomRight = new Vector2(NPC.position.X + 109, NPC.position.Y + 64);
                }

                if (phaseTransitionTimeRemaining == 0 && introTimer > 1)
                {
                    //Phases 1-5 each own one lantern break, at an unscaled life threshold of the REAL boss.
                    //Clones read the real boss's life so every Pinwheel sheds the same lantern at the same moment.
                    int lanternThreshold = 0;
                    List<Vector2> breakingLanterns = new List<Vector2>();

                    switch (Phase)
                    {
                        case 1:
                            lanternThreshold = 4100;
                            breakingLanterns.Add(lanternMiddleLeft);
                            break;
                        case 2:
                            lanternThreshold = 3700;
                            breakingLanterns.Add(lanternBottomRight);
                            break;
                        case 3:
                            lanternThreshold = 3300;
                            breakingLanterns.Add(lanternBottomLeft);
                            break;
                        case 4:
                            lanternThreshold = 2900;
                            breakingLanterns.Add(lanternMiddleRight);
                            break;
                        case 5:
                            lanternThreshold = 2500;
                            breakingLanterns.Add(lanternTopLeft);
                            breakingLanterns.Add(lanternTopRight);
                            break;
                    }

                    int bossLife = NPC.life;
                    if (isClone)
                    {
                        bossLife = Main.npc[mainBossIndex].life;
                    }

                    int scaledThreshold = (int)(lanternThreshold * HealthScale * EnemyBalance.PhaseLifeMultiplier(NPC.type, 2));
                    bool dyingClone = isClone && NPC.life <= 1;

                    //A dying clone still keeps pace with lanterns 1-4, but must not enter the phase 6 transition.
                    bool lanternBreaks = lanternThreshold > 0 && bossLife < scaledThreshold;
                    if (Phase == 5 && dyingClone)
                    {
                        lanternBreaks = false;
                    }

                    if (lanternBreaks)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);

                        //The first lantern's embers pop more upward (-1 max Y speed instead of -0.5)
                        float slowestRise = -0.5f;
                        if (Phase == 1)
                        {
                            slowestRise = -1f;
                        }

                        foreach (Vector2 lantern in breakingLanterns)
                        {
                            //Shatter dust on every machine (it used to sit inside the server gate, so MP clients never saw it)
                            if (!Main.dedServ)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lantern, 8, 10, DustID.Torch, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                            }

                            //Spilled greek fire from the lantern's centre (+4,+5 is the middle of its 8x10 socket)
                            if (Main.netMode != NetmodeID.MultiplayerClient && !dyingClone)
                            {
                                Vector2 spillPoint = lantern + new Vector2(4, 5);
                                int[] greekFireTypes = { ProjectileID.GreekFire1, ProjectileID.GreekFire2, ProjectileID.GreekFire3, ProjectileID.GreekFire3 };

                                foreach (int greekFireType in greekFireTypes)
                                {
                                    Vector2 spillVelocity = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, slowestRise));
                                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spillPoint, spillVelocity, greekFireType, 18, 0, Main.myPlayer, 0, 0);
                                }
                            }
                        }

                        if (Phase < 5)
                        {
                            if (Phase == 4)
                            {
                                attackTransitionDuration = 140;
                            }

                            phaseTransitionTimeRemaining = phaseTransitionDuration;
                            Phase++; //Not NextPhase(): lantern losses must not reset MoveTimer mid-attack
                        }
                        else
                        {
                            attackTransitionDuration = 180;
                            phaseTransitionDuration = 400;
                            if (!isClone)
                            {
                                deathAnimationDuration = 250;
                            }

                            NextPhase();
                        }

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.netUpdate = true;
                        }
                    }
                }

                #endregion

            }

            if (Phase == 6)
            {
                //damageModifier = 3f;
                if (!isClone && phaseTransitionTimeRemaining == 100) despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Pinwheel.DespawnHandler2"), Color.DarkViolet, DustID.ShadowbeamStaff);
            }

            if (Phase >= 2 && Phase < 6) //No phase transitions on lantern loss "phases"
            {
                phaseTransitionDuration = 0;
                phaseTransitionTimeRemaining = 0;
            }

            //If the NPC is dying, then do nothing else until it's done
            if (NPC.life <= 1 && Phase == 0)
            {
                NPC.velocity = Vector2.Zero;

                if ((deathAnimationProgress / 2) < deathAnimationDuration && opacityTimer < 30)
                {
                    //opacity = 1;
                    //opacityTimer = 30;
                    opacityTimer++;
                }

                if (deathAnimationProgress <= deathAnimationDuration)
                {
                    HandleDeath();
                    deathAnimationProgress++;
                    return;
                }
            }

            if (phaseTransitionTimeRemaining > 0)
            {
                PhaseTransition();
                phaseTransitionTimeRemaining--;
                return;
            }

            if (!introFinished && introTimer > introDuration)
            {
                introFinished = true;
                movesBeforeTeleport = DecideMovesBeforeTeleport(Main.npc[NPC.whoAmI]); //DecideMovesBeforeTeleport when intro finishes
                if (!isClone)
                {
                    justSkippedMove = true;
                }
            }

            if (!gotMainBossIndex && isClone) //Get the index of the main boss, to be able to make clones behave based on what main boss does
            {
                for (int i = 0; i < Main.maxNPCs; i++) //Loop through all NPCs
                {
                    if (Main.npc[i].type == ModContent.NPCType<Pinwheel>() && Main.npc[i].boss && Main.npc[i].ai[3] != 1) //If the npc is pinwheel, is a boss and isnt a clone
                    {
                        mainBossIndex = i; //Save index to mainBossIndex
                        gotMainBossIndex = true;
                    }
                }
            }

            if (isClone && (Main.npc[mainBossIndex].life <= 1 || !Main.npc[mainBossIndex].active || (NPC.HasValidTarget && Target.Distance(NPC.Center) > 2000))) //Kill clones if main boss dies, despawns or if player is far
            {
                MoveTimer = 0;
                NPC.life = 1;
            }

            if (!isClone && NPC.CountNPCS(ModContent.NPCType<Pinwheel>()) < 3 && NPC.ai[0] != 0 && attackTransitionTimeRemaining == 30 && Phase == 0) //If there are less than 3 Pinwheels and it wasn-t about to spawn clones anyway. Check transition time so as not to interrupt current attack
            {
                MoveIndex = PinwheelAttackID.CreateClonesID; //Then spawn clones, while not skipping what attack was coming next anyway

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }
            else if (!isClone && NPC.CountNPCS(ModContent.NPCType<Pinwheel>()) < 3 && MoveIndex != PinwheelAttackID.CreateClonesID && Phase >= 1) //Same as previous phase but will interrupt attacks, causing clones to respawn sooner
            {
                MoveIndex = PinwheelAttackID.CreateClonesID;
                MoveTimer = 0;
                NextMove();
                justSkippedMove = true;
            }
            if (attackTransitionTimeRemaining == attackTransitionDuration) //When an attack is going to start
            {
                moveCount++; //Increase moveCount
            }

            if (opacity < 1) //If not fully opaque
            {
                NPC.dontTakeDamage = true; //Become invincible
                NPC.velocity = Vector2.Zero; //Don't move
            }

            if (opacityTimer == 30 && attackTransitionTimeRemaining == 0) //If fully opaque again
            {
                justTeleported = false;
            }

            base.AI(); //Continue with base.AI(), which is the AI found in BossBase
        }

        /// <summary>
        /// Add all the moves and damage numbers for your boss in here!
        /// You need the function name, the time the attack lasts, and optionally can specify a color (for use with things like VFX or lighting)
        /// </summary>
        public override void InitializeMovesAndDamage()
        {
            //Create a new function for every move of your boss, and then add them to this list alongside the duration of the attack
            MoveList = new List<BossMove>
            {
                new BossMove(CreateClones, 220, id : PinwheelAttackID.CreateClonesID), //Index 0 always opens the fight
                new BossMove(VolcanicEruption, 370, id : PinwheelAttackID.VolcanicEruptionID),
                new BossMove(Flamethrower, 420, id : PinwheelAttackID.FlamethrowerID),
                new BossMove(BouncingFireball, 180, id: PinwheelAttackID.BouncingFireballID),
                new BossMove(KillableFireball, 150, id: PinwheelAttackID.KillableFireballID),
                new BossMove(BlindingPulse, 210, id: PinwheelAttackID.BlindingPulseID),

            };


            //Set the damage numbers for every attack or projectile your boss uses here
            //Remember: Contact damage is doubled, and projectile damage is multiplied by 4!
            DamageNumbers = new Dictionary<string, int>
            {
                ["BouncingFireballDamage"] = 8,
                ["KillableFireballDamage"] = 26,
                ["FlamethrowerDamage"] = 6,
                ["VolcanicEruptionDamage"] = 9,
                ["BlindingPulseDamage"] = 8,
            };
        }

        //So I don't have to remember magic numbers
        public class PinwheelAttackID
        {
            public const short CreateClonesID = 0;
            public const short VolcanicEruptionID = 1;
            public const short FlamethrowerID = 2;
            public const short BouncingFireballID = 3;
            public const short KillableFireballID = 4;
            public const short BlindingPulseID = 5;
        }


        #region Attacks


        public void BouncingFireball() //Shoots 3 bouncing fireballs which arent affected by gravity
        {
            NPC.velocity.X *= 0f;

            if (Phase < 6)
            {
                float num48 = 5f; //used for projectile velocity
                Vector2 vector8 = new Vector2(NPC.Center.X, NPC.Center.Y - 30);
                Vector2 shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 2);
                float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X);
                float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y);


                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                num51 = num48 / num51;
                speedX *= num51;
                speedY *= num51;

                int dustQuantity = (int)MoveTimer / 10;
                for (int i = 0; i < dustQuantity; i++)
                {
                    if (MoveTimer > 20)
                    {
                        UsefulFunctions.DustRing(vector8, 10, 6, 3, 4);
                    }
                }

                if (MoveTimer == 90 && Phase < 5) //Shoot bouncing fireball
                {                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ProjectileID.Fireball;
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX, speedY), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0, 1);
                        shot1.friendly = false;
                        shot1.hostile = true;
                        shot1.timeLeft = 600;
                        if (Phase >= 1) shot1.timeLeft = 720;
                    }
                }

                if (MoveTimer == 120 && Phase < 3) //Shoot bouncing fireball
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ProjectileID.Fireball;
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX, speedY), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0, 1);
                        
                        shot1.friendly = false;
                        shot1.hostile = true;
                        shot1.timeLeft = 600;
                        if (Phase >= 1) shot1.timeLeft = 720;
                    }
                }

                if (MoveTimer == 150) //Shoot bouncing fireball
                {                        
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ProjectileID.Fireball;
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX, speedY), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0, 1);
                        shot1.friendly = false;
                        shot1.hostile = true;
                        shot1.timeLeft = 600;
                        if (Phase >= 1) shot1.timeLeft = 720;
                    }
                }
            }
            if (Phase == 6)
            {
                float num48 = 5f; //used for projectile velocity
                Vector2 vector8 = new Vector2(NPC.Center.X, NPC.Center.Y - 30);
                Vector2 shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 2);
                float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X);
                float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y);


                float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                num51 = num48 / num51;
                speedX *= num51;
                speedY *= num51;

                int dustQuantity = (int)MoveTimer / 10;
                for (int i = 0; i < dustQuantity; i++)
                {
                    if (MoveTimer > 20)
                    {
                        UsefulFunctions.DustRing(vector8, 10, DustID.Shadowflame, 1, 5);
                    }
                }

                if (MoveTimer == 90) //Shoot bouncing fireball
                {                        
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ProjectileID.CultistBossFireBallClone;

                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX * 0.5f, speedY * 0.5f), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0);
                        shot1.friendly = false;
                        shot1.hostile = true;
                        shot1.timeLeft = 600;
                    }
                }

                if (MoveTimer == 120) //Shoot bouncing fireball
                {                        
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ProjectileID.CultistBossFireBallClone;

                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX * 0.5f, speedY * 0.5f), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0);
                        shot1.friendly = false;
                        shot1.hostile = true;
                        shot1.timeLeft = 600;
                    }
                }

                if (MoveTimer == 150) //Shoot bouncing fireball
                {                        
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ProjectileID.CultistBossFireBallClone;

                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX * 0.5f, speedY * 0.5f), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0);
                        shot1.friendly = false;
                        shot1.hostile = true;
                        shot1.timeLeft = 600;
                    }
                }
            }
        }

        public void KillableFireball() //Shoots 2 groups of fireballs which can be killed/destroyed, 1 from each lantern
        {
            NPC.velocity.X *= 0f;

            Vector2 lanternBottomLeft = new Vector2(NPC.position.X - 55, NPC.position.Y + 88);
            Vector2 lanternMiddleLeft = new Vector2(NPC.position.X - 58, NPC.position.Y + 16);
            Vector2 lanternTopLeft = new Vector2(NPC.position.X - 18, NPC.position.Y - 40);
            Vector2 lanternTopRight = new Vector2(NPC.position.X + 68, NPC.position.Y - 40);
            Vector2 lanternMiddleRight = new Vector2(NPC.position.X + 106, NPC.position.Y + 16);
            Vector2 lanternBottomRight = new Vector2(NPC.position.X + 105, NPC.position.Y + 88);
            int projectileType = ModContent.NPCType<GaibonFireball>();
            if (Phase == 6) projectileType = ModContent.NPCType<PinwheelFireball>();
            Vector2 shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 6);
            if (Phase >= 1) shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 8);

            if (MoveTimer == 90 || MoveTimer == 120) //Shoot killable Gaibon fireballs, one per lantern still lit
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //Lanterns break one per phase 2-5 (top pair last); phase 6 relights all six with shadowflame
                    List<Vector2> litLanterns = new List<Vector2>();
                    litLanterns.Add(lanternTopLeft);
                    litLanterns.Add(lanternTopRight);
                    if (Phase < 5 || Phase == 6)
                    {
                        litLanterns.Add(lanternMiddleRight);
                    }
                    if (Phase < 4 || Phase == 6)
                    {
                        litLanterns.Add(lanternBottomLeft);
                    }
                    if (Phase < 2 || Phase == 6)
                    {
                        litLanterns.Add(lanternMiddleLeft);
                    }
                    if (Phase < 3 || Phase == 6)
                    {
                        litLanterns.Add(lanternBottomRight);
                    }

                    //Damage rides in ai[3]: NPC.damage isn't synced, and contact damage is checked on the hit player's client
                    int fireballDamage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);

                    foreach (Vector2 lantern in litLanterns)
                    {
                        NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lantern.X, (int)lantern.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y, fireballDamage);
                    }
                }
            }
        }

        public void Flamethrower() //Shoots a sustained flamethrower in the targets direction
        {
            NPC.velocity.X *= 0f;

            int dustQuantity = (int)MoveTimer / 15;
            for (int i = 0; i < dustQuantity; i++)
            {
                if (MoveTimer > 40 && MoveTimer < 300)
                {
                    int type = 6;
                    int speed = 3;
                    if (Phase == 6)
                    {
                        type = DustID.ShadowbeamStaff;
                        speed = 6;
                    }
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), 6, type, 1, speed);
                }
            }

            if (MoveTimer >= 120 && MoveTimer < 300)
            {
                Vector2 vector8 = new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30);
                Vector2 shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1f);
                if (Phase == 1) shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1.25f);
                if (Phase > 1 && Phase < 5) shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1f);
                if (Phase == 5) shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 0.6f);
                if (Phase == 6) shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1.4f);
                int projectileType = ModContent.ProjectileType<SmallFlameJet>();

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (Phase < 6)
                    {
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(shootSpeed.X * Main.rand.NextFloat(2.5f, 4.5f), shootSpeed.Y * Main.rand.NextFloat(2.5f, 4.5f)), projectileType, (int)(DamageNumbers["FlamethrowerDamage"] * damageModifier), 0f, Main.myPlayer, 0);
                        shot1.timeLeft = 70;
                    }
                    if (Phase == 6)
                    {
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(shootSpeed.X * Main.rand.NextFloat(2.5f, 4.5f), shootSpeed.Y * Main.rand.NextFloat(2.5f, 4.5f)), projectileType, (int)(DamageNumbers["FlamethrowerDamage"] * damageModifier), 0f, Main.myPlayer, 1);
                        shot1.timeLeft = 70;
                    }
                }

                //play breath sound
                if (Main.rand.NextBool(3))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 1.2f, PitchVariance = 0.2f }, NPC.Center); //flame thrower sound
                }
            }
        }

        public void CreateClones() //Creates two clones at the end of the move near the player
        {
            if (isClone || (NPC.CountNPCS(ModContent.NPCType<Pinwheel>()) >= 5 && MoveTimer < 3))
            {
                justSkippedMove = true;
                NextMove();
            }
            NPC.noGravity = true;
            NPC.velocity = Vector2.Zero;

            if (MoveTimer == 0)
            {
                cloneTimer = 0;
                cloneOffset = 0;
            }
            if (MoveTimer == 1) //A sad attempt at fixing weirdly drawn clones. It worked I think, why.
            {
                cloneTimer = 0;
                cloneOffset = 0;
            }
            if (MoveTimer <= 120 && opacityTimer < 30)
            {
                opacityTimer++;
            }

            if (MoveTimer >= 120)
            {
                NPC.dontTakeDamage = true;
            }
            if (MoveTimer == 220) //Spawn clones at random spots around the player and teleport main boss away
            {
                //Server-only: it owns the clone spawns, and the warp reaches clients through the synced position + TeleportTelegraph.
                //Spots are picked now rather than at MoveTimer 0, so they're checked against where the player stands after the 220-tick wind-up.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    cloneSpawnLocation1 = GenerateTeleportPosition(NPC, 50, true);
                    cloneSpawnLocation2 = GenerateTeleportPosition(NPC, 50, true);
                    QueueTeleport(NPC, 50, true, 200);

                    if (cloneSpawnLocation1.HasValue)
                    {
                        int cloneLeft = NPC.NewNPC(NPC.GetSource_FromThis(), (int)cloneSpawnLocation1.Value.X, (int)cloneSpawnLocation1.Value.Y + 70, ModContent.NPCType<Pinwheel>(), 0, 0, 0, Phase, 1);
                        Main.npc[cloneLeft].boss = false;
                    }
                    if (cloneSpawnLocation2.HasValue)
                    {
                        int cloneRight = NPC.NewNPC(NPC.GetSource_FromThis(), (int)cloneSpawnLocation2.Value.X, (int)cloneSpawnLocation2.Value.Y + 70, ModContent.NPCType<Pinwheel>(), 0, 0, 0, Phase, 1);
                        Main.npc[cloneRight].boss = false;
                    }

                    ExecuteQueuedTeleport(NPC);
                    NPC.netUpdate = true;
                }

                justTeleported = true;
                justSkippedMove = true; //This makes it so that the boss and new clones all attack at the same time once the move ends, except when they're skipping using BlindingPulse in first phase
                moveCount = 0;
            }
        }

        //These allow us to alternate between which side each shot ruptures toward
        int rightSideShotDelay = 30;
        int leftSideShotDelay = 30;

        public void VolcanicEruption()
        {
            NPC.velocity *= 0f;

            if (Phase < 6)
            {
                if (MoveTimer > 120 && MoveTimer < 250 && MoveTimer % rightSideShotDelay == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X - 5, NPC.Center.Y - 140), new Vector2(Main.rand.NextFloat(0.5f, 3f), Main.rand.NextFloat(-5.5f, -1.5f)), ModContent.ProjectileType<PinwheelBallOfFire>(), (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
                    shot1.timeLeft = 360;
                    if (Phase == 1) shot1.timeLeft = 480;
                }

                if (MoveTimer > 120 && MoveTimer < 250 && (MoveTimer - 15) % leftSideShotDelay == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X - 5, NPC.Center.Y - 140), new Vector2(Main.rand.NextFloat(-3f, -0.5f), Main.rand.NextFloat(-5.5f, -1.5f)), ModContent.ProjectileType<PinwheelBallOfFire>(), (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
                    shot1.timeLeft = 360;
                    if (Phase >= 1) shot1.timeLeft = 480;
                }
            }

            if (Phase == 6 && MoveTimer > 120 && MoveTimer < 250)
            {
                if (MoveTimer % rightSideShotDelay == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(Main.rand.NextFloat(NPC.Center.X, NPC.Center.X + 120), NPC.Bottom.Y), new Vector2(0, Main.rand.NextFloat(-3.5f, -1.5f)), ModContent.ProjectileType<EnemySpellSuddenDeathStrike>(), (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
                    }
                }

                if ((MoveTimer - 15) % leftSideShotDelay == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(Main.rand.NextFloat(NPC.Center.X - 120, NPC.Center.X), NPC.Bottom.Y), new Vector2(0, Main.rand.NextFloat(-3.5f, -1.5f)), ModContent.ProjectileType<EnemySpellSuddenDeathStrike>(), (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
                    }
                }

                if (MoveTimer == 220)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie81 with { Volume = 0.8f, Pitch = -1f, PitchVariance = 1f, MaxInstances = 5 }, NPC.Center); //wraith
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X, NPC.Center.Y - 30), new Vector2(0, Main.rand.NextFloat(0, -1f)), ModContent.ProjectileType<EnemyBlackKnightHomingCrystal>(), (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
                    }
                }
            }
        }

        public void BlindingPulse() //Raises from the ground and sends out a shockwave which inflicts Darkness and Chilled, and upgrades those to their stronger counterparts if hit again with previous debuffs active.
        {
            if (Phase == 0) //Only use after phase "0" onwards
            {
                justSkippedMove = true;
                NextMove();
            }

            NPC.velocity *= 0f;

            if (MoveTimer < 100)
            {
                NPC.velocity.Y = -1f;
            }
            if (MoveTimer >= 100 && MoveTimer < 140)
            {
                NPC.velocity.Y = 0f;
                NPC.noGravity = true;
            }

            if (MoveTimer == 120)
            {
                if (Phase < 6)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 5 }, NPC.Center);
                    
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 15);
                            position += NPC.Center;
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);

                            Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 3f, ModContent.ProjectileType<BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier), 0f, Main.myPlayer);
                            shot1.timeLeft = 90;
                        }
                    }
                }

                if (Phase == 6) //In final phase, create larger faster ring with demon sickles
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 5 }, NPC.Center);
                    
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 60; i++)
                        {
                            Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 30f);
                            position += NPC.Center;
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);
                            Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 8f, ModContent.ProjectileType<BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier), 0f, Main.myPlayer);
                            shot1.timeLeft = 105;
                        }
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 10);
                            position += NPC.Center;
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 0.1f);
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity, ProjectileID.DemonSickle, (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier), 0f, Main.myPlayer);
                        }
                    }
                }
            }

        }

        #endregion


        public override void HandleIntro()
        {
            NPC.noGravity = true;
            NPC.velocity = Vector2.Zero;
            if (isClone)
            {
                UsefulFunctions.SmoothHoming(NPC, Target.Center, 0.01f, 0.15f);
                NPC.noGravity = false;
            }

            //Clones skip the opening CreateClones (move 0) and start on the queued random move instead.
            //The real boss keeps move 0, so it opens the fight by splitting.
            if (introTimer == 30 && isClone)
            {
                NextMove();
            }

            if (introTimer >= introDuration)
            {
                attackTransitionTimeRemaining = 0;
            }
        }

        public override void HandleLife()
        {
            base.HandleLife();
        }

        public override void HandleDeath()
        {
            NPC.velocity = Vector2.Zero;
            MoveTimer = 0;
            attackTransitionTimeRemaining = 0;

            NPC.dontTakeDamage = true;
            if (deathAnimationProgress == deathAnimationDuration && isClone)
            {
                NPC.life = 0;
            }
            else
            {
                if (Phase == 0 && !isClone)
                {
                    opacity = 1;
                    opacityTimer = 30;
                    justTeleported = false;
                    deathAnimationProgress = 0;
                    NextPhase();
                }
                else
                {
                    opacity = 1;
                    opacityTimer = 30;
                    justTeleported = false;

                    if (deathAnimationProgress % 12 == 0 && deathAnimationProgress > 150 && !isClone)
                    {                        
                        Vector2 explosionLocation = Main.rand.NextVector2Circular(100, 100);
                        for (int i = 0; i < 40; i++) //Shadowflame
                        {
                            Dust.NewDustDirect(NPC.Center + explosionLocation - new Vector2(-4, -4), 8, 8, DustID.Shadowflame, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3), 50, default(Color), Main.rand.NextFloat(1f, 4f));
                            Dust dust = Dust.NewDustDirect(NPC.Center + explosionLocation - new Vector2(-4, -4), 8, 8, DustID.ShadowbeamStaff, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3), 50, default(Color), Main.rand.NextFloat(1f, 4f));
                            dust.velocity *= 2;
                        }

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { MaxInstances = 3 }, NPC.Center); 

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + explosionLocation, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<ShockwaveEffect>(), 0, 0, Main.myPlayer, 100, 30);
                        }
                    }

                    //One copy per player, dropped instanced: in MP each player sees and picks up only their own copy.
                    //These used to be loose Item.NewItem drops (one per player), so whoever got there first took everyone's.
                    if (deathAnimationProgress == deathAnimationDuration && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 dropPosition = NPC.Center + new Vector2(-18, 24);
                        List<int> drops = new List<int>();

                        if (Main.expertMode || Main.masterMode)
                        {
                            drops.Add(ModContent.ItemType<PinwheelBag>());
                        }
                        else
                        {
                            drops.Add(ModContent.ItemType<WingedSpear>());
                            drops.Add(ModContent.ItemType<WandOfFire2>());
                            drops.Add(ModContent.ItemType<MaskOfTheChild>());
                            drops.Add(ModContent.ItemType<MaskOfTheFather>());
                            drops.Add(ModContent.ItemType<MaskOfTheMother>());
                            drops.Add(ModContent.ItemType<EstusRing>());
                        }

                        //interactionRequired false: players who only fought the clones never "interacted" with this NPC.
                        //DropItemInstanced also zeroes NPC.value (vanilla puts the coins in the bag), and the Dark Souls drop reads it.
                        float soulValue = NPC.value;

                        foreach (int dropType in drops)
                        {
                            NPC.DropItemInstanced(dropPosition, Vector2.One, dropType, 1, false);
                        }

                        NPC.value = soulValue;
                    }
                    base.HandleDeath();
                }
            }
            if (Phase == 6 && !isClone)
            {
                //attackTransitionTimeRemaining = 300;
                justTeleported = true;
            }
        }

        

        public override void AttackTransition()
        {
            if (introTimer > introDuration)
            {
                if (Phase == 6) UsefulFunctions.SmoothHoming(NPC, Target.Center, 0.01f, 0.3f);
                else UsefulFunctions.SmoothHoming(NPC, Target.Center, 0.01f, 0.15f);
            }
            else
            {
                NPC.noGravity = true;
                NPC.velocity = Vector2.Zero;
            }
            NPC.noGravity = false;

            if (attackTransitionTimeRemaining == attackTransitionDuration)
            {
                QueueTeleport(NPC, 50, true, 200);
            }

            //Server decides the warp; clients get the new position, moveCount, movesBeforeTeleport and justTeleported from the sync.
            //Clients used to run this too, with their own Main.rand roll for movesBeforeTeleport, so they warped on different attacks.
            if (attackTransitionTimeRemaining <= 1 && introFinished && moveCount >= movesBeforeTeleport && NPC.life > 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ExecuteQueuedTeleport(NPC);
                moveCount = 0;
                justTeleported = true;
                NPC.netUpdate = true;
            }
        }

        public override void PhaseTransition()
        {
            if (Phase == 1)
            {
                deathAnimationProgress = 0;
                NPC.noGravity = true;
                NPC.dontTakeDamage = true;
                MoveIndex = PinwheelAttackID.CreateClonesID; //Force next attack to be CreateClones

                if (NPC.life <= 0)
                {
                    NPC.life = 1;
                }

                if (phaseTransitionTimeRemaining > phaseTransitionDuration - 90)
                {
                    NPC.velocity = Vector2.Zero;
                    opacityTimer = 30; //This keeps boss visible
                    opacity = 1;
                }

                if (phaseTransitionTimeRemaining == phaseTransitionDuration - 110) //Teleport "dead" Pinwheel to arena centre floor
                {
                    //Only when this is the adventure map's arena: on any other world (or summoned elsewhere with the Rematch Tome)
                    //that tile is arbitrary terrain, so it stays put. 1600px = 100 tiles, generous for anywhere inside the arena.
                    Vector2 arenaCentre = ExpandedWorldTransform.MapWorld(new Vector2(4139f, 933f) * 16);
                    bool inAdventureArena = tsorcRevampWorld.CustomMap && !tsorcRevampWorld.RemixMap && NPC.Distance(arenaCentre) < 1600f;

                    if (inAdventureArena)
                    {
                        NPC.Center = arenaCentre;
                    }

                    justTeleported = true;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                    }
                }

                if (phaseTransitionTimeRemaining == phaseTransitionDuration - 240 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //Instanced so each player gets their own ring (was one loose drop per player, first come first served).
                    //Same NPC.value restore and no-interaction rule as the death drops in HandleDeath.
                    float soulValue = NPC.value;
                    NPC.DropItemInstanced(NPC.Center + new Vector2(-18, 24), Vector2.One, ModContent.ItemType<RingOfFavorAndProtection>(), 1, false);
                    NPC.value = soulValue;
                }

                if (phaseTransitionTimeRemaining < phaseTransitionDuration - 300 && phaseTransitionTimeRemaining > phaseTransitionDuration - 1000)
                {
                    NPC.lifeMax = (int)(5000 * HealthScale * EnemyBalance.PhaseLifeMultiplier(NPC.type, 2));

                    if (NPC.life < NPC.lifeMax)
                    {
                        NPC.life += NPC.lifeMax / 250;
                    }
                    if (NPC.life > NPC.lifeMax)
                    {
                        NPC.life = (int)(5000 * HealthScale * EnemyBalance.PhaseLifeMultiplier(NPC.type, 2));
                    }
                }

                if (phaseTransitionTimeRemaining < phaseTransitionDuration - 510 && phaseTransitionTimeRemaining > phaseTransitionDuration - 720)
                {
                    NPC.velocity.Y = -1f;
                }

                if (phaseTransitionTimeRemaining == phaseTransitionDuration - 720)
                {
                    NPC.velocity.Y = 0f;
                }

                if (phaseTransitionTimeRemaining == phaseTransitionDuration - 720)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1.1f, PitchVariance = 0.2f, MaxInstances = 5 }, NPC.Center);

                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 30f);
                        position += NPC.Center;
                        Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 8f, ModContent.ProjectileType<BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier) + 6, 0f, Main.myPlayer);
                            shot1.timeLeft = 105;
                        }
                    }
                }

                if (phaseTransitionTimeRemaining == phaseTransitionDuration - 800)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1.1f, Pitch = -0.2f, MaxInstances = 5 }, NPC.Center);

                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 30f);
                        position += NPC.Center;
                        Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 8f, ModContent.ProjectileType<BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier) + 6, 0f, Main.myPlayer);
                            shot1.timeLeft = 105;
                        }
                    }
                }

                if (phaseTransitionTimeRemaining == phaseTransitionDuration - 920)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1f, Pitch = -0.4f, MaxInstances = 5 }, NPC.Center);

                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 30f);
                        position += NPC.Center;
                        Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 12f, ModContent.ProjectileType<BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier) + 10, 0f, Main.myPlayer);
                            shot1.timeLeft = 80;
                        }
                    }
                }
            }

            if (Phase == 6)
            {
                MoveTimer = 0;
                attackTransitionTimeRemaining = 0;
                NPC.velocity = Vector2.Zero;
                NPC.noGravity = true;
                NPC.dontTakeDamage = true;
                opacityTimer = 30; //This keeps boss visible
                opacity = 1;
            }

            base.PhaseTransition();
        }

        #region Drawing & Animation


        int dustQuantityTimer;


        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false; //Don't draw hp bar, as there will be clones and we don't want it to be glaringly obvious which is the boss
        }


        float cloneOffset = 8;
        float cloneTimer = 0;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            #region Dusts


            Vector2 lanternBottomLeft = new Vector2(NPC.position.X - 55, NPC.position.Y + 88);
            Vector2 lanternMiddleLeft = new Vector2(NPC.position.X - 58, NPC.position.Y + 16);
            Vector2 lanternTopLeft = new Vector2(NPC.position.X - 18, NPC.position.Y - 40);
            Vector2 lanternTopRight = new Vector2(NPC.position.X + 68, NPC.position.Y - 40);
            Vector2 lanternMiddleRight = new Vector2(NPC.position.X + 106, NPC.position.Y + 16);
            Vector2 lanternBottomRight = new Vector2(NPC.position.X + 105, NPC.position.Y + 88);


            #region Bouncing Fireballs Attack Dusts


            if (MoveIndex == PinwheelAttackID.BouncingFireballID && NPC.frame.Y == 16 * 234) //If arms up, on frame 16 (attacking frame, etc). 234 is frameheight
            {
                if (Main.rand.NextBool(10) && Phase < 6) //Fire
                {
                    if (Phase < 4) Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 2) Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 6) Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 6) Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 5) Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 3) Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(10) && Phase == 6)
                {
                    Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                }
            }

            #endregion


            #region Killable Fireballs Attack Dusts


            if (MoveIndex == PinwheelAttackID.KillableFireballID && MoveTimer > 20) //If arms up, on frame 16 (attacking frame, etc). 234 is frameheight
            {
                if (Main.rand.NextBool(4) && Phase < 6) //Fire
                {
                    if (Phase < 4) Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 2) Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 6) Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 6) Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 5) Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    if (Phase < 3) Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(6) && Phase == 6)
                {
                    Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                }
            }

            #endregion


            #region Flamethrower Attack Dusts

            if (MoveIndex == PinwheelAttackID.FlamethrowerID && MoveTimer >= 20 && MoveTimer < 420)
            {
                if (Phase < 6)
                {
                    if (Main.rand.NextBool(6)) //Fire
                    {
                        if (Phase < 4) Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 2) Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 6) Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 6) Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 5) Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 3) Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    }

                    if (MoveTimer == 1) dustQuantityTimer = 0;

                    if (dustQuantityTimer < 120 && MoveTimer < 300)
                    {
                        dustQuantityTimer++;
                    }

                    if (MoveTimer >= 300)
                    {
                        dustQuantityTimer--;
                    }

                    int dustQuantity = (int)(dustQuantityTimer * 20f / 1000f);
                    float flameSpeedPower = (int)(dustQuantityTimer * 60f / 1000f);


                    if (dustQuantity < 1 && Main.rand.NextBool(2)) //Flare
                    {
                        if (Phase < 4)
                        {
                            Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 40f, -20f, 50, default(Color), 2f);
                            dust1.noGravity = true;
                            dust1.velocity /= 8;
                            dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 2)
                        {
                            Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 47f, 13f, 50, default(Color), 2f);
                            dust2.noGravity = true;
                            dust2.velocity /= 8;
                            dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 6)
                        {
                            Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, 40f, 50, default(Color), 2f);
                            dust3.noGravity = true;
                            dust3.velocity /= 8;
                            dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 6)
                        {
                            Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, 40f, 50, default(Color), 2f);
                            dust4.noGravity = true;
                            dust4.velocity /= 8;
                            dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 5)
                        {
                            Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -47f, 13f, 50, default(Color), 2f);
                            dust5.noGravity = true;
                            dust5.velocity /= 8;
                            dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 3)
                        {
                            Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -40, -20, 50, default(Color), 2f);
                            dust6.noGravity = true;
                            dust6.velocity /= 8;
                            dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                    }

                    else
                    {
                        for (int i = 0; i < dustQuantity; i++) //Flare
                        {
                            if (Phase < 4)
                            {
                                Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 40f, -20f, 50, default(Color), 2f);
                                dust1.noGravity = true;
                                dust1.velocity /= 8;
                                dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 2)
                            {
                                Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 47f, 13f, 50, default(Color), 2f);
                                dust2.noGravity = true;
                                dust2.velocity /= 8;
                                dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 6)
                            {
                                Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, 40f, 50, default(Color), 2f);
                                dust3.noGravity = true;
                                dust3.velocity /= 8;
                                dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 6)
                            {
                                Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, 40f, 50, default(Color), 2f);
                                dust4.noGravity = true;
                                dust4.velocity /= 8;
                                dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 5)
                            {
                                Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -47f, 13f, 50, default(Color), 2f);
                                dust5.noGravity = true;
                                dust5.velocity /= 8;
                                dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 3)
                            {
                                Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -40, -20, 50, default(Color), 2f);
                                dust6.noGravity = true;
                                dust6.velocity /= 8;
                                dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                        }
                    }
                }

                if (Phase == 6)
                {
                    if (Main.rand.NextBool(6)) //Shadowbeam
                    {
                        Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                        Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                        Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                        Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                        Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                        Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), 1f);
                    }

                    if (MoveTimer == 1) dustQuantityTimer = 0;

                    if (dustQuantityTimer < 120 && MoveTimer < 300)
                    {
                        dustQuantityTimer++;
                    }

                    if (MoveTimer >= 300)
                    {
                        dustQuantityTimer--;
                    }

                    int dustQuantity = (int)(dustQuantityTimer * 20f / 1000f);
                    float flameSpeedPower = (int)(dustQuantityTimer * 60f / 1000f);


                    if (dustQuantity < 1 && Main.rand.NextBool(2)) //Shadowflame
                    {

                        Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, DustID.Shadowflame, 40f, -20f, 50, default(Color), 1.5f);
                        dust1.noGravity = true;
                        dust1.velocity /= 20;
                        dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, DustID.Shadowflame, 47f, 13f, 50, default(Color), 1.5f);
                        dust2.noGravity = true;
                        dust2.velocity /= 20;
                        dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, DustID.Shadowflame, 20f, 40f, 50, default(Color), 1.5f);
                        dust3.noGravity = true;
                        dust3.velocity /= 20;
                        dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, DustID.Shadowflame, -20f, 40f, 50, default(Color), 1.5f);
                        dust4.noGravity = true;
                        dust4.velocity /= 20;
                        dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, DustID.Shadowflame, -47f, 13f, 50, default(Color), 1.5f);
                        dust5.noGravity = true;
                        dust5.velocity /= 20;
                        dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, DustID.Shadowflame, -40, -20, 50, default(Color), 1.5f);
                        dust6.noGravity = true;
                        dust6.velocity /= 20;
                        dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                    }

                    else
                    {
                        for (int i = 0; i < dustQuantity; i++) //Shadowflame
                        {

                            Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, DustID.Shadowflame, 40f, -20f, 50, default(Color), 1.5f);
                            dust1.noGravity = true;
                            dust1.velocity /= 20;
                            dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                            Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, DustID.Shadowflame, 47f, 13f, 50, default(Color), 1.5f);
                            dust2.noGravity = true;
                            dust2.velocity /= 20;
                            dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                            Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, DustID.Shadowflame, 20f, 40f, 50, default(Color), 1.5f);
                            dust3.noGravity = true;
                            dust3.velocity /= 20;
                            dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                            Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, DustID.Shadowflame, -20f, 40f, 50, default(Color), 1.5f);
                            dust4.noGravity = true;
                            dust4.velocity /= 20;
                            dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                            Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, DustID.Shadowflame, -47f, 13f, 50, default(Color), 1.5f);
                            dust5.noGravity = true;
                            dust5.velocity /= 20;
                            dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                            Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, DustID.Shadowflame, -40, -20, 50, default(Color), 1.5f);
                            dust6.noGravity = true;
                            dust6.velocity /= 20;
                            dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        }
                    }
                }
            }

            #endregion


            #region Volcanic Eruption Attack Dusts


            if (MoveIndex == PinwheelAttackID.VolcanicEruptionID && MoveTimer >= 20 && MoveTimer < 370)
            {
                if (Phase < 6)
                {
                    if (Main.rand.NextBool(10)) //Fire
                    {
                        if (Phase < 4) Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 2) Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 6) Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 6) Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 5) Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                        if (Phase < 3) Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    }

                    if (dustQuantityTimer < 120 && MoveTimer < 270)
                    {
                        dustQuantityTimer++;
                    }

                    if (MoveTimer >= 270)
                    {
                        dustQuantityTimer--;
                    }

                    int dustQuantity = (int)(dustQuantityTimer * 20f / 1000f);
                    float flameSpeedPower = (int)(dustQuantityTimer * 40f / 1000f);

                    if (dustQuantity < 1 && Main.rand.NextBool(2)) //Flare
                    {
                        if (Phase < 4)
                        {
                            Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 4f, -45f, 50, default(Color), 2f);
                            dust1.noGravity = true;
                            dust1.velocity /= 10;
                            dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 2)
                        {
                            Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 27f, -45f, 50, default(Color), 2f);
                            dust2.noGravity = true;
                            dust2.velocity /= 10;
                            dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 6)
                        {
                            Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, -20f, 50, default(Color), 2f);
                            dust3.noGravity = true;
                            dust3.velocity /= 10;
                            dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 6)
                        {
                            Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, -20f, 50, default(Color), 2f);
                            dust4.noGravity = true;
                            dust4.velocity /= 10;
                            dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 5)
                        {
                            Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -27f, -45f, 50, default(Color), 2f);
                            dust5.noGravity = true;
                            dust5.velocity /= 10;
                            dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Phase < 3)
                        {
                            Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -4, -45, 50, default(Color), 2f);
                            dust6.noGravity = true;
                            dust6.velocity /= 10;
                            dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                    }

                    else
                    {
                        for (int i = 0; i < dustQuantity; i++) //Flare
                        {
                            if (Phase < 4)
                            {
                                Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 4f, -45f, 50, default(Color), 2f);
                                dust1.noGravity = true;
                                dust1.velocity /= 10;
                                dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 2)
                            {
                                Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 27f, -45f, 50, default(Color), 2f);
                                dust2.noGravity = true;
                                dust2.velocity /= 10;
                                dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 6)
                            {
                                Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, -20f, 50, default(Color), 2f);
                                dust3.noGravity = true;
                                dust3.velocity /= 10;
                                dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 6)
                            {
                                Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, -20f, 50, default(Color), 2f);
                                dust4.noGravity = true;
                                dust4.velocity /= 10;
                                dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 5)
                            {
                                Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -27f, -45f, 50, default(Color), 2f);
                                dust5.noGravity = true;
                                dust5.velocity /= 10;
                                dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                            if (Phase < 3)
                            {
                                Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -4, -45, 50, default(Color), 2f);
                                dust6.noGravity = true;
                                dust6.velocity /= 10;
                                dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                            }
                        }
                    }
                    if (dustQuantity == 2 && Main.rand.NextBool(2) && Phase < 6)
                    {
                        Dust dust = Dust.NewDustDirect(new Vector2(NPC.Center.X - 20, NPC.Center.Y - 140), 40, 10, 127, 0, -25, 50, default(Color), 2f);
                        dust.noGravity = true;
                        dust.velocity /= 10;
                        dust.velocity *= Main.rand.NextFloat(5, 10);
                    }
                }

                if (Phase == 6)
                {

                    if (Main.rand.NextBool(10)) //Gold
                    {
                        Dust dust2 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                        dust2.velocity *= 0.2f;
                        dust2.noGravity = false;
                    }

                    if (Main.rand.NextBool(10)) //Gold
                    {
                        Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                        dust2.velocity *= 0.2f;
                        dust2.noGravity = false;
                    }

                    if (Main.rand.NextBool(10)) //Gold
                    {
                        Dust dust2 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                        dust2.velocity *= 0.2f;
                        dust2.noGravity = false;
                    }

                    if (Main.rand.NextBool(10)) //Gold
                    {
                        Dust dust2 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                        dust2.velocity *= 0.2f;
                        dust2.noGravity = false;
                    }

                    if (Main.rand.NextBool(10)) //Gold
                    {
                        Dust dust2 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                        dust2.velocity *= 0.2f;
                        dust2.noGravity = false;
                    }

                    if (Main.rand.NextBool(10)) //Gold
                    {
                        Dust dust2 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                        dust2.velocity *= 0.2f;
                        dust2.noGravity = false;
                    }

                    if (MoveTimer >= 20) //Ground dusts
                    {
                        Dust dust = Dust.NewDustDirect(new Vector2(NPC.Center.X - 120, NPC.Bottom.Y), 240, 10, 57, 0, -1, 50, default(Color), 1.2f);
                        dust.noGravity = true;
                        dust.velocity /= 30;
                        dust.velocity *= Main.rand.NextFloat(5, 10);
                    }
                    if (MoveTimer >= 120) //Ground rising dusts
                    {
                        Dust dust = Dust.NewDustDirect(new Vector2(NPC.Center.X - 120, NPC.Bottom.Y), 240, 10, 57, 0, -10, 50, default(Color), 1.2f);
                        dust.noGravity = true;
                        dust.velocity /= 30;
                        dust.velocity *= Main.rand.NextFloat(5, 10);
                    }
                    if (MoveTimer >= 20) //Ground dusts
                    {
                        Dust dust = Dust.NewDustDirect(new Vector2(NPC.Center.X - 120, NPC.Bottom.Y), 240, 10, 57, 0, -1, 50, default(Color), 1.2f);
                        dust.noGravity = true;
                        dust.velocity /= 30;
                        dust.velocity *= Main.rand.NextFloat(5, 10);
                    }
                    if (MoveTimer > 20 && MoveTimer <= 80 && Main.rand.NextBool(6))
                    {
                        int z = Dust.NewDust(new Vector2(NPC.Center.X, NPC.Center.Y - 30), 30, 30, 57, 0f, 0f, 120, default, 1f);
                        Main.dust[z].noGravity = true;
                        Main.dust[z].velocity *= 2.75f;
                        Main.dust[z].fadeIn = 1.3f;
                        Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        vectorother.Normalize();
                        vectorother *= (float)Main.rand.Next(50, 100) * 0.035f;
                        Main.dust[z].velocity = vectorother;
                        vectorother.Normalize();
                        vectorother *= 45f;
                        Main.dust[z].position = new Vector2(NPC.Center.X, NPC.Center.Y - 30) - vectorother;
                    }
                    if (MoveTimer > 80 && MoveTimer <= 140 && Main.rand.NextBool(3))
                    {
                        int z = Dust.NewDust(new Vector2(NPC.Center.X, NPC.Center.Y - 30), 30, 30, 57, 0f, 0f, 120, default, 1f);
                        Main.dust[z].noGravity = true;
                        Main.dust[z].velocity *= 2.75f;
                        Main.dust[z].fadeIn = 1.3f;
                        Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        vectorother.Normalize();
                        vectorother *= (float)Main.rand.Next(50, 100) * 0.045f;
                        Main.dust[z].velocity = vectorother;
                        vectorother.Normalize();
                        vectorother *= 45f;
                        Main.dust[z].position = new Vector2(NPC.Center.X, NPC.Center.Y - 30) - vectorother;
                    }
                    if (MoveTimer > 140 && MoveTimer < 210)
                    {
                        int z = Dust.NewDust(new Vector2(NPC.Center.X, NPC.Center.Y - 30), 30, 30, 57, 0f, 0f, 120, default, 1f);
                        Main.dust[z].noGravity = true;
                        Main.dust[z].velocity *= 2.75f;
                        Main.dust[z].fadeIn = 1.3f;
                        Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                        vectorother.Normalize();
                        vectorother *= (float)Main.rand.Next(50, 100) * 0.055f;
                        Main.dust[z].velocity = vectorother;
                        vectorother.Normalize();
                        vectorother *= 45f;
                        Main.dust[z].position = new Vector2(NPC.Center.X, NPC.Center.Y - 30) - vectorother;
                    }
                    if (MoveTimer > 210 && MoveTimer < 220)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int z = Dust.NewDust(new Vector2(NPC.Center.X, NPC.Center.Y - 30), 30, 30, 57, 0f, 0f, 120, default, 1f);
                            Main.dust[z].noGravity = true;
                            Main.dust[z].velocity *= 2.75f;
                            Main.dust[z].fadeIn = 1.3f;
                            Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                            vectorother.Normalize();
                            vectorother *= (float)Main.rand.Next(50, 100) * 0.06f;
                            Main.dust[z].velocity = vectorother;
                            vectorother.Normalize();
                            vectorother *= 45f;
                            Main.dust[z].position = new Vector2(NPC.Center.X, NPC.Center.Y - 30) - vectorother;
                        }
                    }
                    if (MoveTimer == 220)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Dust dust = Dust.NewDustDirect(new Vector2(NPC.Center.X, NPC.Center.Y - 30), 16, 22, 57, Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1), 120, default, 1f);
                            dust.velocity *= 2;
                        }
                    }
                }
            }

            #endregion


            #region Create Clones Move Dusts


            if (MoveIndex == PinwheelAttackID.CreateClonesID && MoveTimer >= 20 && MoveTimer < 120)
            {
                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }
            }

            if (MoveIndex == PinwheelAttackID.CreateClonesID && MoveTimer == 120)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust1.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust1.noGravity = false;

                    Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust2.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust2.noGravity = false;

                    Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust3.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust3.noGravity = false;

                    Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust4.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust4.noGravity = false;

                    Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust5.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust5.noGravity = false;

                    Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f);
                    dust6.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust6.noGravity = false;
                }

                for (int i = 0; i < 30; i++)
                {
                    Dust dust1 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 2, NPC.position.Y), 4, NPC.height, 57, Main.rand.NextFloat(-4f, -3f), 0, 50, default(Color), 1f);
                    dust1.noGravity = false;

                    Dust dust2 = Dust.NewDustDirect(new Vector2(NPC.Center.X + 2, NPC.position.Y), 4, NPC.height, 57, Main.rand.NextFloat(3f, 4f), 0, 50, default(Color), 1f);
                    dust2.noGravity = false;
                }
            }

            #endregion


            #region Blinding Pulse Move Dusts


            if (MoveIndex == PinwheelAttackID.BlindingPulseID)
            {

                if (MoveTimer >= 1) //Floaty gold hover dusts
                {
                    Dust dust = Dust.NewDustDirect(new Vector2(NPC.Center.X - 60, NPC.Center.Y + 70), 120, 10, 57, 0, 10, 50, default(Color), 1.2f);
                    dust.noGravity = true;
                    dust.velocity /= 30;
                    dust.velocity *= Main.rand.NextFloat(5, 10);
                }

                if (MoveTimer >= 1 && MoveTimer < 120)
                {
                    float radius = 94;
                    int quantity = MoveTimer / 10 - 2;
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius, DustID.Shadowflame, quantity, 3);
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius + (radius / 10), DustID.Shadowflame, quantity, -10);
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y), 4, DustID.Shadowflame, 1, 0);
                }
                if (MoveTimer >= 120)
                {
                    if (Phase < 6)
                    {
                        float radius = (MoveTimer - 100) * 3.2f + 30f;
                        if (Phase == 6) { radius = (MoveTimer - 100) * 3.2f + 30f; }
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius, DustID.Shadowflame, 20, 3);
                    }
                    if (Phase == 6)
                    {
                        float radius = (MoveTimer - 120) * 8f + 94f;
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius, DustID.Shadowflame, 40, 3);
                    }
                }
            }

            #endregion


            #region Phase Transition

            if (Phase == 1 && phaseTransitionTimeRemaining > 0)
            {
                //Skull dusts
                if (phaseTransitionTimeRemaining < phaseTransitionDuration - 240 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 360 && Main.rand.NextBool(10))
                {
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y + 11), 4, DustID.Shadowflame, 1, 0);
                }
                if (phaseTransitionTimeRemaining < phaseTransitionDuration - 360 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 420 && Main.rand.NextBool(4))
                {
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y + 8), 4, DustID.Shadowflame, 1, 0);
                }
                if (phaseTransitionTimeRemaining < phaseTransitionDuration - 420 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 480 && Main.rand.NextBool(2))
                {
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y + 6), 4, DustID.Shadowflame, 1, 0);
                }
                if (phaseTransitionTimeRemaining < phaseTransitionDuration - 480 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 510)
                {
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y + 6), 4, DustID.Shadowflame, 1, 0);
                }

                //Other dusts
                if (phaseTransitionTimeRemaining < phaseTransitionDuration - 510 && phaseTransitionTimeRemaining > phaseTransitionDuration - 1000)
                {

                    Dust dust = Dust.NewDustDirect(new Vector2(NPC.Center.X - 60, NPC.Center.Y + 70), 120, 10, 57, 0, 10, 50, default(Color), 1.2f); //Gold hover dust
                    dust.noGravity = true;
                    dust.velocity /= 30;
                    dust.velocity *= Main.rand.NextFloat(5, 10);

                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y + 2), 4, DustID.Shadowflame, 1, 0); //Skull dust

                    //Big circle dusts
                    if (phaseTransitionTimeRemaining < phaseTransitionDuration - 510 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 570)
                    {
                        float radius = 20 * 3.2f + 30f;
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius, DustID.Shadowflame, 1, 3);
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius + (radius / 10), DustID.Shadowflame, 1, -10);
                    }
                    if (phaseTransitionTimeRemaining < phaseTransitionDuration - 570 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 630)
                    {
                        float radius = 20 * 3.2f + 30f;
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius, DustID.Shadowflame, 4, 3);
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius + (radius / 10), DustID.Shadowflame, 4, -10);
                    }
                    if (phaseTransitionTimeRemaining < phaseTransitionDuration - 630 && phaseTransitionTimeRemaining > phaseTransitionDuration - 920)
                    {
                        float radius = 20 * 3.2f + 30f;
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius, DustID.Shadowflame, 10, 3);
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius + (radius / 10), DustID.Shadowflame, 10, -15);
                    }
                    //Blinding wave 1
                    if (phaseTransitionTimeRemaining < phaseTransitionDuration - 720 && phaseTransitionTimeRemaining > phaseTransitionDuration - 825)
                    {
                        float radius = (phaseTransitionTimeRemaining - 280) * -8f + 94f;
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius, DustID.Shadowflame, 40, 3);
                    }
                    //Blinding wave 2
                    if (phaseTransitionTimeRemaining < phaseTransitionDuration - 800 && phaseTransitionTimeRemaining > phaseTransitionDuration - 905)
                    {
                        float radius = (phaseTransitionTimeRemaining - 200) * -8f + 94f;
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius, DustID.Shadowflame, 40, 3);
                    }
                    //Blinding wave 3
                    if (phaseTransitionTimeRemaining < phaseTransitionDuration - 920 && phaseTransitionTimeRemaining > phaseTransitionDuration - 1000)
                    {
                        float radius = (phaseTransitionTimeRemaining - 80) * -12f + 94f;
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius, DustID.Shadowflame, 50, 3);
                        UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), radius + 20, DustID.Shadowflame, 30, -15);

                    }
                }
            }

            if (Phase == 6)
            {
                if (phaseTransitionTimeRemaining == phaseTransitionDuration - 399) dustQuantityTimer = 0;

                if (phaseTransitionTimeRemaining < phaseTransitionDuration - 30 && phaseTransitionTimeRemaining > phaseTransitionDuration - 300)
                {
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y + 6), 4, DustID.Shadowflame, 1, 0);

                    if (dustQuantityTimer < 120 && phaseTransitionTimeRemaining > phaseTransitionDuration - 300)
                    {
                        dustQuantityTimer++;
                    }

                    if (phaseTransitionTimeRemaining < phaseTransitionDuration - 300)
                    {
                        dustQuantityTimer = 0;
                    }

                    int dustQuantity = (int)(dustQuantityTimer * 20f / 1000f);
                    float flameSpeedPower = (int)(dustQuantityTimer * 60f / 1000f);

                    if (/*dustQuantity < 1 && */Main.rand.NextBool(2)) //Shadowflame
                    {
                        if (Main.rand.NextBool(2))
                        {
                            Dust dust1 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, 60f, 0f, 50, default(Color), 1.5f);
                            dust1.noGravity = true;
                            dust1.velocity /= 25;
                            dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2))
                        {
                            Dust dust2 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, -60f, 0f, 50, default(Color), 1.5f);
                            dust2.noGravity = true;
                            dust2.velocity /= 25;
                            dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2))
                        {
                            Dust dust3 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, 34f, -26f, 50, default(Color), 1.5f);
                            dust3.noGravity = true;
                            dust3.velocity /= 25;
                            dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2))
                        {
                            Dust dust4 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, -34f, -26f, 50, default(Color), 1.5f);
                            dust4.noGravity = true;
                            dust4.velocity /= 25;
                            dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2))
                        {
                            Dust dust5 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, -15f, -45f, 50, default(Color), 1.5f);
                            dust5.noGravity = true;
                            dust5.velocity /= 20;
                            dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2))
                        {
                            Dust dust6 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, 15, -45, 50, default(Color), 1.5f);
                            dust6.noGravity = true;
                            dust6.velocity /= 20;
                            dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                    }
                }
                if (phaseTransitionTimeRemaining == phaseTransitionDuration - 300)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    for (int i = 0; i < 20; i++)
                    {
                        Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, DustID.ShadowbeamStaff, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), 1f);
                        dust1.velocity *= 3;
                        Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, DustID.ShadowbeamStaff, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), 1f);
                        dust2.velocity *= 3;
                        Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, DustID.ShadowbeamStaff, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), 1f);
                        dust3.velocity *= 3;
                        Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, DustID.ShadowbeamStaff, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), 1f);
                        dust4.velocity *= 3;
                        Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, DustID.ShadowbeamStaff, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), 1f);
                        dust5.velocity *= 3;
                        Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, DustID.ShadowbeamStaff, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), 1f);
                        dust6.velocity *= 3;
                    }
                }
            }
            #endregion

            if (deathAnimationProgress > 0 && Phase == 6 && !isClone)
            {
                if (Main.rand.NextBool(5))
                {
                    //Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.ShadowbeamStaff, 0, 0, 50, default(Color), Main.rand.NextFloat(1, 2));
                    Dust dust = Dust.NewDustDirect(new Vector2(NPC.Center.X - 6, NPC.Center.Y + 4), 8, 8, DustID.ShadowbeamStaff, Main.rand.NextFloat(5f, 15f), -10, 50, default(Color), Main.rand.NextFloat(1, 2f));
                    dust.velocity /= 10;
                    dust.velocity *= Main.rand.NextFloat(1f, 3f);
                }
            }

            else // Fire dusts while not attacking. Separate randomizations so the sparse dusts look natural
            {
                if (Main.rand.NextBool(60) && Phase < 4 && phaseTransitionTimeRemaining < 600 && opacity == 1 && NPC.life > 1)
                {
                    Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60) && Phase < 2 && phaseTransitionTimeRemaining < 600 && opacity == 1 && NPC.life > 1)
                {
                    Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60) && Phase < 6 && phaseTransitionTimeRemaining < 600 && opacity == 1 && NPC.life > 1)
                {
                    Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60) && Phase < 6 && phaseTransitionTimeRemaining < 600 && opacity == 1 && NPC.life > 1)
                {
                    Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60) && Phase < 5 && phaseTransitionTimeRemaining < 600 && opacity == 1 && NPC.life > 1)
                {
                    Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60) && Phase < 3 && phaseTransitionTimeRemaining < 600 && opacity == 1 && NPC.life > 1)
                {
                    Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }

                if (Phase == 6 && phaseTransitionTimeRemaining <= 100 && opacity == 1 && NPC.life > 1)
                {
                    if (Main.rand.NextBool(60))
                    {
                        Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, DustID.Shadowflame, 0, 1, 50, default(Color), 1f);
                    }
                    if (Main.rand.NextBool(60))
                    {
                        Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, DustID.Shadowflame, 0, 1, 50, default(Color), 1f);
                    }
                    if (Main.rand.NextBool(60))
                    {
                        Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, DustID.Shadowflame, 0, 1, 50, default(Color), 1f);
                    }
                    if (Main.rand.NextBool(60))
                    {
                        Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, DustID.Shadowflame, 0, 1, 50, default(Color), 1f);
                    }
                    if (Main.rand.NextBool(60))
                    {
                        Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, DustID.Shadowflame, 0, 1, 50, default(Color), 1f);
                    }
                    if (Main.rand.NextBool(60))
                    {
                        Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, DustID.Shadowflame, 0, 1, 50, default(Color), 1f);
                    }
                }
            }


            #endregion

            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.Pinwheel];
            Texture2D lanternMiddleLeftTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternMiddleLeft");
            Texture2D lanternBottomRightTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternBottomRight");
            Texture2D lanternBottomLeftTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternBottomLeft");
            Texture2D lanternMiddleRightTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternMiddleRight");
            Texture2D lanternTopLeftRightTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternTopLeftRight");
            Texture2D shadowflameTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_ShadowflameGlow");

            //Draw "clones"
            if (MoveIndex == PinwheelAttackID.CreateClonesID)
            {
                if (MoveTimer == 1)
                {
                    transparency = 0.5f;
                }
                if (MoveTimer > 28 && MoveTimer <= 120)
                {
                    cloneTimer++;
                    cloneOffset = cloneTimer / 15;
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 2) spriteBatch.Draw(lanternMiddleLeftTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 3) spriteBatch.Draw(lanternBottomRightTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 4) spriteBatch.Draw(lanternBottomLeftTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 5) spriteBatch.Draw(lanternMiddleRightTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 6) spriteBatch.Draw(lanternTopLeftRightTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase == 6) spriteBatch.Draw(shadowflameTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);

                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 2) spriteBatch.Draw(lanternMiddleLeftTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 3) spriteBatch.Draw(lanternBottomRightTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 4) spriteBatch.Draw(lanternBottomLeftTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 5) spriteBatch.Draw(lanternMiddleRightTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 6) spriteBatch.Draw(lanternTopLeftRightTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase == 6) spriteBatch.Draw(shadowflameTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                }
                if (MoveTimer > 120 && MoveTimer <= 190)
                {
                    cloneTimer += 2;
                    cloneOffset = cloneTimer - 108;
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 2) spriteBatch.Draw(lanternMiddleLeftTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 3) spriteBatch.Draw(lanternBottomRightTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 4) spriteBatch.Draw(lanternBottomLeftTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 5) spriteBatch.Draw(lanternMiddleRightTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 6) spriteBatch.Draw(lanternTopLeftRightTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase == 6) spriteBatch.Draw(shadowflameTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);

                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 2) spriteBatch.Draw(lanternMiddleLeftTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 3) spriteBatch.Draw(lanternBottomRightTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 4) spriteBatch.Draw(lanternBottomLeftTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 5) spriteBatch.Draw(lanternMiddleRightTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 6) spriteBatch.Draw(lanternTopLeftRightTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase == 6) spriteBatch.Draw(shadowflameTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                }
                if (MoveTimer > 190 && MoveTimer <= 220)
                {
                    transparency -= 0.016f;
                    cloneOffset = cloneTimer - 108;
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * transparency, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 2) spriteBatch.Draw(lanternMiddleLeftTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 3) spriteBatch.Draw(lanternBottomRightTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 4) spriteBatch.Draw(lanternBottomLeftTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 5) spriteBatch.Draw(lanternMiddleRightTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 6) spriteBatch.Draw(lanternTopLeftRightTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase == 6) spriteBatch.Draw(shadowflameTexture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);

                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * transparency, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 2) spriteBatch.Draw(lanternMiddleLeftTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 3) spriteBatch.Draw(lanternBottomRightTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 4) spriteBatch.Draw(lanternBottomLeftTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 5) spriteBatch.Draw(lanternMiddleRightTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase < 6) spriteBatch.Draw(lanternTopLeftRightTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    if (Phase == 6) spriteBatch.Draw(shadowflameTexture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                }
            }
            //Draw main boss texture (opacity is simulated in AI's Opacity region)
            Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition, new Rectangle(0, NPC.frame.Y, 198, 234), lightColor * (float)opacity, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);

            if (introTimer < 1) return true; //This has to be done so that Boss Rematch Tome has a sprite to display for pinwheel
            else return false;
        }


        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            //Glow mask for fire in lanterns
            double fireTextureOpacity = (double)opacityTimer / (double)30;
            if (introTimer < introDuration && !isClone) { fireTextureOpacity = (double)introTimer / (double)40; }
            if (introTimer < introDuration && isClone) { fireTextureOpacity = (double)introTimer / (double)30; }

            //Death opacity
            if (deathAnimationProgress > deathAnimationDuration / 2) { fireTextureOpacity = ((double)deathAnimationProgress / (double)90) * -1 + 2; }
            if (deathAnimationProgress > deathAnimationDuration / 2 && Phase == 6 && !isClone) { fireTextureOpacity = ((double)deathAnimationProgress / (double)150) * -1 + 2; }

            //"Death" during phase transition opacity
            if (phaseTransitionTimeRemaining > phaseTransitionDuration - 80) { fireTextureOpacity = ((double)deathAnimationProgress / (double)90) * -1 + 2; }

            Texture2D lanternMiddleLeft = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternMiddleLeft");
            Texture2D lanternBottomRight = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternBottomRight");
            Texture2D lanternBottomLeft = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternBottomLeft");
            Texture2D lanternMiddleRight = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternMiddleRight");
            Texture2D lanternTopLeftRight = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_LanternTopLeftRight");

            Texture2D shadowflameTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_ShadowflameGlow");

            if (Phase < 2) spriteBatch.Draw(lanternMiddleLeft, NPC.position - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(0, NPC.frame.Y, 198, 234), Color.White * (float)fireTextureOpacity, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
            if (Phase < 3) spriteBatch.Draw(lanternBottomRight, NPC.position - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(0, NPC.frame.Y, 198, 234), Color.White * (float)fireTextureOpacity, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
            if (Phase < 4) spriteBatch.Draw(lanternBottomLeft, NPC.position - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(0, NPC.frame.Y, 198, 234), Color.White * (float)fireTextureOpacity, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
            if (Phase < 5) spriteBatch.Draw(lanternMiddleRight, NPC.position - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(0, NPC.frame.Y, 198, 234), Color.White * (float)fireTextureOpacity, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
            if (Phase < 6) spriteBatch.Draw(lanternTopLeftRight, NPC.position - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(0, NPC.frame.Y, 198, 234), Color.White * (float)fireTextureOpacity, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
            if (Phase == 6 && phaseTransitionTimeRemaining <= 100) spriteBatch.Draw(shadowflameTexture, NPC.position - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(0, NPC.frame.Y, 198, 234), Color.White * (float)fireTextureOpacity, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
        }


        public override void FindFrame(int frameHeight)
        {
            if (introTimer < introDuration)
            {
                NPC.frame.Y = 0 * frameHeight;
                /*if (Math.Abs(NPC.velocity.X) > 0 || Math.Abs(NPC.velocity.Y) > 0)
                {
                    if (NPC.frameCounter < 352)
                    {
                        NPC.frame.Y = (int)(NPC.frameCounter / 22) * frameHeight;
                    }
                    else
                    {
                        NPC.frameCounter = 0;
                    }
                }*/


                //int frameTimer;
                /*NPC.frameCounter++;

                if (NPC.frameCounter < 40) //9, 16,  
                {
                    NPC.frame.Y = (int)((NPC.frameCounter / 8) + 17) * frameHeight; //under 64 will at most come up with 15 * frameheight, same as with the else if chain
                } 
                else 
                {
                    NPC.frameCounter = 0; //above 63 it resets the counter to 0 but it didn't do anything else in that tick, so frameY stays at 15 * frameheight I guess
                }
                /*
                switch (NPC.frameCounter) this is one alternative to the infintie else if chain: switch statement, better performance like this because it just has to choose one case and not loop through every if statement until it gets to the right one
                {
                    case double one when one < 4:
                        {
                            NPC.frame.Y = 0 * frameHeight;
                            break;
                        }
                    case double two when two >= 4 && two < 8:
                        {
                            NPC.frame.Y = 1 * frameHeight;
                            break;
                        }
                    case double three when three >= 8 && three < 12:
                        {
                            NPC.frame.Y = 2 * frameHeight;
                            break;
                        }
                    case double four when four >= 12 && four < 16:
                        {
                            NPC.frame.Y = 3 * frameHeight;
                            break;
                        }
                    case double five when five >= 16 && five < 20:
                        {
                            NPC.frame.Y = 4 * frameHeight;
                            break;
                        }
                    //and so on
                    default:
                        {
                            NPC.frameCounter = 0;
                            break;
                        }
                }*/
            }

            if (attackTransitionTimeRemaining < attackTransitionDuration && MoveTimer == 0 && introTimer >= introDuration && Math.Abs(NPC.velocity.X) > 0)
            {
                NPC.frameCounter++;

                if (NPC.frameCounter < 352)
                {
                    NPC.frame.Y = (int)(NPC.frameCounter / 22) * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }

            if ((MoveIndex == PinwheelAttackID.BouncingFireballID || MoveIndex == PinwheelAttackID.KillableFireballID) && MoveTimer != 0)
            {
                if (MoveTimer < 20)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (MoveTimer < 150)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
            }

            if (MoveIndex == PinwheelAttackID.FlamethrowerID && MoveTimer != 0 && MoveTimer <= 300)
            {
                //NPC.frameCounter++;

                if (MoveTimer < 20)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (MoveTimer < 300)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
            }

            if (MoveIndex == PinwheelAttackID.VolcanicEruptionID && MoveTimer != 0 && MoveTimer <= 280)
            {
                //NPC.frameCounter++;

                if (MoveTimer < 20)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (MoveTimer < 280)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
            }

            if ((MoveIndex == PinwheelAttackID.CreateClonesID || MoveIndex == PinwheelAttackID.BlindingPulseID) && MoveTimer != 0 && MoveTimer < 220)
            {
                if (MoveTimer > 0 && MoveTimer < 120)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
                else if (MoveTimer < 220)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
            }

            if (opacityTimer != 30 && attackTransitionTimeRemaining != 0)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0 * frameHeight;
            }

            if (deathAnimationProgress > 0) //If dying (clones)
            {
                if (deathAnimationProgress < 30)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
                else if (deathAnimationProgress < 40)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (deathAnimationProgress < 50)
                {
                    NPC.frame.Y = 21 * frameHeight;
                }
                else if (deathAnimationProgress <= 180)
                {
                    NPC.frame.Y = 22 * frameHeight;
                }
            }

            if (Phase == 1 && phaseTransitionTimeRemaining > 0) //Transition to second phase animation
            {
                if (phaseTransitionTimeRemaining > phaseTransitionDuration - 30)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 40)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 50)
                {
                    NPC.frame.Y = 21 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 360)
                {
                    NPC.frame.Y = 22 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 420)
                {
                    NPC.frame.Y = 21 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 510)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 720)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 760)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 800)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 840)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 920)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
                else if (phaseTransitionTimeRemaining > phaseTransitionDuration - 980)
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
            }

            if (Phase == 6 && phaseTransitionTimeRemaining > 0) //Transition to third (technically 6th) phase animation
            {
                if (phaseTransitionTimeRemaining > phaseTransitionDuration - 300 && NPC.life > 1)
                {
                    NPC.frameCounter++;
                    if (NPC.frameCounter < 6)
                    {
                        NPC.frame.Y = 17 * frameHeight;
                    }
                    else if (NPC.frameCounter < 12)
                    {
                        NPC.frame.Y = 18 * frameHeight;
                    }
                    else if (NPC.frameCounter < 18)
                    {
                        NPC.frame.Y = 19 * frameHeight;
                    }
                    else if (NPC.frameCounter < 24)
                    {
                        NPC.frame.Y = 20 * frameHeight;
                    }
                    else
                    {
                        NPC.frameCounter = 0;
                    }
                }
                else
                {
                    NPC.frame.Y = 17 * frameHeight;
                }
            }
        }
        #endregion


        public override void BossLoot(ref string name, ref int potionType)
        {
            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
            {
                potionType = ModContent.ItemType<LifegemItem>();
            }
            else { potionType = ItemID.HealingPotion; }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            /*if (NPC.lifeMax == (int)(5000 * HealthScale * EnemyBalance.PhaseLifeMultiplier(NPC.type, 2))) 
            {
                npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.PinwheelBag>())); //Bag is dropped as an item at the end of death anim instead, because checking for anything else seems to fail
            }*/

            //npcLoot.Add(notExpertCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
        }

        #region Modified Teleportation Functions


        public int DecideMovesBeforeTeleport(NPC npc)
        {
            int Moves = Main.rand.Next(2, 4); //Between 2 and 3

            if (isClone)
            {
                Moves = Main.rand.Next(1, 4); //Between 1 and 3
                if (npc.life < npc.lifeMax * 0.8f) { Moves = Main.rand.Next(1, 3); } //Between 1 and 2
            }
            else
            {
                if (npc.life < npc.lifeMax * 0.8f && npc.life >= npc.lifeMax / 2) { Moves = Main.rand.Next(1, 4); } //Between 1 and 3
                if (npc.life < npc.lifeMax / 2) { Moves = Main.rand.Next(1, 3); } //Between 1 and 2
            }

            /*int Moves = Main.rand.Next(2, 4); //Between 2 and 3
            if (npc.life < npc.lifeMax * 0.8f && npc.life >= npc.lifeMax / 2) { Moves = Main.rand.Next(1, 4); } //Between 1 and 3
            if (npc.life < npc.lifeMax / 2) { Moves = Main.rand.Next(1, 3); } //Between 1 and 2*/
            return Moves;

        }
        public static Vector2? GenerateTeleportPosition(NPC npc, int range, bool requireLineofSight = true)
        {
            //Do not teleport if the player is way way too far away (stops enemies following you home if you mirror away)
            if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
            { // far away from target; 2000 pixels = 125 blocks
                return null;
            }

            //Try 100 times at most
            for (int i = 0; i < 100; i++)
            {
                Vector2 teleportTarget = Vector2.Zero;

                //Pinwheel doesn't need as low of a minimum range as he doesn't deal contact damage and has 2 seconds wind up before any attack.

                teleportTarget.X = Main.rand.Next(2, range);
                if (Main.rand.NextBool())
                {
                    teleportTarget.X *= -1;
                }

                //Add the player's position to it to convert it to an actual tile coordinate
                teleportTarget += Main.player[npc.target].position / 16;

                //Starting from the point we picked, go down one block at a time until we find hit a solid block
                bool odd = false;
                for (int y = 0; Math.Abs(y) < range / 2;)
                {
                    if (odd)
                    {
                        y *= -1;
                        y++;
                        odd = !odd;
                    }
                    else
                    {
                        y *= -1;
                        odd = !odd;
                    }
                    if (UsefulFunctions.IsTileReallySolid((int)teleportTarget.X, (int)teleportTarget.Y + y))
                    {
                        //Skip to the next tile if any of the following is true:

                        // If there are solid blocks in the way, leaving no room to teleport to
                        if (Collision.SolidTiles((int)teleportTarget.X - 1, (int)teleportTarget.X + 1, (int)teleportTarget.Y + y - 4, (int)teleportTarget.Y + y - 1))
                        {
                            //Main.NewText("Fail 1");
                            continue;
                        }

                        //If it requires line of sight and the spot can't see the player, skip to the next try.
                        //Collision.CanHit works in PIXELS; this used to pass tile coords, so the check tested a point near the world's
                        //top-left corner and passed or failed at random (clones landing on top of the arena roof). Eye point = 70px
                        //above the floor tile, the same offset newPosition uses below, so it's roughly the boss's chest.
                        Vector2 eyePoint = new Vector2((int)teleportTarget.X * 16, ((int)teleportTarget.Y + y) * 16 - 70);
                        Vector2 playerCenter = Main.player[npc.target].Center;
                        bool canSeePlayer = Collision.CanHit(eyePoint, 2, 2, playerCenter, 2, 2) && Collision.CanHitLine(eyePoint, 2, 2, playerCenter, 2, 2);

                        if (requireLineofSight && !canSeePlayer)
                        {
                            //Main.NewText("Fail 3");
                            continue;
                        }

                        //If the selected tile has lava above it, and the npc isn't immune
                        else if (Main.tile[(int)teleportTarget.X, (int)teleportTarget.Y + y - 1].LiquidType == LiquidID.Lava && !npc.lavaImmune)
                        {
                            //Main.NewText("Fail 4");
                            continue;
                        }

                        //Then teleport and return
                        Vector2 newPosition = Vector2.Zero;
                        newPosition.X = ((int)teleportTarget.X * 16 - npc.width / 2); //Center npc at target
                        newPosition.Y = (((int)teleportTarget.Y + y) * 16 - 70); //Subtract 75, not npc.height from y so block is under feet (because of the way its drawn)
                        //No TargetClosest here: targeting belongs to despawnHandler.TargetAndDespawn; picking a spot shouldn't retarget the boss.
                        //Main.NewText(newPosition.X + " " + newPosition.Y);
                        return newPosition;
                    }
                }
            }

            return null;
        }
        public static void QueueTeleport(NPC npc, int range, bool requireLineofSight = true, int TeleportTelegraphTime = 140)
        {
            Vector2? potentialNewPos;

            //SoundEngine.PlaySound(SoundID.Item8, npc.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 100; i++)
                {
                    potentialNewPos = GenerateTeleportPosition(npc, range, requireLineofSight);
                    if (potentialNewPos.HasValue && (!requireLineofSight || (Collision.CanHit(potentialNewPos.Value, 1, 1, Main.player[npc.target].Center, 1, 1) && Collision.CanHitLine(potentialNewPos.Value, 1, 1, Main.player[npc.target].Center, 1, 1))))
                    {
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportCountdown = TeleportTelegraphTime;
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportTelegraph = potentialNewPos.Value;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            //Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, npc.whoAmI, TeleportTelegraphTime);
                            //Projectile.NewProjectileDirect(npc.GetSource_FromThis(), potentialNewPos.Value, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, ai1: TeleportTelegraphTime);
                        }

                        break;
                    }
                }
            }
        }

        public void ExecuteQueuedTeleport(NPC npc)
        {
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            
            if (globalNPC.TeleportTelegraph == Vector2.Zero)
            {
                return;
            }

            //SoundEngine.PlaySound(SoundID.Item8, npc.Center);


            Vector2 diff = globalNPC.TeleportTelegraph - npc.Center;
	

            float length = diff.Length();
            diff.Normalize();
            Vector2 offset = Vector2.Zero;

            for (int i = 0; i < length; i++)
            {
                offset += diff;
                if (Main.rand.NextBool(2))
                {
                    Vector2 dustPoint = offset;
                    dustPoint.X += Main.rand.NextFloat(-npc.width / 2, npc.width / 2);
                    dustPoint.Y += Main.rand.NextFloat(-npc.height / 2, npc.height / 2);
                    if (Main.rand.NextBool() && isClone)
                    {
                        //Dust.NewDustPerfect(npc.Center + dustPoint, 71, diff * 5, 200, default, 0.8f).noGravity = true;
                    }
                    else
                    {
                        //Dust.NewDustPerfect(npc.Center + dustPoint, DustID.FireworkFountain_Pink, diff * 5, 200, default, 0.8f).noGravity = true;
                    }
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
                //Projectile.NewProjectileDirect(npc.GetSource_FromThis(), globalNPC.TeleportTelegraph, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
            }

            movesBeforeTeleport = DecideMovesBeforeTeleport(Main.npc[npc.whoAmI]);
            npc.Center = globalNPC.TeleportTelegraph;

            //Consume the destination. Otherwise a later QueueTeleport that finds no valid spot leaves this one in place,
            //and the next warp jumps back to an old position that may be nowhere near the player any more.
            globalNPC.TeleportTelegraph = Vector2.Zero;
        }

        #endregion

    }
}
