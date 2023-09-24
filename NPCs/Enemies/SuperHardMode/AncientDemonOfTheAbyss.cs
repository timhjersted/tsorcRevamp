using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class AncientDemonOfTheAbyss : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            AnimationType = 28;
            NPC.height = 120;
            NPC.width = 50;
            NPC.damage = 110;
            NPC.defense = 70;
            NPC.lifeMax = 15000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;

            NPC.value = 60000; // life / 2.5 : was 30,075!
            NPC.knockBackResist = 0;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.AncientDemonOfTheAbyssBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.AncientDemonOfTheAbyss.DespawnHandler"), Color.Gold, DustID.GoldFlame);
        }

        NPCDespawnHandler despawnHandler;
        int meteorDamage = 16;
        int cultistFireDamage = 46;
        int cultistMagicDamage = 145;
        int cultistLightningDamage = 100;
        int fireBreathDamage = 76;
        int lostSoulDamage = 112;


        int greatFireballDamage = 133;
        int blackFireDamage = 79;
        int greatAttackDamage = 81;


        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Poisoned, 20 * 60, false);
            target.AddBuff(BuffID.Bleeding, 20 * 60, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 300 * 60, false); //reduced defense on hit
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 300 * 60, false); //-20 HP after several hits
            target.GetModPlayer<tsorcRevampPlayer>().CurseLevel += 30;
        }


        //Spawns in Lower Cavern into the Underworld. Spawns more under 2.5/10th and again after 7.5/10th (Length). Spawns in Super Hardmode. Will not spawn if there are more than 2 Town NPCs nearby (or if a Blood Moon).

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool oMagmaCavern = (spawnInfo.Player.position.Y >= (Main.maxTilesY * 0.6f) && !spawnInfo.Player.ZoneUnderworldHeight);
            bool BeforeThreeAfterSeven = (spawnInfo.Player.position.X < Main.maxTilesX * 0.3f) || (spawnInfo.Player.position.X > Main.maxTilesX * 0.7f); //Before 3/10ths or after 7/10ths width

            float chance = 0;
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (spawnInfo.Player.ZoneUnderworldHeight)
                {
                    chance = 0.002f;

                    if (BeforeThreeAfterSeven)
                    {
                        chance *= 2;
                    }
                }
                if (oMagmaCavern)
                {
                    chance = 0.001f;
                }
            }

            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }
        #endregion

        int intspawnedSpirits = 0;

        public Player player
        {
            get => Main.player[NPC.target];
        }
        //PROJECTILE HIT LOGIC
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.FighterOnHit(NPC, true);

            //JUSTHIT CODE
            //MELEE RANGE
            if (NPC.Distance(player.Center) < 100 && NPC.localAI[1] < 70f) 
            {
                NPC.localAI[1] = 50f;

                //TELEPORT MELEE
                if (Main.rand.NextBool(12))
                {
                    tsorcRevampAIs.QueueTeleport(NPC, 25, true, 60);
                }
            }
            //RISK ZONE
            if (NPC.Distance(player.Center) < 300 && NPC.localAI[1] < 70f && Main.rand.NextBool(5))
            {
                NPC.velocity.Y = Main.rand.NextFloat(-5f, -3f); //was 6 and 3
                float v = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-10f, -7f);
                NPC.velocity.X = v;
                NPC.netUpdate = true;
            }

        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {

            //TELEPORT RANGED
            if (Main.rand.NextBool(26))
            {
                tsorcRevampAIs.QueueTeleport(NPC, 20, true, 60);
                NPC.localAI[1] = 70f;
            }
            //RANGED
            if (NPC.Distance(player.Center) > 201 && NPC.velocity.Y == 0f && Main.rand.NextBool(3))
            {

                NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(11f, 8f);
                NPC.netUpdate = true;

            }
        }



        //int breathTimer gives weird cool arrow shape, float does the circle
        int breathTimer = 0;
        int spawnedDemons = 0;
        public override void AI()
        {

            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            int choice = Main.rand.Next(6);


            //CHANCE TO JUMP BEFORE ATTACK  
            if (NPC.localAI[1] == 140 && NPC.velocity.Y == 0f && Main.rand.NextBool(40) && NPC.life >= NPC.lifeMax / 3)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-9f, -6f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                NPC.netUpdate = true;
            }

            if (NPC.localAI[1] == 140 && NPC.velocity.Y == 0f && Main.rand.NextBool(33) && NPC.life <= NPC.lifeMax / 3)
            {
                NPC.velocity.Y = Main.rand.NextFloat(-7f, -4f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(2f, 1f);
                NPC.netUpdate = true;

            }

            //play creature sounds
            if (Main.rand.NextBool(1700))
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.5f }, NPC.Center);
                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
            }

            NPC.localAI[1]++;
            //FIGHTER AI
            bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
            tsorcRevampAIs.FighterAI(NPC, 1, 0.1f, canTeleport: true, lavaJumping: true, enragePercent: 0.2f, enrageTopSpeed: 2, canDodgeroll: false);

            if (NPC.localAI[1] >= 179)
            {
                if (Main.rand.NextBool(150))
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, player.Center, 0.1f), ProjectileID.CultistBossFireBallClone, cultistMagicDamage, 0.1f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17);
                    NPC.localAI[1] = 0;
                }
                if (Main.rand.NextBool(20))
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, player.Center, 0.1f), ProjectileID.CultistBossFireBall, cultistFireDamage, 0.1f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit34);
                    NPC.localAI[1] = 0;
                }
            }

            //EARLY TELEGRAPH
            if (NPC.localAI[1] >= 60)
            {
                Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(6))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoblinSorcerer, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoblinSorcerer, NPC.velocity.X, NPC.velocity.Y); //pink dusts
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoblinSorcerer, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoblinSorcerer, NPC.velocity.X, NPC.velocity.Y); //pink dusts


                }
            }
            //LAST SECOND TELEGRAPH
            if (NPC.localAI[1] >= 110)
            {
                Lighting.AddLight(NPC.Center, Color.DeepPink.ToVector3() * 5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y); //pink dusts
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y);
                }
            }

            if (breathTimer == 350 && Main.rand.NextBool(3))
            {
                breathTimer = 1;
            }
            // NEW BREATH ATTACK 
            breathTimer++;
            if (breathTimer > 480)
            {
                NPC.localAI[1] = -50;
                if (NPC.life >= NPC.lifeMax / 3)
                { breathTimer = -70; }
                if (NPC.life <= NPC.lifeMax / 3)
                { breathTimer = -160; }

            }

            if (breathTimer == 470)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f }, NPC.Center); //shadowflame hex (little beasty)
            }

            if (breathTimer < 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //npc.velocity.Y = -1.1f;
                    NPC.velocity.Y = Main.rand.NextFloat(-4f, -1.1f);
                    NPC.velocity.X = 0f;

                    //play breath sound
                    if (Main.rand.NextBool(3))
                    {

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //flame thrower
                    }

                    Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].OldPos(9), 9);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);


                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y - 40f, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
                    NPC.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                    NPC.localAI[1] = -50;
                }
            }

            if (breathTimer == 361)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.5f }, NPC.Center);
            }
            if (breathTimer > 360)
            {
                NPC.localAI[1] = -50;
                UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.Torch, 48, 4);
                Lighting.AddLight(NPC.Center * 2, Color.Red.ToVector3() * 5);
            }

            if (breathTimer == 0)
            {
                NPC.localAI[1] = -150;

                NPC.velocity.X = 0f;

            }

            //PLAYER RUNNING AWAY? SPAWN DesertDjinnCurse, 
            Player player3 = Main.player[NPC.target];
            if (Main.rand.NextBool(30) && NPC.Distance(player3.Center) > 700)
            {
                Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8f, 1.06f, true, true);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ProjectileID.DesertDjinnCurse, lostSoulDamage, 7f, Main.myPlayer);

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center); //wobble
                NPC.localAI[1] = 1f;

                NPC.netUpdate = true;
            }


            //SPAWN FIRE LURKER
            if ((spawnedDemons < 6) && NPC.life >= NPC.lifeMax / 15 && Main.rand.NextBool(3000))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.FireLurker>(), 0);
                    Main.npc[Spawned].velocity.Y = -8;
                    spawnedDemons++;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
            }






            //CHOICES
            if (NPC.localAI[1] >= 160f && (choice == 0 || choice == 4) && NPC.life >= NPC.lifeMax / 3)
            {
                bool clearSpace = true;
                for (int i = 0; i < 15; i++)
                {
                    if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i))
                    {
                        clearSpace = false;
                    }
                }
                //LOB ATTACK PURPLE; 
                if (NPC.life >= NPC.lifeMax / 10 && NPC.life <= NPC.lifeMax / 3 * 2 && clearSpace)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);

                    speed.Y += Main.rand.NextFloat(-2f, -6f);
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int lob2 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, fireBreathDamage, 0f, Main.myPlayer);

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -.5f }, NPC.Center);

                    }
                    if (NPC.localAI[1] >= 195f)
                    { NPC.localAI[1] = 1f; }
                }
                //LOB ATTACK >> BOUNCING FIRE
                if (NPC.life >= NPC.lifeMax / 15 && clearSpace)

                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);
                    speed.Y += Main.rand.NextFloat(2f, -2f);
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, fireBreathDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                        if (NPC.localAI[1] >= 186f)
                        { NPC.localAI[1] = 1f; }
                    }

                }

            }

            NPC.TargetClosest(true);


            //MULTI-FIRE 1 ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life >= NPC.lifeMax / 3 && choice == 1)
            {

                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(4), 7);
                //speed.Y += Main.rand.NextFloat(2f, -2f); //just added
                if (Main.rand.NextBool(3) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                }

                if (NPC.localAI[1] >= 175f)
                {
                    NPC.localAI[1] = 1f;
                }
                NPC.netUpdate = true;
            }
            //MULTI-BOUNCING DESPERATE FIRE ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life <= NPC.lifeMax / 3 && (choice == 1 || choice == 2))
            {
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 3);
                speed.Y += Main.rand.NextFloat(2f, -2f);
                if (Main.rand.NextBool(2) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, cultistFireDamage, 3f, Main.myPlayer); //5f was 0f in the example that works
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center); //fire

                }

                if (NPC.localAI[1] >= 190f) //was 126
                {
                    NPC.localAI[1] = 1f;
                }
                NPC.netUpdate = true;
            }
            //LIGHTNING ATTACK
            if (NPC.localAI[1] == 160f && NPC.life >= NPC.lifeMax / 3 && NPC.life <= NPC.lifeMax / 3 * 2 && (choice == 5 || choice == 4)) //&& Main.rand.NextBool(8) 
            {
                //&& Main.rand.NextBool(10) Main.rand.NextBool(2) &&
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(1), 1);
                //speed += Main.player[npc.target].velocity / 4;

                speed.Y += Main.rand.NextFloat(-2, -5f);//was -2, -6


                if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.CultistBossLightningOrb, cultistLightningDamage, 0f, Main.myPlayer);
                    //ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()


                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                }

                NPC.localAI[1] = -50f;


            }

            /*JUMP DASH FOR FINAL
			if (npc.localAI[1] == 140 && npc.velocity.Y == 0f && Main.rand.NextBool(20) && npc.life <= NPC.lifeMax / 3)
			{
				int dust2 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f);
				Main.dust[dust2].noGravity = true;
				npc.velocity.Y = Main.rand.NextFloat(-9f, -6f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
				npc.netUpdate = true;
			}
			*/
            //FINAL JUNGLE FLAMES DESPERATE ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life <= NPC.lifeMax / 3 && (choice == 0 || choice == 3))
            //if (Main.rand.NextBool(40))
            {
                Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    int dust3 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.OrangeRed, 1f);
                    Main.dust[dust3].noGravity = true;
                }
                NPC.velocity.Y = Main.rand.NextFloat(-3f, -1f);

                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5); //last # is speed
                speed += Main.rand.NextVector2Circular(-3, 3);
                speed.Y += Main.rand.NextFloat(3f, -3f); //just added
                if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.JungleWyvernFire>(), fireBreathDamage, 0f, Main.myPlayer); //5f was 0f in the example that works

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.1f, Pitch = 0.2f }, NPC.Center);
                }

                if (NPC.localAI[1] >= 185f) //was 206
                {
                    NPC.localAI[1] = -90f;
                }


                NPC.netUpdate = true;
            }



            if ((intspawnedSpirits < 6) && Main.rand.NextBool(1000))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<NPCs.Enemies.FireLurker>(), 0);
                    Main.npc[Spawned].velocity.Y = -8;
                    intspawnedSpirits++;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
            }
        }







        //NPC.localAI[1]++;

        /* //old projectiles from 1.1 era
        bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
        tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), poisonFireDamage, 10, Main.rand.NextBool(200), false, SoundID.Item17);
        tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatEnergyBeamBall>(), energyBeamDamage, 8, Main.rand.NextBool(200), false, SoundID.Item17);
        tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 8, Main.rand.NextBool(70), false, SoundID.Item17, 0);
        tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 8, lineOfSight && Main.rand.NextBool(200), false, SoundID.Item17, 0);
        tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackFire>(), blackFireDamage, 13, lineOfSight && Main.rand.NextBool(150), false, SoundID.Item17);
        tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemyGreatAttack>(), greatAttackDamage, 8, lineOfSight && Main.rand.NextBool(140), false, SoundID.Item17);
        */




        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Demon Gore 3").Type, 1f);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Potions.StrengthPotion>(), 100, 10, 10, 40));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 10, 5, 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Broadswords.Ragnarok>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 1, 1, 2));
        }
    }
}