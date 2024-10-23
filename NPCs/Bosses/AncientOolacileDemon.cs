using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Defensive.Bands;
using tsorcRevamp.Items.Accessories.Mobility;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Potions.PermanentPotions;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]

    class AncientOolacileDemon : ModNPC
    {
        int meteorDamage = 11;
        int cultistFireDamage = 15;
        int cultistMagicDamage = 19;
        int cultistLightningDamage = 17;
        int fireBreathDamage = 13;
        int lostSoulDamage = 14;
        int greatFireballDamage = 19;
        int blackFireDamage = 25;
        int greatAttackDamage = 33;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            AnimationType = 28;
            NPC.height = 120;
            NPC.width = 50;
            NPC.damage = 46;
            NPC.defense = 8;
            NPC.lifeMax = 4800;
            NPC.scale = 1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 60000;
            NPC.knockBackResist = 0.0f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.rarity = 7;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.AncientOolacileDemon.DespawnHandler"), Color.Gold, DustID.GoldFlame);

            //alt code: if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPCID.EaterofWorldsHead))
            if (NPC.downedBoss1)
            {
                NPC.defense = 18;
                NPC.value = 90000;
                meteorDamage = 16;
                cultistFireDamage = 17;
                cultistMagicDamage = 20;
                cultistLightningDamage = 19;
                fireBreathDamage = 15;
                lostSoulDamage = 15;
                greatFireballDamage = 22;
                blackFireDamage = 27;
                greatAttackDamage = 35;
            }

            //difficulty should be on par with jungle wyvern
            if (NPC.downedBoss3)
            {
                NPC.defense = 26;
                NPC.value = 120000;
                meteorDamage = 17;
                cultistFireDamage = 18;
                cultistMagicDamage = 21;
                cultistLightningDamage = 20;
                fireBreathDamage = 17;
                lostSoulDamage = 17;
                greatFireballDamage = 23;
                blackFireDamage = 28;
                greatAttackDamage = 36;
            }
        }

        NPCDespawnHandler despawnHandler;

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPCID.EyeofCthulhu)))
            {
                target.AddBuff(BuffID.Bleeding, 2 * 60 + 30, false); //bleeding
            }

            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPCID.SkeletronHead)))
            {
                target.AddBuff(BuffID.ShadowFlame, 3 * 60 + 30, false); //acid venom
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 300 * 60, false); //-20 HP after several hits
                target.GetModPlayer<tsorcRevampPlayer>().CurseLevel += 20;
            }

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.CursedInferno, 3 * 60, false); //cursed flames
                target.AddBuff(BuffID.Weak, 5 * 60, false); //weak
            }
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
        }

        public Player Player
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
                NPC.localAI[1] = 40f;

                //TELEPORT MELEE
                if (Main.rand.NextBool(12))
                {
                    tsorcRevampAIs.QueueTeleport(NPC, 25, true, 180);
                    NPC.localAI[1] = 0f;
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
            if (Main.rand.NextBool(24) && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportCountdown == 0)
            {
                tsorcRevampAIs.QueueTeleport(NPC, 25, true, 180);
                NPC.localAI[1] = 0f;
            }
            //RANGED
            if (NPC.Distance(Player.Center) > 201 && NPC.velocity.Y == 0f && Main.rand.NextBool(3))
            {

                NPC.velocity.Y = Main.rand.NextFloat(-9f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(11f, 8f);
                NPC.netUpdate = true;

            }
        }


        //int breathTimer gives weird cool arrow shape, float does the circle
        float breathTimer = 0;
        int spawnedDemons = 0;
        float boredTeleport = 0;
        float stuckTeleport = 0;

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            //If the enemy doesn't have line of sight, spawn a cursed skull and then teleport
            //Since this is a boss, the distance and time is fairly aggressive.
            bool clearLineofSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
            if (clearLineofSight)
            {
                boredTeleport = 0;
            }

            if (NPC.velocity.X == 0 && breathTimer > 0)
            {
                stuckTeleport++;
                if (stuckTeleport == 60)
                {
                    //NPC.localAI[1] = 0;
                    tsorcRevampAIs.QueueTeleport(NPC, 60, false, 240);
                    stuckTeleport = 0;
                    //breathTimer = 1;
                }
            }
            if (NPC.velocity.X > 0)
            {
                stuckTeleport = 0;
            }


            if (!clearLineofSight)
            {
                boredTeleport++;

                if (boredTeleport == 900)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int Skull = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.CursedSkull, 0);
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Skull, 0f, 0f, 0f, 0);
                        }
                    }

                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                }

                if (boredTeleport == 1000)
                {
                    NPC.localAI[1] = 0;
                    tsorcRevampAIs.QueueTeleport(NPC, 40, true, 240);
                    boredTeleport = 0;
                }

            }

            //Oh this is totally gonna desync in MP
            int choice = Main.rand.Next(6);


            //CHANCE TO JUMP BEFORE ATTACK  
            if (NPC.localAI[1] == 140 && NPC.velocity.Y == 0f && Main.rand.NextBool(50) && NPC.life >= NPC.lifeMax / 3)
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

            //play creature sounds (it's fine if this sound is under a random because it doesn't matter when it plays)
            if (Main.rand.NextBool(1700))
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.5f }, NPC.Center);
                //Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
            }

            NPC.localAI[1]++;
            bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
            tsorcRevampAIs.FighterAI(NPC, 1, 0.1f, canTeleport: true, lavaJumping: true);
            //tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBallClone, cultistMagicDamage, 0.1f, Main.rand.NextBool(200), false, SoundID.Item17);
            //tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.localAI[1], 179, ProjectileID.CultistBossFireBall, cultistMagicDamage, 1, Main.rand.NextBool(20), false, SoundID.NPCHit34);
            //tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 160, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 8, Main.rand.NextBool(2), false, 2, 34, 0);
            //tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 8, lineOfSight && Main.rand.NextBool(200), false, 2, 34, 0);
            //tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 130, ModContent.ProjectileType<Projectiles.Enemy.EnemyGreatAttack>(), greatAttackDamage, 8, lineOfSight && Main.rand.NextBool(140), false, 2, 34);

            if (NPC.localAI[1] >= 179)
            {
                if (Main.rand.NextBool(200))
                {
                    //SOUND BROKEN IN MP (can not be under a random, they must be synced)
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 0.1f), ProjectileID.CultistBossFireBallClone, cultistMagicDamage, 0.1f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17);
                    NPC.localAI[1] = 0;
                }
                if (Main.rand.NextBool(20))
                {
                    //SOUND BROKEN IN MP (can not be under a random, they must be synced)
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 0.1f), ProjectileID.CultistBossFireBall, cultistFireDamage, 0.1f, Main.myPlayer);
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
                { breathTimer = -30; }
                if (NPC.life <= NPC.lifeMax / 3)
                { breathTimer = -70; }

            }

            if (breathTimer == 470)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f }, NPC.Center); //shadowflame hex (little beasty)
            }

            if (breathTimer < 0)
            {
                //play breath sound (on every client) (it's fine if this one is under a random because breathTimer is deterministic)
                if (Main.rand.NextBool(3))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center); //flame thrower
                }

                //spawn the projectile (not on every client!)
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //npc.velocity.Y = -1.1f;
                    NPC.velocity.Y = Main.rand.NextFloat(-4f, -1.1f);
                    NPC.velocity.X = 0.5f;


                    Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].OldPos(9), 9);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y - 40f, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
                    //NPC.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
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
                //npc.TargetClosest(true);
                NPC.velocity.X = 0f;
                //Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.Center.X, npc.Center.Y / 2, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 0f, Main.myPlayer);
            }

            //PLAYER RUNNING AWAY? SPAWN DesertDjinnCurse, 
            Player player3 = Main.player[NPC.target];
            if (Main.rand.NextBool(100) && NPC.Distance(player3.Center) > 600)
            {
                //SOUND BROKEN IN MP (can not be under a random, they must be synced)
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8f, 1.06f, true, true);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ProjectileID.DesertDjinnCurse, lostSoulDamage, 7f, Main.myPlayer);
                    //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center); //wobble
                NPC.localAI[1] = 1f;


            }
            //tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], ProjectileID.LostSoulHostile, lostSoulDamage, 3, lineOfSight, true, 4, 9);

            //SPAWN FIRE LURKER
            if ((spawnedDemons < 2) && NPC.life >= NPC.lifeMax / 3 * 2 && Main.rand.NextBool(3000))
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
                if (NPC.life >= NPC.lifeMax / 3 && NPC.life <= NPC.lifeMax / 3 * 2 && clearSpace)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);

                    speed.Y += Main.rand.NextFloat(-2f, -6f);
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, fireBreathDamage, 0f, Main.myPlayer);
                        }
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -.5f }, NPC.Center);

                    if (NPC.localAI[1] >= 195f)
                    {
                        NPC.localAI[1] = 1f;
                    }
                }
                //LOB ATTACK >> BOUNCING FIRE
                if (NPC.life >= NPC.lifeMax / 3 * 2 && clearSpace)

                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);
                    speed.Y += Main.rand.NextFloat(2f, -2f);
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, fireBreathDamage, 0f, Main.myPlayer);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);

                        if (NPC.localAI[1] >= 186f)
                        {
                            NPC.localAI[1] = 1f;
                        }
                    }
                }
            }


            //MULTI-FIRE 1 ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life >= NPC.lifeMax / 3 && choice == 1)
            {
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(4), 7);
                //speed.Y += Main.rand.NextFloat(2f, -2f); //just added
                if (Main.rand.NextBool(3) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    //SOUND BROKEN IN MP (can not be under a random, they must be synced)
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 5f, Main.myPlayer); //5f was 0f in the example that works                    
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                }

                if (NPC.localAI[1] >= 175f)
                {
                    NPC.localAI[1] = 1f;
                }
            }

            //MULTI-BOUNCING DESPERATE FIRE ATTACK
            if (NPC.localAI[1] >= 160f && NPC.life <= NPC.lifeMax / 3 && (choice == 1 || choice == 2))
            {
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 3);
                speed.Y += Main.rand.NextFloat(2f, -2f);
                if (Main.rand.NextBool(2) && ((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    //SOUND BROKEN IN MP (can not be under a random, they must be synced)
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, cultistFireDamage, 3f, Main.myPlayer); //5f was 0f in the example that works
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center); //fire
                }

                if (NPC.localAI[1] >= 190f) //was 126
                {
                    NPC.localAI[1] = 1f;
                }
            }
            //LIGHTNING ATTACK
            if (NPC.localAI[1] == 160f && NPC.life >= NPC.lifeMax / 6 && NPC.life <= NPC.lifeMax / 3 * 2 && (choice == 5 || choice == 4))
            {
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].OldPos(1), 1);
                //speed += Main.player[npc.target].velocity / 4;

                speed.Y += Main.rand.NextFloat(-2, -5f);//was -2, -6

                //speed += Main.rand.NextVector2Circular(-10, -8);
                if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.CultistBossLightningOrb, cultistLightningDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                }

                NPC.localAI[1] = -50f;
            }

            /*JUMP DASH FOR FINAL
			if (npc.localAI[1] == 140 && npc.velocity.Y == 0f && Main.rand.NextBool(20) && npc.life <= 1000)
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
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.JungleWyvernFire>(), fireBreathDamage, 0f, Main.myPlayer); //5f was 0f in the example that works
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.1f, Pitch = 0.2f }, NPC.Center);
                }

                if (NPC.localAI[1] >= 185f) //was 206
                {
                    NPC.localAI[1] = -90f;
                }
            }
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.OolacileDemonBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BandOfCosmicPower>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PermanentShinePotion>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PermanentNightOwlPotion>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.CloudinaBalloon));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 1, 1, 3));
            npcLoot.Add(notExpertCondition);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (!Main.dedServ)
            {
                if (NPC.life <= 0)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Oolacile Demon Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Oolacile Demon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Oolacile Demon Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Oolacile Demon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Ancient Oolacile Demon Gore 3").Type, 1f);
                }
            }
        }
    }
}