using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.Utilities;
using static Humanizer.On;

namespace tsorcRevamp.NPCs.Bosses.Pinwheel
{
    [AutoloadBossHead]

    //Current bugs:
    // - Clones will spawn without line of sight. It happens when the player is near the top of the arena. To circumvent this I tried spawning the clones at the
    //      start of CreateClones() elsewhere and then teleporting them to the arena at the end of CreateClones(), but this leads to the same problem.
    // - When summoned with Boss Rematch Tome, Pinwheel spawns in the ceiling. I assume there is always a set distance between player and boss, but I don't know where
    //      this is to try and adjust it.
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
                if (Phase == 0) //This allows us to bring current hp down to 200 as soon as it spawns
                {
                    NPC.lifeMax = (int)(200 * HealthScale);

                    if (!cloneSpawned)
                    {
                        NPC.life = (int)(200 * HealthScale);
                        cloneSpawned = true;
                    }
                }
                if (Phase >= 1)
                {
                    NPC.lifeMax = (int)(400 * HealthScale);

                    if (!cloneSpawned)
                    {
                        NPC.life = (int)(400 * HealthScale);
                        cloneSpawned = true;
                    }
                }
            }

            if (Phase >= 1)
            {
                attackTransitionDuration = 120;
                NPC.defense = 10;
                damageModifier = 2.5f;

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
                    if (isClone) //Clones losing lanterns
                    {
                        if (Main.npc[mainBossIndex].life < (int)(4100 * HealthScale) && Phase == 1) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.life > 1)
                            {
                                for (int i = 0; i < 30; i++)
                                { 
                                    Dust.NewDustDirect(lanternMiddleLeft, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -1f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -1f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -1f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -1f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            phaseTransitionTimeRemaining = phaseTransitionDuration;
                            Phase++; //Use this rather than NextPhase() because I don-t want to set MoveTimer back to 0
                        }
                        if (Main.npc[mainBossIndex].life < (int)(3700 * HealthScale) && Phase == 2) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.life > 1)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lanternBottomRight, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            phaseTransitionTimeRemaining = phaseTransitionDuration;
                            Phase++;
                        }
                        if (Main.npc[mainBossIndex].life < (int)(3300 * HealthScale) && Phase == 3) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.life > 1)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lanternBottomLeft, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            phaseTransitionTimeRemaining = phaseTransitionDuration;
                            Phase++;
                        }
                        if (Main.npc[mainBossIndex].life < (int)(2900 * HealthScale) && Phase == 4) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.life > 1)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lanternMiddleRight, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            attackTransitionDuration = 140;
                            phaseTransitionTimeRemaining = phaseTransitionDuration;
                            Phase++;
                        }
                        if (Main.npc[mainBossIndex].life < (int)(2500 * HealthScale) && Phase == 5 && NPC.life > 1) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.life > 1)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lanternTopLeft, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                    Dust.NewDustDirect(lanternTopRight, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            attackTransitionDuration = 180;
                            phaseTransitionDuration = 400;
                            NextPhase();
                        }
                    }

                    else //Main boss losing lanterns
                    {
                        if (NPC.life < (int)(4100 * HealthScale) && Phase == 1)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lanternMiddleLeft, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -1f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -1f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -1f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -1f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            phaseTransitionTimeRemaining = phaseTransitionDuration;
                            Phase++;
                        }
                        if (NPC.life < (int)(3700 * HealthScale) && Phase == 2) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lanternBottomRight, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            phaseTransitionTimeRemaining = phaseTransitionDuration;
                            Phase++;
                        }
                        if (NPC.life < (int)(3300 * HealthScale) && Phase == 3) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient) {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lanternBottomLeft, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomLeft, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomLeft, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomLeft, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternBottomLeft, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            phaseTransitionTimeRemaining = phaseTransitionDuration;
                            Phase++;
                        }
                        if (NPC.life < (int)(2900 * HealthScale) && Phase == 4) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lanternMiddleRight, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternMiddleRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            attackTransitionDuration = 140;
                            phaseTransitionTimeRemaining = phaseTransitionDuration;
                            Phase++;
                        }
                        if (NPC.life < (int)(2500 * HealthScale) && Phase == 5) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item89 with { Volume = 1.2f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 10 }, NPC.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Dust.NewDustDirect(lanternTopLeft, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                    Dust.NewDustDirect(lanternTopRight, 8, 10, 6, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), 50, default(Color), Main.rand.NextFloat(1, 1.5f));
                                }
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopLeft + new Vector2(4, 5), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire1, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire2, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), lanternTopRight, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1.5f, -0.5f)), ProjectileID.GreekFire3, 20, 0, Main.myPlayer, 0, 0);
                            }
                            attackTransitionDuration = 180;
                            phaseTransitionDuration = 400;
                            deathAnimationDuration = 250;
                            NextPhase();
                        }
                    }
                }

                #endregion

            }

            if (Phase == 6)
            { 
                damageModifier = 3f;
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
                new BossMove(CreateClones, 220, id : PinwheelAttackID.CreateClonesID), //Always plays twice at the start, not what I want. However I skip one.
                new BossMove(VolcanicEruption, 370, id : PinwheelAttackID.VolcanicEruptionID), //Move in index 1 of this list doesn't run till in second movepool loop, hence skipping move several times for clones
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

                if (MoveTimer == 90 && Main.netMode != NetmodeID.MultiplayerClient && Phase < 5) //Shoot bouncing fireball
                {
                    int type = ProjectileID.Fireball;
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX, speedY), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    shot1.friendly = false;
                    shot1.hostile = true;
                    shot1.timeLeft = 600;
                    if (Phase >= 1) shot1.timeLeft = 720;
                }

                if (MoveTimer == 120 && Main.netMode != NetmodeID.MultiplayerClient && Phase < 3) //Shoot bouncing fireball
                {
                    int type = ProjectileID.Fireball;
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX, speedY), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    shot1.friendly = false;
                    shot1.hostile = true;
                    shot1.timeLeft = 600;
                    if (Phase >= 1) shot1.timeLeft = 720;
                }

                if (MoveTimer == 150 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
                {
                    int type = ProjectileID.Fireball;
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX, speedY), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    shot1.friendly = false;
                    shot1.hostile = true;
                    shot1.timeLeft = 600;
                    if (Phase >= 1) shot1.timeLeft = 720;
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

                if (MoveTimer == 90 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
                {
                    int type = ProjectileID.CultistBossFireBallClone;

                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX * 0.5f, speedY * 0.5f), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    shot1.friendly = false;
                    shot1.hostile = true;
                    shot1.timeLeft = 600;
                }

                if (MoveTimer == 120 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
                {
                    int type = ProjectileID.CultistBossFireBallClone;

                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX * 0.5f, speedY * 0.5f), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    shot1.friendly = false;
                    shot1.hostile = true;
                    shot1.timeLeft = 600;
                }

                if (MoveTimer == 150 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
                {
                    int type = ProjectileID.CultistBossFireBallClone;

                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX * 0.5f, speedY * 0.5f), type, (int)(DamageNumbers["BouncingFireballDamage"] * damageModifier), 0f, Main.myPlayer, 0, 0);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    shot1.friendly = false;
                    shot1.hostile = true;
                    shot1.timeLeft = 600;
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

            if (MoveTimer == 90 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot killable Gaibon fireballs
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                if (Phase < 6 || Phase == 6)
                {
                    NPC shot1 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternTopLeft.X, (int)lanternTopLeft.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot1.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 5 || Phase == 6)
                {
                    NPC shot2 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternMiddleRight.X, (int)lanternMiddleRight.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot2.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 4 || Phase == 6)
                {
                    NPC shot3 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternBottomLeft.X, (int)lanternBottomLeft.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot3.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 6 || Phase == 6)
                {
                    NPC shot4 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternTopRight.X, (int)lanternTopRight.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot4.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 2 || Phase == 6)
                {
                    NPC shot5 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternMiddleLeft.X, (int)lanternMiddleLeft.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot5.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 3 || Phase == 6)
                {
                    NPC shot6 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternBottomRight.X, (int)lanternBottomRight.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot6.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
            }

            if (MoveTimer == 120 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot killable Gaibon fireballs
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                if (Phase < 6 || Phase == 6)
                {
                    NPC shot1 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternTopLeft.X, (int)lanternTopLeft.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot1.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 5 || Phase == 6)
                {
                    NPC shot2 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternMiddleRight.X, (int)lanternMiddleRight.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot2.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 4 || Phase == 6)
                {
                    NPC shot3 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternBottomLeft.X, (int)lanternBottomLeft.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot3.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 6 || Phase == 6)
                {
                    NPC shot4 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternTopRight.X, (int)lanternTopRight.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot4.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 2 || Phase == 6)
                {
                    NPC shot5 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternMiddleLeft.X, (int)lanternMiddleLeft.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot5.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
                }
                if (Phase < 3 || Phase == 6)
                {
                    NPC shot6 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternBottomRight.X, (int)lanternBottomRight.Y, projectileType, 0, 0, shootSpeed.X, shootSpeed.Y);
                    shot6.damage = (int)(DamageNumbers["KillableFireballDamage"] * damageModifier);
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

            if (MoveTimer >= 120 && MoveTimer < 300 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vector8 = new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30);
                Vector2 shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1f);
                if (Phase == 1) shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1.25f);
                if (Phase > 1 && Phase < 5) shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1f);
                if (Phase == 5) shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 0.6f);
                if (Phase == 6) shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1.4f);
                int projectileType = ModContent.ProjectileType<Projectiles.Enemy.SmallFlameJet>();

                if (Phase < 6) {
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(shootSpeed.X * Main.rand.NextFloat(2.5f, 4.5f), shootSpeed.Y * Main.rand.NextFloat(2.5f, 4.5f)), projectileType, (int)(DamageNumbers["FlamethrowerDamage"] * damageModifier), 0f, Main.myPlayer, 0);
                    shot1.timeLeft = 70;
                }
                if (Phase == 6) {
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(shootSpeed.X * Main.rand.NextFloat(2.5f, 4.5f), shootSpeed.Y * Main.rand.NextFloat(2.5f, 4.5f)), projectileType, (int)(DamageNumbers["FlamethrowerDamage"] * damageModifier), 0f, Main.myPlayer, 1);
                    shot1.timeLeft = 70;
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
            if (isClone || NPC.CountNPCS(ModContent.NPCType<Pinwheel>()) >= 5 && MoveTimer < 3)
            {
                justSkippedMove = true;
                NextMove();
            }
            NPC.noGravity = true;
            NPC.velocity = Vector2.Zero;

            if (MoveTimer == 0) //Get spawn and teleport locations ready at the start of the attack to allow time to sync
            {
                cloneTimer = 0;
                cloneOffset = 0;
                cloneSpawnLocation1 = GenerateTeleportPosition(NPC, 50, true); //Choose a random location for 1 of the clones to spawn at
                cloneSpawnLocation2 = GenerateTeleportPosition(NPC, 50, true); //Choose a random location for the other clone to spawn at
                QueueTeleport(NPC, 50, true, 200); //Choose a location for main boss to teleport to
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
            if (MoveTimer == 220) //Spawn clones at the chosen random location and teleport main boss away
            {
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
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X - 5, NPC.Center.Y - 140), new Vector2(Main.rand.NextFloat(0.5f, 3f), Main.rand.NextFloat(-5.5f, -1.5f)), ProjectileID.BallofFire, (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
                    shot1.timeLeft = 360;
                    shot1.hostile = true;
                    shot1.friendly = false;
                    if (Phase == 1) shot1.timeLeft = 480;
                }

                if (MoveTimer > 120 && MoveTimer < 250 && (MoveTimer - 15) % leftSideShotDelay == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X - 5, NPC.Center.Y - 140), new Vector2(Main.rand.NextFloat(-3f, -0.5f), Main.rand.NextFloat(-5.5f, -1.5f)), ProjectileID.BallofFire, (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
                    shot1.timeLeft = 360;
                    shot1.hostile = true;
                    shot1.friendly = false;
                    if (Phase >= 1) shot1.timeLeft = 480;
                }
            }

            if (Phase == 6 && MoveTimer > 120 && MoveTimer < 250 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (MoveTimer % rightSideShotDelay == 0)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(Main.rand.NextFloat(NPC.Center.X, NPC.Center.X + 120), NPC.Bottom.Y), new Vector2(0, Main.rand.NextFloat(-3.5f, -1.5f)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
                }

                if ((MoveTimer - 15) % leftSideShotDelay == 0)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(Main.rand.NextFloat(NPC.Center.X - 120, NPC.Center.X), NPC.Bottom.Y), new Vector2(0, Main.rand.NextFloat(-3.5f, -1.5f)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
                }

                if (MoveTimer == 220)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie81 with { Volume = 0.8f, Pitch = -1f, PitchVariance = 1f, MaxInstances = 5 }, NPC.Center); //wraith
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X, NPC.Center.Y - 30), new Vector2(0, Main.rand.NextFloat(0, -1f)), ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackKnightHomingCrystal>(), (int)(DamageNumbers["VolcanicEruptionDamage"] * damageModifier), 0, Main.myPlayer, 0, 0);
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

            if (Main.netMode != NetmodeID.MultiplayerClient && MoveTimer == 120)
            {
                if (Phase < 6)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 5 }, NPC.Center);

                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 15);
                        position += NPC.Center;
                        Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 3f, ModContent.ProjectileType<Projectiles.Enemy.BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier), 0f, Main.myPlayer);
                        shot1.timeLeft = 90;
                    }
                }

                if (Phase == 6) //In final phase, create larger faster ring with demon sickles
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1f, PitchVariance = 0.2f, MaxInstances = 5 }, NPC.Center);

                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 30f);
                        position += NPC.Center;
                        Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 8f, ModContent.ProjectileType<Projectiles.Enemy.BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier), 0f, Main.myPlayer);
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

            if (introTimer == 40)
            {
                NextMove(); //Skip first move of main boss, which would have been a duplicate CreateClones()
            }

            if (introTimer == 30 && isClone)
            {
                NextMove();
                NextMove();
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
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 explosionLocation = Main.rand.NextVector2Circular(100, 100);
                            for (int i = 0; i < 40; i++) //Shadowflame
                            {
                                Dust.NewDustDirect(NPC.Center + explosionLocation - new Vector2(-4, -4), 8, 8, DustID.Shadowflame, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3), 50, default(Color), Main.rand.NextFloat(1f, 4f));
                                Dust dust = Dust.NewDustDirect(NPC.Center + explosionLocation - new Vector2(-4, -4), 8, 8, DustID.ShadowbeamStaff, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3), 50, default(Color), Main.rand.NextFloat(1f, 4f));
                                dust.velocity *= 2;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { MaxInstances = 3 }, NPC.Center);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + explosionLocation, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 100, 30);
                        }
                    }
                    if (deathAnimationProgress == deathAnimationDuration && Main.netMode != NetmodeID.Server)
                    {
                        Item.NewItem(NPC.GetSource_FromThis(), NPC.Center, ModContent.ItemType<Items.BossBags.PinwheelBag>(), 1);
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

            if (attackTransitionTimeRemaining <= 1 && introFinished && moveCount >= movesBeforeTeleport && NPC.life > 1)
            {
                ExecuteQueuedTeleport(NPC);
                moveCount = 0;
                justTeleported = true;
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
                    NPC.Center = new Vector2(4139f, 933f) * 16;
                    justTeleported = true;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                    }
                }

                if (phaseTransitionTimeRemaining == phaseTransitionDuration - 240 && Main.netMode != NetmodeID.Server)
                {
                    Item.NewItem(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-18, 24), ModContent.ItemType<Items.Accessories.Defensive.RingOfFavorAndProtection>(), 1, false, -1);
                }

                if (phaseTransitionTimeRemaining < phaseTransitionDuration - 300 && phaseTransitionTimeRemaining > phaseTransitionDuration - 1000)
                {
                    NPC.lifeMax = (int)(5000 * HealthScale);

                    if (NPC.life < NPC.lifeMax)
                    {
                        NPC.life += NPC.lifeMax / 250;
                    }
                    if (NPC.life > NPC.lifeMax)
                    {
                        NPC.life = (int)(5000 * HealthScale);
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

                if (Main.netMode != NetmodeID.MultiplayerClient && phaseTransitionTimeRemaining == phaseTransitionDuration - 720)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1.1f, PitchVariance = 0.2f, MaxInstances = 5 }, NPC.Center);

                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 30f);
                        position += NPC.Center;
                        Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 8f, ModContent.ProjectileType<Projectiles.Enemy.BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier) + 6, 0f, Main.myPlayer);
                        shot1.timeLeft = 105;
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && phaseTransitionTimeRemaining == phaseTransitionDuration - 800)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1.1f, Pitch = -0.2f, MaxInstances = 5 }, NPC.Center);

                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 30f);
                        position += NPC.Center;
                        Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 8f, ModContent.ProjectileType<Projectiles.Enemy.BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier) + 6, 0f, Main.myPlayer);
                        shot1.timeLeft = 105;
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && phaseTransitionTimeRemaining == phaseTransitionDuration - 920)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 1f, Pitch = -0.4f, MaxInstances = 5 }, NPC.Center);

                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 30f);
                        position += NPC.Center;
                        Vector2 velocity = UsefulFunctions.Aim(NPC.Center, position, 1f);
                        Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(position.X, position.Y - 30), velocity * 12f, ModContent.ProjectileType<Projectiles.Enemy.BlindingPulse>(), (int)(DamageNumbers["BlindingPulseDamage"] * damageModifier) + 10, 0f, Main.myPlayer);
                        shot1.timeLeft = 80;
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
                        if (Main.rand.NextBool(2)) {
                            Dust dust1 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, 60f, 0f, 50, default(Color), 1.5f);
                            dust1.noGravity = true;
                            dust1.velocity /= 25;
                            dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2)) {
                            Dust dust2 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, -60f, 0f, 50, default(Color), 1.5f);
                            dust2.noGravity = true;
                            dust2.velocity /= 25;
                            dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2)) {
                            Dust dust3 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, 34f, -26f, 50, default(Color), 1.5f);
                            dust3.noGravity = true;
                            dust3.velocity /= 25;
                            dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2)) {
                            Dust dust4 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, -34f, -26f, 50, default(Color), 1.5f);
                            dust4.noGravity = true;
                            dust4.velocity /= 25;
                            dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2)) {
                            Dust dust5 = Dust.NewDustDirect(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 2), 8, 10, DustID.Shadowflame, -15f, -45f, 50, default(Color), 1.5f);
                            dust5.noGravity = true;
                            dust5.velocity /= 20;
                            dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                        }
                        if (Main.rand.NextBool(2)) {
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
            /*if (!isClone)
            {
                Main.NewText("opacityTimer is " + opacityTimer + " attackTransTimeRem is " + attackTransitionTimeRemaining + " attacksBeforeTP " + movesBeforeTeleport);
            }*/
            if (introFinished && !justTeleported && ((attackTransitionTimeRemaining > 0 && attackTransitionTimeRemaining <= 30) && moveCount >= movesBeforeTeleport) || (MoveIndex == PinwheelAttackID.CreateClonesID && MoveTimer > 190 && MoveTimer <= 220) && !(phaseTransitionTimeRemaining > phaseTransitionDuration - 90 && Phase == 1))
            {
                opacityTimer--;
            }
            if (opacityTimer < 30 && justTeleported)
            {
                opacityTimer++;
            }
            if (opacityTimer > 30)
            {
                opacityTimer = 30;
            }
            if (opacityTimer < 0)
            {
                opacityTimer = 0;
            }

            opacity = (double)opacityTimer / (double)30;
            if (introTimer < introDuration && !isClone) { opacity = (double)introTimer / (double)40; }
            if (introTimer < introDuration && isClone) { opacity = (double)introTimer / (double)30; }

            //Death opacity
            if (deathAnimationProgress > deathAnimationDuration / 2) { opacity = ((double)deathAnimationProgress / (double)90) * -1 + 2; }
            if (deathAnimationProgress > deathAnimationDuration / 2 && Phase == 6 && !isClone) { opacity = ((double)deathAnimationProgress / (double)150) * -1 + 2; }


            //"Death" during phase transition opacity for teleport to arena center
            if (phaseTransitionTimeRemaining < phaseTransitionDuration - 80 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 110) { opacityTimer--; }
            if (phaseTransitionTimeRemaining == phaseTransitionDuration - 110) { opacityTimer = 0; }
            if (phaseTransitionTimeRemaining < phaseTransitionDuration - 110 && phaseTransitionTimeRemaining >= phaseTransitionDuration - 140) { opacityTimer++; }
            /*if (phaseTransitionTimeRemaining <= phaseTransitionDuration - 140 && phaseTransitionTimeRemaining > phaseTransitionDuration - 1000) 
            { 
                opacityTimer = 30;
                opacity = 1;
            }*/


            //Draw main boss texture
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
            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                potionType = ModContent.ItemType<Lifegem>();
            } else { potionType = ItemID.HealingPotion; }
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            /*if (NPC.lifeMax == (int)(5000 * HealthScale)) 
            {
                npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.PinwheelBag>())); //Bag is dropped as an item at the end of death anim instead, because checking for anything else seems to fail
            }*/
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MaskOfTheChild>(), 1, 1, 1));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MaskOfTheFather>(), 1, 1, 1));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MaskOfTheMother>(), 1, 1, 1));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<EstusRing>(), 1, 1, 1));

            npcLoot.Add(notExpertCondition);
            //npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>())); if you want to give it an extra one time drop outside of expert mode
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

                        //If it requires line of sight, and there is not a clear path, and it has not tried at least 50 times, then skip to the next try
                        else if (requireLineofSight && !(Collision.CanHit(new Vector2(teleportTarget.X, (int)teleportTarget.Y + y), 2, 2, Main.player[npc.target].Center / 16, 2, 2) && Collision.CanHitLine(new Vector2(teleportTarget.X, (int)teleportTarget.Y + y), 2, 2, Main.player[npc.target].Center / 16, 2, 2)))
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
                        npc.TargetClosest(true);
                        npc.netUpdate = true;
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
        }

        #endregion

    }
}
