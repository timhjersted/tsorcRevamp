using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class ExampleBoss : BossBase
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            //Calling base.SetDefaults takes care of a lot of variables that all bosses share
            //Things like making them hostile, marking them as a boss, etc
            base.SetDefaults();

            //The rest are unique to this specific boss, and we have to set here:
            NPC.width = 100;
            NPC.height = 100;
            NPC.scale = 2;
            NPC.damage = 50;
            NPC.defense = 35;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 325000;
            NPC.timeLeft = 22500;
            NPC.value = 600000;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.ExampleBoss.DespawnHandler"), Color.Cyan, 180);

            //You can also specify BossBase specific values here
            introDuration = 120;
            attackTransitionDuration = 60;
            phaseTransitionDuration = 120;
            deathAnimationDuration = 180;
        }


        /// <summary>
        /// Add all the moves and damage numbers for your boss in here!
        /// You need the function name, the time the attack lasts, and optionally can specify a color (for use with things like VFX or lighting)
        /// </summary>
        public override void InitializeMovesAndDamage()
        {
            //Create a new function for every move of your boss, and then add them to this list alongside the duration of th attack
            MoveList = new List<BossMove> {
                new BossMove(ExampleAttack1, 700),
                new BossMove(ExampleAttack2, 600),
                new BossMove(ExampleAttack3, 900),
                };


            //Set the damage numbers for every attack or projectile your boss uses here
            //Remember: Contact damage is doubled, and projectile damage is multiplied by 4!
            DamageNumbers = new Dictionary<string, int>
            {
                ["PlasmaOrb"] = 65,
                ["BlackFire"] = 35,
                ["DeathLaser"] = 55,
                ["BaseContact"] = 140,
                ["Charging"] = 200,
            };
        }

        //An example attack, for demonstration purposes
        //If you're re-using a number a lot, it's good practice to declare it on top like this
        //That means if you ever want to change it or test another, you only have to change it in one place
        int chargeDelay = 90;
        public void ExampleAttack1()
        {
            //When the attack starts, increase its contact damage to the "Charging" value
            NPC.damage = DamageNumbers["Charging"];

            //Every 90 frames (chargeDelay), dash at the player and play a sound
            if (MoveTimer % chargeDelay == 0)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);
                NPC.velocity = UsefulFunctions.Aim(NPC.Center, Target.Center, 20);
            }

            //45 frames after dashing, slow to a stop
            if (MoveTimer % chargeDelay > 45)
            {
                NPC.velocity *= 0.9f;
            }

            //45 frames after dashing, emit a hexagon of projectiles if it is in its second phase
            if (MoveTimer % chargeDelay == 45 && Phase == 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float projectilesInRing = 6;
                    for (int i = 0; i < projectilesInRing; i++)
                    {
                        Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / projectilesInRing);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.Okiku.EnemyAttraidiesBlackFire>(), DamageNumbers["BlackFire"], 0, Main.myPlayer, -1);
                    }
                }
            }

            //On the final frame of the attack, reset its contact damage to the normal value
            if (MoveTimer == CurrentMove.timeLimit)
            {
                NPC.damage = DamageNumbers["BaseContact"];
            }
        }

        //A second example attack, for demonstration purposes
        public void ExampleAttack2()
        {
            //Smoothly accelerate toward the player
            UsefulFunctions.SmoothHoming(NPC, Target.Center, 0.05f, 20);

            //Fire a laser every 5 frames
            if (MoveTimer % 5 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, TargetVector * 15, ModContent.ProjectileType<Projectiles.Enemy.Triad.RetDeathLaser>(), DamageNumbers["DeathLaser"], 0, Main.myPlayer, -1);
                }
            }

            //In the second phase, also fire a hexagon of dark projectiles
            if (MoveTimer % 30 == 0 && Phase == 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float projectilesInRing = 6;
                    for (int i = 0; i < projectilesInRing; i++)
                    {
                        Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / projectilesInRing);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.Okiku.EnemyAttraidiesBlackFire>(), DamageNumbers["BlackFire"], 0, Main.myPlayer, -1);
                    }
                }
            }

        }

        //A majestic kangaroo. Just kidding: It's yet another example attack, for demonstration purposes
        public void ExampleAttack3()
        {
            NPC.velocity *= 0.95f;
            if (MoveTimer % 2 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(10, 10), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), DamageNumbers["PlasmaOrb"], 0, Main.myPlayer, -1);
                }
            }

            //In the second phase, also occasionally fire a laser at the player
            if (Phase == 1 && MoveTimer % 60 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, TargetVector * 15, ModContent.ProjectileType<Projectiles.Enemy.Triad.RetDeathLaser>(), DamageNumbers["DeathLaser"], 0, Main.myPlayer, -1);
                }
            }
        }

        /// <summary>
        /// Controls what this boss does during its intro
        /// </summary>
        public override void HandleIntro()
        {
            //Spawn a shockwave at the start
            if (introTimer == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 500, 60);
                }
                attackTransitionTimeRemaining = attackTransitionDuration;
            }

            //Spawn projectiles continuously
            if (introTimer % 2 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), DamageNumbers["PlasmaOrb"], 0, Main.myPlayer, -1);
                }
            }

            //And spawn a shockwave at the end
            if (introTimer == introDuration)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 500, 60);
                }
                attackTransitionTimeRemaining = attackTransitionDuration;
            }
        }

        /// <summary>
        /// If your boss has multiple phases or does things at certain health percents, it goes in here
        /// In this example, if it drops below half health and is still in the first phase, it moves to the second and activates its transition timer
        /// </summary>
        public override void HandleLife()
        {
            if (NPC.life < NPC.lifeMax / 2f && Phase == 0)
            {
                NextPhase();
            }
        }

        /// <summary>
        /// Override this to make things happen when this boss is killed
        /// In this case, it spawns a bunch of shockwaves with 20 frames between them
        /// </summary>
        public override void HandleDeath()
        {
            NPC.velocity *= 0.95f;

            if (deathAnimationProgress % 20 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + Main.rand.NextVector2Circular(100, 100), Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 200, 40);
                }
            }

            //The base class handles actually killing the NPC when the timer runs out
            base.HandleDeath();
        }

        /// <summary>
        /// Override this to make the boss do things while transitioning between attacks
        /// In this example, it spawns some dust
        /// </summary>
        public override void AttackTransition()
        {
            NPC.velocity *= 0.95f;
            Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2CircularEdge(100, 100), DustID.ShadowbeamStaff, Main.rand.NextVector2Circular(10, 10), Scale: 3);
        }

        /// <summary>
        /// Override this to make the boss do things while transitioning between phases
        /// In this example, it spawns some dust and spawns a shockwave every time it's another third of the way through the phase transition
        /// </summary>
        public override void PhaseTransition()
        {
            NPC.velocity *= 0.95f;
            Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2CircularEdge(100, 100), DustID.ShadowbeamStaff, Main.rand.NextVector2Circular(10, 10), Scale: 5);
            if (phaseTransitionTimeRemaining % (phaseTransitionDuration / 3f) == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 500, 60);
                }
            }
        }

        /// <summary>
        /// Override this to add custom VFX to your boss
        /// </summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }


        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
    }
}