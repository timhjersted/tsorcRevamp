using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.NPCs.Bosses.Pinwheel
{
    [AutoloadBossHead]

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
            NPC.lifeMax = 2500;
            NPC.timeLeft = 180;
            NPC.value = 500;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.ExampleBoss.DespawnHandler"), Color.Cyan, 180);

            //Terraria.GameContent.UI.BigProgressBar.IBigProgressBar bossBar;
            //Main.BigBossProgressBar.TryGetSpecialVanillaBossBar(NPCID.EyeofCthulhu, out bossBar);
            //NPC.BossBar = bossBar;

            //You can also specify BossBase specific values here
            introDuration = 40;
            attackTransitionDuration = 120;
            //phaseTransitionDuration = 120;
            deathAnimationDuration = 180;
            randomAttacks = true;
        }

        public bool isClone = false;
        bool cloneSpawned = false;
        bool introFinished = false;

        public int mainBossIndex;
        bool gotMainBossIndex = false;

        Vector2? cloneSpawnLocation1;
        Vector2? cloneSpawnLocation2;
        float transparency = 0.5f;
        bool justTeleported = false;
        int opacityTimer = 30;
        int movesBeforeTeleport = 0;
        int moveCount = 0;
        double opacity;

        public override void AI()
        {
            NPC.dontTakeDamage = false;

            if (NPC.ai[3] == 1)
            {
                introDuration = 30;

                NPC.BossBar = Main.BigBossProgressBar.NeverValid; //Prevents clones from having boss health bars
                isClone = true;
                NPC.DeathSound = SoundID.NPCDeath6;
                NPC.lifeMax = 200;
                NPC.timeLeft = 180;
                NPC.value = 0;
                NPC.boss = false;
                despawnHandler = new NPCDespawnHandler(null, Color.Black, 6);

                if (!cloneSpawned) //This allows us to bring current hp down to 200 as soon as it spawns
                {
                    NPC.life = 200;
                    cloneSpawned = true;
                }
            }

            if (!introFinished && introTimer > introDuration)
            {
                introFinished = true;
                movesBeforeTeleport = DecideMovesBeforeTeleport(Main.npc[NPC.whoAmI]);
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

            if (isClone && Main.npc[mainBossIndex].life <= 1) //Kill clones if main boss dies
            {
                MoveTimer = 0;
                NPC.life = 1;
            }

            if (!isClone)
            {
                if (NPC.CountNPCS(ModContent.NPCType<Pinwheel>()) < 3 && NPC.ai[0] != 0 && attackTransitionTimeRemaining == 30) //If there are less than 3 Pinwheels and it wasn-t about to spawn clones anyway. Check transition time so as not to interrupt current attack
                {
                    MoveIndex = PinwheelAttackID.CreateClonesID; //Then spawn clones, while not skipping what attack was coming next anyway
                }
                //Main.NewText("attackTransitionTimeRemaining" + attackTransitionTimeRemaining + " " + "opacityTimer" + opacityTimer);
            }

            if (attackTransitionTimeRemaining == attackTransitionDuration)
            {
                moveCount++;
            }
            if (opacity < 1)
            {
                NPC.dontTakeDamage = true;
                NPC.velocity = Vector2.Zero;
            }

            if (justTeleported)
            {
                NPC.velocity = Vector2.Zero;
            }
            if (opacityTimer == 30 && attackTransitionTimeRemaining == 0)
            {
                justTeleported = false;
            }

            base.AI();
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
                new BossMove(CreateClones, 220, id : PinwheelAttackID.CreateClonesID), //Always plays twice at the start, not what I want
                new BossMove(VolcanicEruption, 370, id : PinwheelAttackID.VolcanicEruptionID), //Move in index 1 of this list doesn't run till in second movepool loop
                new BossMove(Flamethrower, 420, id : PinwheelAttackID.FlamethrowerID),
                new BossMove(BouncingFireball, 180, id: PinwheelAttackID.BouncingFireballID),
                new BossMove(KillableFireball, 150, id: PinwheelAttackID.KillableFireballID),
            };


            //Set the damage numbers for every attack or projectile your boss uses here
            //Remember: Contact damage is doubled, and projectile damage is multiplied by 4!
            DamageNumbers = new Dictionary<string, int>
            {
                ["BouncingFireballDamage"] = 8, //was 14
                ["KillableFireballDamage"] = 26, //was 40
                ["FlamethrowerDamage"] = 6, //was 10
                ["VolcanicEruptionDamage"] = 9, //was 16
                ["BaseContactDamage"] = 0,
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
        }


        #region Attacks


        public void BouncingFireball()
        {
            NPC.velocity.X *= 0f;

            float num48 = 5f;
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

            if (MoveTimer == 90 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
            {
                int type = ProjectileID.Fireball;
                Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX, speedY), type, DamageNumbers["BouncingFireballDamage"], 0f, Main.myPlayer, 0, 0);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                shot1.friendly = false;
                shot1.hostile = true;
                shot1.timeLeft = 600;
            }

            if (MoveTimer == 120 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
            {
                int type = ProjectileID.Fireball;
                Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(speedX, speedY), type, DamageNumbers["BouncingFireballDamage"], 0f, Main.myPlayer, 0, 0);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                shot1.friendly = false;
                shot1.hostile = true;
                shot1.timeLeft = 600;
            }

            if (MoveTimer == 150 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
            {
                int type = ProjectileID.Fireball;
                Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y),  new Vector2(speedX, speedY), type, DamageNumbers["BouncingFireballDamage"], 0f, Main.myPlayer, 0, 0);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                shot1.friendly = false;
                shot1.hostile = true;
                shot1.timeLeft = 600;
            }
        }

        public void KillableFireball()
        {
            NPC.velocity.X *= 0f;

            Vector2 lanternBottomLeft = new Vector2(NPC.position.X - 55, NPC.position.Y + 88);
            Vector2 lanternMiddleLeft = new Vector2(NPC.position.X - 58, NPC.position.Y + 16);
            Vector2 lanternTopLeft = new Vector2(NPC.position.X - 18, NPC.position.Y - 40);
            Vector2 lanternTopRight = new Vector2(NPC.position.X + 68, NPC.position.Y - 40);
            Vector2 lanternMiddleRight = new Vector2(NPC.position.X + 106, NPC.position.Y + 16);
            Vector2 lanternBottomRight = new Vector2(NPC.position.X + 105, NPC.position.Y + 88);
            Vector2 shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 8);

            if (MoveTimer == 90 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot killable Gaibon fireballs
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                NPC shot1 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternTopLeft.X, (int)lanternTopLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot1.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot2 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternMiddleRight.X, (int)lanternMiddleRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot2.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot3 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternBottomLeft.X, (int)lanternBottomLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot3.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot4 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternTopRight.X, (int)lanternTopRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot4.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot5 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternMiddleLeft.X, (int)lanternMiddleLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot5.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot6 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternBottomRight.X, (int)lanternBottomRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot6.damage = DamageNumbers["KillableFireballDamage"];
            }

            if (MoveTimer == 120 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot killable Gaibon fireballs
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                NPC shot1 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternTopLeft.X, (int)lanternTopLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot1.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot2 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternMiddleRight.X, (int)lanternMiddleRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot2.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot3 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternBottomLeft.X, (int)lanternBottomLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot3.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot4 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternTopRight.X, (int)lanternTopRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot4.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot5 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternMiddleLeft.X, (int)lanternMiddleLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot5.damage = DamageNumbers["KillableFireballDamage"];
                NPC shot6 = NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)lanternBottomRight.X, (int)lanternBottomRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                shot6.damage = DamageNumbers["KillableFireballDamage"];
            }

        }

        public void Flamethrower()
        {
            NPC.velocity.X *= 0f;

            int dustQuantity = (int)MoveTimer / 15;
            for (int i = 0; i < dustQuantity; i++)
            {
                if (MoveTimer > 40 && MoveTimer < 300)
                {
                    UsefulFunctions.DustRing(new Vector2(NPC.Center.X, NPC.Center.Y - 30), 6, 6, 1, 3);
                }
            }
            
            if (MoveTimer >= 120 && MoveTimer < 300 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vector8 = new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30);
                Vector2 shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1f);
                for (int i = 0; i < 3; i++)
                {
                    Projectile shot1 = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(vector8.X, vector8.Y), new Vector2(shootSpeed.X * Main.rand.NextFloat(4, 6), shootSpeed.Y * Main.rand.NextFloat(4, 6)), ModContent.ProjectileType<Projectiles.Enemy.SmallFlameJet>(), DamageNumbers["FlamethrowerDamage"], 0f, Main.myPlayer);
                    shot1.timeLeft = 50;
                }
                //play breath sound
                if (Main.rand.NextBool(3))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, PitchVariance = 0.2f }, NPC.Center); //flame thrower sound
                }

            }
        }

        public void CreateClones()
        {
            if (isClone || NPC.CountNPCS(ModContent.NPCType<Pinwheel>()) >= 5)
            {
                justSkippedMove = true;
                NextMove();
            }
            NPC.noGravity = true;
            NPC.velocity = Vector2.Zero;

            if (MoveTimer == 0) //Get spawn and teleport locations ready at the start of the attack to allow time to sync
            {
                cloneSpawnLocation1 = GenerateTeleportPosition(NPC, 50, true); //Choose a random location for 1 of the clones to spawn at
                cloneSpawnLocation2 = GenerateTeleportPosition(NPC, 50, true); //Choose a random location for the other clone to spawn at
                QueueTeleport(NPC, 50, true, 200); //Choose a location for main boss to teleport to
            }
            if (MoveTimer >= 120)
            {
                NPC.dontTakeDamage = true;
            }
            if (MoveTimer == 220) //Spawn clones at the chosen random location and teleport main boss away
            {
                if (cloneSpawnLocation1.HasValue)
                {
                    int cloneLeft = NPC.NewNPC(NPC.GetSource_FromThis(), (int)cloneSpawnLocation1.Value.X, (int)cloneSpawnLocation1.Value.Y + 70, ModContent.NPCType<Pinwheel>(), 0, 1, 0, 0, 1);
                    Main.npc[cloneLeft].boss = false;
                }
                if (cloneSpawnLocation2.HasValue)
                {
                    int cloneRight = NPC.NewNPC(NPC.GetSource_FromThis(), (int)cloneSpawnLocation2.Value.X, (int)cloneSpawnLocation2.Value.Y + 70, ModContent.NPCType<Pinwheel>(), 0, 4, 0, 0, 1);
                    Main.npc[cloneRight].boss = false;
                }
                ExecuteQueuedTeleport(NPC);
                justTeleported = true;
                moveCount = 0;
            }
        }

        //These allow us to alternate between which side each shot ruptures toward
        int rightSideShotDelay = 30;
        int leftSideShotDelay = 30;

        public void VolcanicEruption()
        {
            NPC.velocity *= 0f;

            if (MoveTimer > 120 && MoveTimer < 250 && MoveTimer % rightSideShotDelay == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int shot1 = Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X - 5, NPC.Center.Y - 140), new Vector2(Main.rand.NextFloat(0.5f, 3f), Main.rand.NextFloat(-5.5f, -1.5f)), ProjectileID.BallofFire, DamageNumbers["VolcanicEruptionDamage"], 0, Main.myPlayer, 0, 0);
                    Main.projectile[shot1].timeLeft = 600;
                    Main.projectile[shot1].hostile = true;
                    Main.projectile[shot1].friendly = false;

                }
            }

            if (MoveTimer > 120 && MoveTimer < 250 && (MoveTimer - 15) % leftSideShotDelay == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int shot1 = Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X - 5, NPC.Center.Y - 140), new Vector2(Main.rand.NextFloat(-3f, -0.5f), Main.rand.NextFloat(-5.5f, -1.5f)), ProjectileID.BallofFire, DamageNumbers["VolcanicEruptionDamage"], 0, Main.myPlayer, 0, 0);
                    Main.projectile[shot1].timeLeft = 600;
                    Main.projectile[shot1].hostile = true;
                    Main.projectile[shot1].friendly = false;
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

            NPC.dontTakeDamage = true;
            if (deathAnimationProgress == deathAnimationDuration && isClone)
            {
                NPC.life = 0;
            }
            else
            {
                base.HandleDeath();
            }
        }

        public override void AttackTransition()
        {
            if (introTimer > introDuration)
            {
                UsefulFunctions.SmoothHoming(NPC, Target.Center, 0.01f, 0.15f);
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

            if (attackTransitionTimeRemaining <= 1 && introFinished && moveCount >= movesBeforeTeleport)
            {
                ExecuteQueuedTeleport(NPC);
                moveCount = 0;
                justTeleported = true;
            }
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

            //Main.NewText(dustQuantityTimer);
            //Main.NewText(MoveTimer);


            #region Bouncing Fireballs Attack Dusts


            if (MoveIndex == PinwheelAttackID.BouncingFireballID && NPC.frame.Y == 16 * 234) //If arms up, on frame 16 (attacking frame, etc). 234 is frameheight
            {
                if (Main.rand.NextBool(10)) //Fire
                {
                    Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
            }

            #endregion


            #region Killable Fireballs Attack Dusts


            if (MoveIndex == PinwheelAttackID.KillableFireballID && NPC.frame.Y == 16 * 234) //If arms up, on frame 16 (attacking frame, etc). 234 is frameheight
            {
                if (Main.rand.NextBool(4)) //Fire
                {
                    Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
            }

            #endregion


            #region Flamethrower Attack Dusts

            if (MoveIndex == PinwheelAttackID.FlamethrowerID && MoveTimer >= 20 && MoveTimer < 420)
            {

                if (Main.rand.NextBool(6)) //Fire
                {
                    Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }

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
                    Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 40f, -20f, 50, default(Color), 2f);
                    dust1.noGravity = true;
                    dust1.velocity /= 8;
                    dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 47f, 13f, 50, default(Color), 2f);
                    dust2.noGravity = true;
                    dust2.velocity /= 8;
                    dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, 40f, 50, default(Color), 2f);
                    dust3.noGravity = true;
                    dust3.velocity /= 8;
                    dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, 40f, 50, default(Color), 2f);
                    dust4.noGravity = true;
                    dust4.velocity /= 8;
                    dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -47f, 13f, 50, default(Color), 2f);
                    dust5.noGravity = true;
                    dust5.velocity /= 8;
                    dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -40, -20, 50, default(Color), 2f);
                    dust6.noGravity = true;
                    dust6.velocity /= 8;
                    dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                }

                else
                {
                    for (int i = 0; i < dustQuantity; i++) //Flare
                    {
                        Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 40f, -20f, 50, default(Color), 2f);
                        dust1.noGravity = true;
                        dust1.velocity /= 8;
                        dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 47f, 13f, 50, default(Color), 2f);
                        dust2.noGravity = true;
                        dust2.velocity /= 8;
                        dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, 40f, 50, default(Color), 2f);
                        dust3.noGravity = true;
                        dust3.velocity /= 8;
                        dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, 40f, 50, default(Color), 2f);
                        dust4.noGravity = true;
                        dust4.velocity /= 8;
                        dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -47f, 13f, 50, default(Color), 2f);
                        dust5.noGravity = true;
                        dust5.velocity /= 8;
                        dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -40, -20, 50, default(Color), 2f);
                        dust6.noGravity = true;
                        dust6.velocity /= 8;
                        dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                    }
                }
            }

            #endregion


            #region Volcanic Eruption Attack Dusts


            if (MoveIndex == PinwheelAttackID.VolcanicEruptionID && MoveTimer >= 20 && MoveTimer < 370)
            {
                if (Main.rand.NextBool(10)) //Fire
                {
                    Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                    Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
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
                //Main.NewText(dustQuantity);
                //Main.NewText(flameSpeedPower);


                if (dustQuantity < 1 && Main.rand.NextBool(2)) //Flare
                {
                    Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 4f, -45f, 50, default(Color), 2f);
                    dust1.noGravity = true;
                    dust1.velocity /= 10;
                    dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 27f, -45f, 50, default(Color), 2f);
                    dust2.noGravity = true;
                    dust2.velocity /= 10;
                    dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, -20f, 50, default(Color), 2f);
                    dust3.noGravity = true;
                    dust3.velocity /= 10;
                    dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, -20f, 50, default(Color), 2f);
                    dust4.noGravity = true;
                    dust4.velocity /= 10;
                    dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -27f, -45f, 50, default(Color), 2f);
                    dust5.noGravity = true;
                    dust5.velocity /= 10;
                    dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -4, -45, 50, default(Color), 2f);
                    dust6.noGravity = true;
                    dust6.velocity /= 10;
                    dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                }

                else
                {
                    for (int i = 0; i < dustQuantity; i++) //Flare
                    {
                        Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 4f, -45f, 50, default(Color), 2f);
                        dust1.noGravity = true;
                        dust1.velocity /= 10;
                        dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 27f, -45f, 50, default(Color), 2f);
                        dust2.noGravity = true;
                        dust2.velocity /= 10;
                        dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, -20f, 50, default(Color), 2f);
                        dust3.noGravity = true;
                        dust3.velocity /= 10;
                        dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, -20f, 50, default(Color), 2f);
                        dust4.noGravity = true;
                        dust4.velocity /= 10;
                        dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -27f, -45f, 50, default(Color), 2f);
                        dust5.noGravity = true;
                        dust5.velocity /= 10;
                        dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -4, -45, 50, default(Color), 2f);
                        dust6.noGravity = true;
                        dust6.velocity /= 10;
                        dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                    }
                }
                if (dustQuantity == 2 && Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustDirect(new Vector2(NPC.Center.X - 20, NPC.Center.Y - 140), 40, 10, 127, 0, -25, 50, default(Color), 2f);
                    dust.noGravity = true;
                    dust.velocity /= 10;
                    dust.velocity *= Main.rand.NextFloat(5, 10);
                }
            }

            #endregion


            #region Create Clones Move Dusts


            if (MoveIndex == PinwheelAttackID.CreateClonesID && MoveTimer >= 20 && MoveTimer < 120)
            {
                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }

                if (Main.rand.NextBool(10)) //Gold
                {
                    Dust dust2 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust2.velocity *= 0.2f;
                    dust2.noGravity = false;
                }
            }

            if (MoveIndex == PinwheelAttackID.CreateClonesID && MoveTimer == 120)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust1.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust1.noGravity = false;

                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust2.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust2.noGravity = false;

                    Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust3.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust3.noGravity = false;

                    Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust4.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust4.noGravity = false;

                    Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust5.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust5.noGravity = false;

                    Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 57, 0, 0, 50, default(Color), 1f)];
                    dust6.velocity *= Main.rand.NextFloat(0.2f, 0.5f);
                    dust6.noGravity = false;
                }

                for (int i = 0; i < 30; i++)
                {
                    Dust dust1 = Main.dust[Dust.NewDust(new Vector2(NPC.Center.X - 2, NPC.position.Y), 4, NPC.height, 57, Main.rand.NextFloat(-4f, -3f), 0, 50, default(Color), 1f)];
                    dust1.noGravity = false;

                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.Center.X + 2, NPC.position.Y), 4, NPC.height, 57, Main.rand.NextFloat(3f, 4f), 0, 50, default(Color), 1f)];
                    dust2.noGravity = false;
                }
            }

            #endregion


            else //Dusts while not attacking. Separate randomizations so the sparse dusts look natural
            {
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust1 = Dust.NewDustDirect(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust2 = Dust.NewDustDirect(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust3 = Dust.NewDustDirect(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust4 = Dust.NewDustDirect(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust5 = Dust.NewDustDirect(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust6 = Dust.NewDustDirect(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f);
                }
            }


            #endregion


            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.Pinwheel];

            //Draw "clones"
            if (MoveIndex == PinwheelAttackID.CreateClonesID)
            {
                if (MoveTimer > 28 && MoveTimer <= 120)
                {
                    cloneTimer++;
                    cloneOffset = cloneTimer / 15;
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                }
                if (MoveTimer > 120 && MoveTimer <= 190)
                {
                    cloneTimer += 2;
                    cloneOffset = cloneTimer - 108;
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * 0.5f, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                }
                if (MoveTimer > 190 && MoveTimer <= 220)
                {
                    transparency -= 0.016f;
                    cloneOffset = cloneTimer - 108;
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * transparency, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.LightYellow * transparency, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                }
            }

            if (MoveIndex != PinwheelAttackID.CreateClonesID)
            {
                cloneTimer = 0;
            }

            if (introFinished && !justTeleported && ((attackTransitionTimeRemaining >= 0 && attackTransitionTimeRemaining < 30) && moveCount >= movesBeforeTeleport) || (MoveIndex == PinwheelAttackID.CreateClonesID && MoveTimer > 190 && MoveTimer <= 220))
            {
                opacityTimer--;
            }
            if (opacityTimer < 30 && justTeleported)
            {
                opacityTimer++;
            }
            /*if (opacityTimer == 30)
            {
                justTeleported = false;
            }*/

            opacity = (double)opacityTimer / (double)30;
            if (introTimer < introDuration && !isClone) { opacity = (double)introTimer / (double)40; }
            if (introTimer < introDuration && isClone) { opacity = (double)introTimer / (double)30; }

            //Death opacity
            if (deathAnimationProgress > deathAnimationDuration / 2) { opacity = ((double)deathAnimationProgress / (double)90) * -1 + 2; }


            //Draw main boss texture
            Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition, new Rectangle(0, NPC.frame.Y, 198, 234), lightColor * (float)opacity, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);

            return false;
        }


        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            //Glow mask for fire in lanterns
            double fireTextureOpacity = (double)opacityTimer / (double)30;
            if (introTimer < introDuration && !isClone) { fireTextureOpacity = (double)introTimer / (double)40; }
            if (introTimer < introDuration && isClone) { fireTextureOpacity = (double)introTimer / (double)30; }

            //Death opacity
            if (deathAnimationProgress > deathAnimationDuration / 2) { fireTextureOpacity = ((double)deathAnimationProgress / (double)90) * -1 + 2; }

            Texture2D fireTexture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PinwheelFireglow];
            spriteBatch.Draw(fireTexture, NPC.position - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(0, NPC.frame.Y, 198, 234), Color.White * (float)fireTextureOpacity, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
        }


        public override void FindFrame(int frameHeight)
        {

            /*if (opacity < 1)
            {
                NPC.frame.Y = 0 * frameHeight;
            }*/

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

            #endregion


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

            if (MoveIndex == PinwheelAttackID.CreateClonesID && MoveTimer != 0 && MoveTimer < 220)
            {
                if (MoveTimer < 120)
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

            if (deathAnimationProgress > 0) //If dying
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
