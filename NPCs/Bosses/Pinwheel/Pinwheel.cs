using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;
using Terraria.UI;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Bosses.Pinwheel
{
    [AutoloadBossHead]

    internal class Pinwheel : BossBase
    {

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 17;
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
            NPC.lifeMax = 1000;
            NPC.timeLeft = 180;
            NPC.value = 500;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.ExampleBoss.DespawnHandler"), Color.Cyan, 180);

            //Terraria.GameContent.UI.BigProgressBar.IBigProgressBar bossBar;
            //Main.BigBossProgressBar.TryGetSpecialVanillaBossBar(NPC.netID, out bossBar);


            //You can also specify BossBase specific values here
            introDuration = 120;
            attackTransitionDuration = 120;
            //phaseTransitionDuration = 120;
            deathAnimationDuration = 60;
            randomAttacks = true;
        }

        public bool isClone = false;
        bool cloneSpawned = false;

        public int mainBossIndex;
        bool gotMainBossIndex = false;

        public override void AI()
        {
            //Main.NewText(attackTransitionTimeRemaining);
            if (NPC.ai[3] == 1)
            {
                introDuration = 60;
                isClone = true;
                NPC.DeathSound = SoundID.NPCDeath6;
                NPC.lifeMax = 200;
                NPC.timeLeft = 180;
                NPC.value = 0;
                NPC.boss = false;
                //despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.ExampleBoss.DespawnHandler"), Color.Cyan, 180);

                if (!cloneSpawned) //This allows us to bring current hp down to 200 as soon as it spawns
                {
                    NPC.life = 200;
                    cloneSpawned = true;
                }
            }

            if (!gotMainBossIndex) //Get the index of the main boss, to be able to make clones behave based on what main boss does
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
                NPC.life = 1;
            }

            if (!isClone)
            {
                //Main.NewText(NPC.ai[0]); //AI 0 is NextMove

                if (NPC.CountNPCS(ModContent.NPCType<Pinwheel>()) < 3 && NPC.ai[0] != 0 && attackTransitionTimeRemaining == 30) //If there are less than 3 Pinwheels and it wasn-t about to spawn clones anyway. Check transition time so as not to interrupt current attack
                {
                    MoveIndex = PinwheelAttackID.CreateClonesID; //Then spawn clones, while not skipping what attack was coming next anyway
                    //MoveTimer = 0;
                }
            }


            despawning = despawnHandler.TargetAndDespawn(NPC.whoAmI);
            HandleLife();
            Rotate();

            //If the move list has not been initialized then do so
            if (MoveList == null)
            {
                InitializeMovesAndDamage();
                do
                {
                    NextMoveIndex = Main.rand.Next(MoveList.Count);
                } while (NextMoveIndex != 0);

                //Create the 'used move list'
                UsableMoveList = new List<BossMove>();
                for (int i = 0; i < MoveList.Count; i++)
                {
                    UsableMoveList.Add(MoveList[i]);
                }

                //Remove the two moves already picked (0 is always the first move)
                UsableMoveList.RemoveAt(0);
                UsableMoveList.RemoveAt(NextMoveIndex);
            }

            //If it's doing an attack or phase transition, then do nothing else until it's done
            if (attackTransitionTimeRemaining > 0)
            {
                AttackTransition();
                attackTransitionTimeRemaining--;
                return;
            }

            //If it's doing an attack or phase transition, then do nothing else until it's done
            if (phaseTransitionTimeRemaining > 0)
            {
                PhaseTransition();
                phaseTransitionTimeRemaining--;
                return;
            }

            //If it hasn't finished its intro, then do nothing else until it's done
            if (introTimer <= introDuration)
            {
                HandleIntro();
                introTimer++;
                return;
            }

            //If the NPC is dying, then do nothing else until it's done
            if (NPC.life == 1)
            {
                if (deathAnimationProgress <= deathAnimationDuration)
                {
                    HandleDeath();
                    deathAnimationProgress++;
                    return;
                }
            }

            //Otherwise, perform its current move normally
            PerformMove();
        }

        //Useful code from old AI to check if it's on the ground.
        bool OnGround()
        {
            bool standing_on_solid_tile = false;

            int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
            int x_left_edge = (int)NPC.position.X / 16;
            int x_right_edge = (int)(NPC.position.X + (float)NPC.width) / 16;
            for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
            {
                Tile t = Main.tile[l, y_below_feet];
                if (t.HasTile && !t.IsActuated && Main.tileSolid[(int)t.TileType]) // tile exists and is solid
                {
                    standing_on_solid_tile = true;
                    break; // one is enough so stop checking
                }
            } // END traverse blocks under feet
            return standing_on_solid_tile;
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
                new BossMove(CreateClones, 300, id : PinwheelAttackID.CreateClonesID), //Always plays twice at the start, not what I want
                new BossMove(VolcanicEruption, 370, id : PinwheelAttackID.VolcanicEruptionID), //Move in index 1 of this list doesn't run till in second movepool loop
                new BossMove(Flamethrower, 420, id : PinwheelAttackID.FlamethrowerID),
                new BossMove(BouncingFireball, 180, id: PinwheelAttackID.BouncingFireballID),
                new BossMove(KillableFireball, 150, id: PinwheelAttackID.KillableFireballID),
            };


            //Set the damage numbers for every attack or projectile your boss uses here
            //Remember: Contact damage is doubled, and projectile damage is multiplied by 4!
            DamageNumbers = new Dictionary<string, int>
            {
                ["BouncingFireballDamage"] = 14,
                ["KillableFireballDamage"] = 40,
                ["FlamethrowerDamage"] = 10,
                ["VolcanicEruptionDamage"] = 16,
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
            if (NPC.wet) { NPC.velocity.Y = -3.5f; }

            float num48 = 5f;
            Vector2 vector8 = new Vector2(NPC.Center.X, NPC.Center.Y - 30);
            Vector2 shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 2);
            float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X);
            float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y);


            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
            num51 = num48 / num51;
            speedX *= num51;
            speedY *= num51;

            if (MoveTimer > 20)
            {
                UsefulFunctions.DustRing(vector8, 5, 6);
            }

            if (MoveTimer == 90 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
            {

                int type = ProjectileID.Fireball;
                int shot1 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, DamageNumbers["BouncingFireballDamage"], 0f, Main.myPlayer, 0, 0);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                Main.projectile[shot1].friendly = false;
                Main.projectile[shot1].hostile = true;
            }

            if (MoveTimer == 120 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
            {
                int type = ProjectileID.Fireball;
                int shot1 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, DamageNumbers["BouncingFireballDamage"], 0f, Main.myPlayer, 0, 0);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                Main.projectile[shot1].friendly = false;
                Main.projectile[shot1].hostile = true;
            }

            if (MoveTimer == 150 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot bouncing fireball
            {
                int type = ProjectileID.Fireball;
                int shot1 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, DamageNumbers["BouncingFireballDamage"], 0f, Main.myPlayer, 0, 0);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                Main.projectile[shot1].friendly = false;
                Main.projectile[shot1].hostile = true;
            }
        }

        public void KillableFireball()
        {
            NPC.velocity.X *= 0f;
            if (NPC.wet) { NPC.velocity.Y = -3.5f; }

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
                int shot1 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternTopLeft.X, (int)lanternTopLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot1].damage = DamageNumbers["KillableFireballDamage"];
                int shot2 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternMiddleRight.X, (int)lanternMiddleRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot2].damage = DamageNumbers["KillableFireballDamage"];
                int shot3 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternBottomLeft.X, (int)lanternBottomLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot3].damage = DamageNumbers["KillableFireballDamage"];
                int shot4 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternTopRight.X, (int)lanternTopRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot4].damage = DamageNumbers["KillableFireballDamage"];
                int shot5 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternMiddleLeft.X, (int)lanternMiddleLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot5].damage = DamageNumbers["KillableFireballDamage"];
                int shot6 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternBottomRight.X, (int)lanternBottomRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot6].damage = DamageNumbers["KillableFireballDamage"];
            }

            if (MoveTimer == 120 && Main.netMode != NetmodeID.MultiplayerClient) //Shoot killable Gaibon fireballs
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                int shot1 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternTopLeft.X, (int)lanternTopLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot1].damage = DamageNumbers["KillableFireballDamage"];
                int shot2 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternMiddleRight.X, (int)lanternMiddleRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot2].damage = DamageNumbers["KillableFireballDamage"];
                int shot3 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternBottomLeft.X, (int)lanternBottomLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot3].damage = DamageNumbers["KillableFireballDamage"];
                int shot4 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternTopRight.X, (int)lanternTopRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot4].damage = DamageNumbers["KillableFireballDamage"];
                int shot5 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternMiddleLeft.X, (int)lanternMiddleLeft.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot5].damage = DamageNumbers["KillableFireballDamage"];
                int shot6 = NPC.NewNPC(NPC.GetSource_FromThis(), (int)lanternBottomRight.X, (int)lanternBottomRight.Y, ModContent.NPCType<GaibonFireball>(), 0, 0, shootSpeed.X, shootSpeed.Y);
                Main.npc[shot6].damage = DamageNumbers["KillableFireballDamage"];
            }

        }

        public void Flamethrower()
        {
            NPC.velocity.X *= 0f;
            if (NPC.wet) { NPC.velocity.Y = -3.5f; }

            int dustQuantity = (int)MoveTimer / 6;
            for (int i = 0; i < dustQuantity; i++)
            {
                if (Main.rand.Next(20) == 0 && MoveTimer > 40 && MoveTimer < 120)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 42), 10, 10, 6, 0, 0, 0, default(Color), 1.5f);
                }
            }

            if (MoveTimer >= 120 && MoveTimer < 300 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vector8 = new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30);
                Vector2 shootSpeed = UsefulFunctions.Aim(new Vector2(NPC.Center.X - 5, NPC.Center.Y - 30), Target.Center, 1f);
                for (int i = 0; i < 3; i++)
                {
                    int shot1 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, shootSpeed.X * Main.rand.NextFloat(7, 10), shootSpeed.Y * Main.rand.NextFloat(5, 10), ModContent.ProjectileType<Projectiles.Enemy.SmallFlameJet>(), DamageNumbers["FlamethrowerDamage"], 0f, Main.myPlayer);
                    Main.projectile[shot1].timeLeft = 50;
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

            NPC.velocity *= 0f;

            if (MoveTimer == 200)
            {
                Vector2 leftSide = new Vector2(NPC.Center.X - 250, NPC.Center.Y);
                Vector2 rightSide = new Vector2(NPC.Center.X + 250, NPC.Center.Y);

                //set ai[3] to 1, so it has isClone set to true
                int cloneLeft = NPC.NewNPC(NPC.GetSource_FromThis(), (int)leftSide.X, (int)leftSide.Y, ModContent.NPCType<Pinwheel>(), 0, 1, 0, 0, 1);
                Main.npc[cloneLeft].boss = false;
                int cloneRight = NPC.NewNPC(NPC.GetSource_FromThis(), (int)rightSide.X, (int)rightSide.Y, ModContent.NPCType<Pinwheel>(), 0, 4, 0, 0, 1);
                Main.npc[cloneRight].boss = false;

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
            if (introTimer == 30)
            {
                NextMove(); //Skip first move, which would have been a duplicate CreateClones()
                NextMove();
                NextMove();
            }

        }

        public override void HandleLife()
        {          
            base.HandleLife();
        }

        public override void HandleDeath()
        {
            base.HandleDeath();
        }

        public override void AttackTransition() 
        {
            if (NPC.wet) { NPC.velocity.Y = -3.5f; } //Float if in water
            UsefulFunctions.SmoothHoming(NPC, Target.Center, 0.01f, 0.15f);
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
                    Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                }
            }

            #endregion


            #region Killable Fireballs Attack Dusts


            if (MoveIndex == PinwheelAttackID.KillableFireballID && NPC.frame.Y == 16 * 234) //If arms up, on frame 16 (attacking frame, etc). 234 is frameheight
            {
                if (Main.rand.NextBool(4)) //Fire
                {
                    Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                }
            }

            #endregion


            #region Flamethrower Attack Dusts

            if (MoveIndex == PinwheelAttackID.FlamethrowerID && MoveTimer >= 20 && MoveTimer < 420)
            {

                if (Main.rand.NextBool(6)) //Fire
                {
                    Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
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
                    Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 40f, -20f, 50, default(Color), 2f)];
                    dust1.noGravity = true;
                    dust1.velocity /= 8;
                    dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 47f, 13f, 50, default(Color), 2f)];
                    dust2.noGravity = true;
                    dust2.velocity /= 8;
                    dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, 40f, 50, default(Color), 2f)];
                    dust3.noGravity = true;
                    dust3.velocity /= 8;
                    dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, 40f, 50, default(Color), 2f)];
                    dust4.noGravity = true;
                    dust4.velocity /= 8;
                    dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -47f, 13f, 50, default(Color), 2f)];
                    dust5.noGravity = true;
                    dust5.velocity /= 8;
                    dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -40, -20, 50, default(Color), 2f)];
                    dust6.noGravity = true;
                    dust6.velocity /= 8;
                    dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                }

                else
                {
                    for (int i = 0; i < dustQuantity; i++) //Flare
                    {
                        Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 40f, -20f, 50, default(Color), 2f)];
                        dust1.noGravity = true;
                        dust1.velocity /= 8;
                        dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 47f, 13f, 50, default(Color), 2f)];
                        dust2.noGravity = true;
                        dust2.velocity /= 8;
                        dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, 40f, 50, default(Color), 2f)];
                        dust3.noGravity = true;
                        dust3.velocity /= 8;
                        dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, 40f, 50, default(Color), 2f)];
                        dust4.noGravity = true;
                        dust4.velocity /= 8;
                        dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -47f, 13f, 50, default(Color), 2f)];
                        dust5.noGravity = true;
                        dust5.velocity /= 8;
                        dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -40, -20, 50, default(Color), 2f)];
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
                    Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                    Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
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
                    Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 4f, -45f, 50, default(Color), 2f)];
                    dust1.noGravity = true;
                    dust1.velocity /= 10;
                    dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 27f, -45f, 50, default(Color), 2f)];
                    dust2.noGravity = true;
                    dust2.velocity /= 10;
                    dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, -20f, 50, default(Color), 2f)];
                    dust3.noGravity = true;
                    dust3.velocity /= 10;
                    dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, -20f, 50, default(Color), 2f)];
                    dust4.noGravity = true;
                    dust4.velocity /= 10;
                    dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -27f, -45f, 50, default(Color), 2f)];
                    dust5.noGravity = true;
                    dust5.velocity /= 10;
                    dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                    Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -4, -45, 50, default(Color), 2f)];
                    dust6.noGravity = true;
                    dust6.velocity /= 10;
                    dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                }

                else
                {
                    for (int i = 0; i < dustQuantity; i++) //Flare
                    {
                        Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 127, 4f, -45f, 50, default(Color), 2f)];
                        dust1.noGravity = true;
                        dust1.velocity /= 10;
                        dust1.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 127, 27f, -45f, 50, default(Color), 2f)];
                        dust2.noGravity = true;
                        dust2.velocity /= 10;
                        dust2.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 127, 20f, -20f, 50, default(Color), 2f)];
                        dust3.noGravity = true;
                        dust3.velocity /= 10;
                        dust3.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 127, -20f, -20f, 50, default(Color), 2f)];
                        dust4.noGravity = true;
                        dust4.velocity /= 10;
                        dust4.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 127, -27f, -45f, 50, default(Color), 2f)];
                        dust5.noGravity = true;
                        dust5.velocity /= 10;
                        dust5.velocity *= Main.rand.NextFloat(0, flameSpeedPower);

                        Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 127, -4, -45, 50, default(Color), 2f)];
                        dust6.noGravity = true;
                        dust6.velocity /= 10;
                        dust6.velocity *= Main.rand.NextFloat(0, flameSpeedPower);
                    }
                }
                if (dustQuantity == 2 && Main.rand.NextBool(2))
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(NPC.Center.X - 20, NPC.Center.Y - 140), 40, 10, 127, 0, -25, 50, default(Color), 2f)];
                    dust.noGravity = true;
                    dust.velocity /= 10;
                    dust.velocity *= Main.rand.NextFloat(5, 10);
                }
            }

            #endregion



            else //Dusts while not attacking. Separate randomizations so the sparse dusts look natural
            {
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust1 = Main.dust[Dust.NewDust(lanternBottomLeft + new Vector2(-4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust2 = Main.dust[Dust.NewDust(lanternMiddleLeft + new Vector2(4, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust3 = Main.dust[Dust.NewDust(lanternTopLeft + new Vector2(6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust4 = Main.dust[Dust.NewDust(lanternTopRight + new Vector2(-6, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust5 = Main.dust[Dust.NewDust(lanternMiddleRight + new Vector2(-2, -10), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                }
                if (Main.rand.NextBool(60)) //Fire
                {
                    Dust dust6 = Main.dust[Dust.NewDust(lanternBottomRight + new Vector2(4, -24), 8, 10, 6, 0, 0, 50, default(Color), 1f)];
                }
            }


            #endregion


            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Npc[NPC.type];

            //Draw "clones"
            if (MoveIndex == PinwheelAttackID.CreateClonesID)
            {
                Main.NewText(cloneTimer);

                if (MoveTimer > 28 && MoveTimer <= 120)
                {
                    cloneTimer++;
                    cloneOffset = cloneTimer / 15;
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.Gold, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.Gold, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                }
                if (MoveTimer > 120 && MoveTimer <= 200)
                {
                    cloneTimer += 2;
                    cloneOffset = cloneTimer - 108;
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.Gold, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.Gold, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                }
                if (MoveTimer > 200 && MoveTimer <= 260)
                {
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition - new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.Gold, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition + new Vector2(cloneOffset, 0), new Rectangle(0, NPC.frame.Y, 198, 234), Color.Gold, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
                }
                if (MoveTimer > 260)
                {
                    cloneTimer = 0;
                }
            }

            //Draw main boss texture
            Main.EntitySpriteDraw(texture, NPC.position - Main.screenPosition, new Rectangle(0, NPC.frame.Y, 198, 234), lightColor, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);

            return false;
        }


        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            //Glow mask for fire in lanterns
            Texture2D fireTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Bosses/Pinwheel/Pinwheel_Fireglow");
            spriteBatch.Draw(fireTexture, NPC.position - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Rectangle(0, NPC.frame.Y, 198, 234), Color.White, NPC.rotation, new Vector2(69, 75), NPC.scale, SpriteEffects.None, 0);
        }


        public override void FindFrame(int frameHeight)
        {
            // To be changed
            #region Intro animation

            if (introTimer < introDuration)
            {
                NPC.frameCounter++;

                if (NPC.frameCounter < 4)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (NPC.frameCounter < 8)
                {
                    NPC.frame.Y = 1 * frameHeight;
                }
                else if (NPC.frameCounter < 12)
                {
                    NPC.frame.Y = 2 * frameHeight;
                }
                else if (NPC.frameCounter < 16)
                {
                    NPC.frame.Y = 3 * frameHeight;
                }
                else if (NPC.frameCounter < 20)
                {
                    NPC.frame.Y = 4 * frameHeight;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = 5 * frameHeight;
                }
                else if (NPC.frameCounter < 28)
                {
                    NPC.frame.Y = 6 * frameHeight;
                }
                else if (NPC.frameCounter < 32)
                {
                    NPC.frame.Y = 7 * frameHeight;
                }
                else if (NPC.frameCounter < 36)
                {
                    NPC.frame.Y = 8 * frameHeight;
                }
                else if (NPC.frameCounter < 40)
                {
                    NPC.frame.Y = 9 * frameHeight;
                }
                else if (NPC.frameCounter < 44)
                {
                    NPC.frame.Y = 10 * frameHeight;
                }
                else if (NPC.frameCounter < 48)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (NPC.frameCounter < 52)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (NPC.frameCounter < 56)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (NPC.frameCounter < 60)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else if (NPC.frameCounter < 64)
                {
                    NPC.frame.Y = 15 * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }

            #endregion


            if (attackTransitionTimeRemaining < attackTransitionDuration && MoveTimer == 0 && introTimer >= introDuration)
            {
                NPC.frameCounter++;

                if (NPC.frameCounter < 22)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (NPC.frameCounter < 44)
                {
                    NPC.frame.Y = 1 * frameHeight;
                }
                else if (NPC.frameCounter < 66)
                {
                    NPC.frame.Y = 2 * frameHeight;
                }
                else if (NPC.frameCounter < 88)
                {
                    NPC.frame.Y = 3 * frameHeight;
                }
                else if (NPC.frameCounter < 110)
                {
                    NPC.frame.Y = 4 * frameHeight;
                }
                else if (NPC.frameCounter < 132)
                {
                    NPC.frame.Y = 5 * frameHeight;
                }
                else if (NPC.frameCounter < 154)
                {
                    NPC.frame.Y = 6 * frameHeight;
                }
                else if (NPC.frameCounter < 176)
                {
                    NPC.frame.Y = 7 * frameHeight;
                }
                else if (NPC.frameCounter < 198)
                {
                    NPC.frame.Y = 8 * frameHeight;
                }
                else if (NPC.frameCounter < 220)
                {
                    NPC.frame.Y = 9 * frameHeight;
                }
                else if (NPC.frameCounter < 242)
                {
                    NPC.frame.Y = 10 * frameHeight;
                }
                else if (NPC.frameCounter < 264)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (NPC.frameCounter < 286)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (NPC.frameCounter < 308)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (NPC.frameCounter < 330)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else if (NPC.frameCounter < 352)
                {
                    NPC.frame.Y = 15 * frameHeight;
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
                NPC.frameCounter++;

                if (MoveTimer < 20)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (MoveTimer < 300)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
            }

            if (MoveIndex == PinwheelAttackID.VolcanicEruptionID && MoveTimer != 0 && MoveTimer <= 280)
            {
                NPC.frameCounter++;

                if (MoveTimer < 20)
                {
                    NPC.frame.Y = 0 * frameHeight;
                }
                else if (MoveTimer < 280)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
            }
        }

        #endregion


    }
}
