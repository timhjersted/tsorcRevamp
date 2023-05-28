using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class TheSorrow : ModNPC
    {
        int waterTrailsDamage = 30;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] 
                {
                    BuffID.Frostburn,
                    BuffID.Frostburn2,
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 18000;
            NPC.damage = 60;
            NPC.defense = 34;
            NPC.knockBackResist = 0f;
            NPC.scale = 1.4f;
            NPC.value = 170000;
            NPC.npcSlots = 6;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.coldDamage = true;
            DrawOffsetY = +70;
            NPC.width = 140;
            NPC.height = 60;
            despawnHandler = new NPCDespawnHandler("The Sorrow claims you...", Color.DarkCyan, 29);
        }

        //npc.ai[0] = damage taken counter
        //npc.ai[1] = invulnerability timer
        //npc.ai[3] = state counter
        public float flapWings;
        int hitTime = 0; //How long since it's last been hit (used for reducing damage counter)

        //magic from above attack
        public float iceSpiritTimer;

        float breathTimer = 60;

        //gaibon 
        public int Timer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public Player Target
        {
            get => Main.player[NPC.target];
        }
        public bool secondPhase
        {
            get => NPC.life <= NPC.lifeMax / 2;
        }

        float turtleTimer;
        bool announcedDebuffs = false;
        float ice3Timer;


        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            BirdAI();
            BreathAttack();
            Ice3Attack();
            InflictDebuffs();

            //Ice spirit attack starts in phase 2
            if (secondPhase)
            {
                IceSpiritAttack();
            }
            turtleTimer++;
            //SPAWN TURTLES!
            if (turtleTimer > 3000 && (Target.Center.Y + 20 >= NPC.Center.Y) && breathTimer > 0 && !secondPhase)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit24 with { Volume = 0.5f }, NPC.Center);

                Dust.NewDust(NPC.Center, NPC.width, NPC.height, DustID.PoisonStaff, NPC.velocity.X, NPC.velocity.Y);
                Dust.NewDust(NPC.Center, NPC.width, NPC.height, DustID.PoisonStaff, NPC.velocity.X, NPC.velocity.Y);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int Turtle = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.IceTortoise, 0); 

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Turtle, 0f, 0f, 0f, 0);
                    }
                }

                turtleTimer = 0;
            }

            //DARK ELF MAGE SPAWN
            /*
            if ((customspawn1 < 3) && Main.rand.NextBool(2000))
            {
                int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.DarkElfMage>(), 0);
                Main.npc[Spawned].velocity.Y = -8;
                Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;

                customspawn1 += 1f;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                }
            }
            */
        }

        void InflictDebuffs()
        {
            //Displaying slow range with ring
            if (secondPhase)
            {
                UsefulFunctions.DustRing(NPC.Center, 200, DustID.IceTorch, 10, 2f);
            }

            //Check every player in the game to see if they are in range
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                //Storing the distance means we don't have to re-calculate it multiple times
                float distance = NPC.Distance(player.Center);

               
                //Phase 2 triggers chilled, slow and frostburn
                if (distance < 1550 && secondPhase)
                {
                    player.AddBuff(BuffID.Chilled, 30, false);

                    if (distance < 200)
                    {
                        player.AddBuff(BuffID.Slow, 30, false);
                        player.AddBuff(BuffID.Frostburn, 30, false);
                    }

                    //announce proximity debuffs once
                    if (!announcedDebuffs)
                    {
                        UsefulFunctions.BroadcastText("The Sorrow emits a chilling cold from its body. The loss of your family lashes your heart with grief!", 235, 199, 23);//deep yellow
                        announcedDebuffs = true;
                    }
                }
            }            
        }

        void BreathAttack()
        {
            breathTimer++;

            //Breath charges faster in phase 2
            if (secondPhase)
            {
                breathTimer += 0.34f;
            }

            //dust animation
            if (breathTimer > 380)
            {
                UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 100)), DustID.IceTorch, 48, 4);
                Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 5);

                if (Main.GameUpdateCount % 5 == 0)
                {
                    NPC.netUpdate = true;
                    NPC.netSpam = 0;
                }
            }

            breathTimer++;
            //longer breath at half health          
            if (breathTimer > 480)
            {
                breathTimer = -120;

                if (secondPhase)
                {
                    breathTimer = -180;
                }
            }

            //projectile
            if (breathTimer < 0)
            {
                NPC.velocity.X = 0f;

                if (Main.GameUpdateCount % 5 == 0)
                {
                    NPC.netUpdate = true;
                    NPC.netSpam = 0;
                }

                //play breath sound
                if (Main.rand.NextBool(3))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.9f, PitchVariance = 1f }, NPC.Center); //flame thrower
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 9);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<FrozenDragonsBreath>(), waterTrailsDamage, 0f, Main.myPlayer);
                }
            }
        }

        void Ice3Attack()
        {
            ice3Timer++;
            //GAIBON SHOOT!
            if (breathTimer > 0 && ice3Timer > 160 && !secondPhase)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8, .1f, true, true);
                    velocity += Target.velocity / 1.5f;
                    if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIce3Ball>(), waterTrailsDamage / 4, 0.5f, Main.myPlayer); //EnemySpellIcestormBall
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIce3Ball>(), waterTrailsDamage / 4, 0.5f, Main.myPlayer);
                    }
                }

                ice3Timer = 0;
            }
        }

        void IceSpiritAttack()
        {
            float iceSpiritTimerCap = 900;
            iceSpiritTimer++;

            //Cooldown gets faster with lower life
            if (NPC.life <= NPC.lifeMax / 4)
            {
                iceSpiritTimerCap = 450;
            }

            if (NPC.life <= NPC.lifeMax / 6)
            {
                iceSpiritTimerCap = 225;
            }

            if (iceSpiritTimer > iceSpiritTimerCap)
            {
                iceSpiritTimer = -5;
            }

            if (iceSpiritTimer < 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)Target.Center.X - 800 + Main.rand.Next(1600), (float)Target.Center.Y - 500f, (float)(-40 + Main.rand.Next(80)) / 10, 2.5f, ModContent.ProjectileType<IceSpirit>(), waterTrailsDamage, 2f, Main.myPlayer); //ProjectileID.CultistBossFireBallClone
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.3f, PitchVariance = 2f }, NPC.Center);
            }
        }

        void BirdAI()
        {
            //BIRD AI
            NPC.ai[1]++;
            NPC.ai[2]++;
            hitTime++;
            if (NPC.ai[0] > 0) NPC.ai[0] -= hitTime / 10;
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, NPC.velocity.X, NPC.velocity.Y, 200, new Color(), 0.1f + (10.5f * (NPC.ai[0] / (NPC.lifeMax / 10)))); //10.5 was 15.5
            Main.dust[dust].noGravity = true;

            flapWings++;

            //Flap Wings
            if (flapWings == 30 || flapWings == 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.0f }, NPC.Center); //wing flap sound

            }
            if (flapWings == 95)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.1f }, NPC.Center);
                flapWings = 0;
            }

            if (NPC.ai[3] == 0)
            {
                //normal
                NPC.alpha = 0;
                NPC.defense = 24;
                NPC.damage = 120;
                //NPC.dontTakeDamage = false;
                if (NPC.ai[2] < 600)
                {
                    //No horizontal movement if using breath attack
                    if (breathTimer > 0)
                    {
                        if (Main.player[NPC.target].Center.X < NPC.Center.X)
                        {
                            if (NPC.velocity.X > -8) { NPC.velocity.X -= 0.22f; }
                        }
                        if (Main.player[NPC.target].Center.X > NPC.Center.X)
                        {
                            if (NPC.velocity.X < 8) { NPC.velocity.X += 0.22f; }
                        }
                    }

                    if (Main.player[NPC.target].Center.Y < NPC.Center.Y + 300)
                    {
                        if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                        else NPC.velocity.Y -= 0.07f;
                    }
                    if (Main.player[NPC.target].Center.Y > NPC.Center.Y + 300)
                    {
                        if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                        else NPC.velocity.Y += 0.07f;
                    }

                    if (NPC.ai[1] >= 0 && NPC.ai[2] > 120 && NPC.ai[2] < 600)
                    {
                        //If the sorrow doesn't have line of sight to the player due to blocks in the way, its projectiles will be able to phase through walls to hit them and travel much faster.
                        //phasedBullets is passed to the projectile's ai[0] value (which takes a float) to tell it whether or not to collide with tiles
                        float speed = 9f;
                        float phasedBullets = 0;
                        if (!Collision.CanHit(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1) && !Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                        {
                            speed = 18f;
                            phasedBullets = 1;
                        }

                        int type = ModContent.ProjectileType<WaterTrail>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.SplashWeak, NPC.Center);

                        //Get the vector that points from the NPC to the player
                        Vector2 difference = Main.player[NPC.target].Center - NPC.Center;

                        //Create a new vector that is just a line with length [speed], and rotate it to be facing the player based on the preivious vector
                        Vector2 velocity = new Vector2(speed, 0).RotatedBy(difference.ToRotation());
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            //Fire a projectile right at the player
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, velocity.X, velocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);

                            //Rotate it further to fire the shots angled away from the player
                            Vector2 angledVelocity = velocity.RotatedBy(Math.PI / 6);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);
                            angledVelocity = velocity.RotatedBy(-Math.PI / 6);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);

                            //And again the more offset shots
                            angledVelocity = velocity.RotatedBy(Math.PI / 3);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);
                            angledVelocity = velocity.RotatedBy(-Math.PI / 3);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);

                            //And once mroe for the most offset shots
                            angledVelocity = velocity.RotatedBy(Math.PI / 1.8);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);
                            angledVelocity = velocity.RotatedBy(-Math.PI / 1.8);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);
                            //Could this all have been a for loop? Yeah. Easier to read like this though, imo.
                        }
                        NPC.ai[1] = -180;
                    }
                }
                else if (NPC.ai[2] >= 600 && NPC.ai[2] < 690)
                {
                    //Then chill for a second.
                    //This exists to delay switching to the 'charging' pattern for a moment to give time for the player to make distance
                    NPC.velocity.X *= 0.95f;
                    NPC.velocity.Y *= 0.95f;
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 132, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, default, 1f);
                }
                else if (NPC.ai[2] >= 690 && NPC.ai[2] < 1290 && breathTimer > 0)
                {
                    int dashSpeed = 18;
                    NPC.velocity.X *= 0.98f;
                    NPC.velocity.Y *= 0.98f;
                    if ((NPC.velocity.X < 2f) && (NPC.velocity.X > -2f) && (NPC.velocity.Y < 2f) && (NPC.velocity.Y > -2f))
                    {
                        float rotation = (float)Math.Atan2((NPC.Center.Y) - Main.player[NPC.target].Center.Y, (NPC.Center.X) - Main.player[NPC.target].Center.X);
                        NPC.velocity.X = (float)(Math.Cos(rotation) * dashSpeed) * -1;
                        NPC.velocity.Y = (float)(Math.Sin(rotation) * dashSpeed) * -1;
                    }
                }
                else NPC.ai[2] = 0;
            }
            else
            {
                //invisibility mode
                NPC.ai[3]++;
                NPC.alpha = 220;
                NPC.defense = 44;
                NPC.damage = 150;

                //NPC.dontTakeDamage = true;
                if (Main.player[NPC.target].Center.X < NPC.Center.X)
                {
                    if (NPC.velocity.X > -6) { NPC.velocity.X -= 0.22f; }
                }
                if (Main.player[NPC.target].Center.X > NPC.Center.X)
                {
                    if (NPC.velocity.X < 6) { NPC.velocity.X += 0.22f; }
                }
                if (Main.player[NPC.target].Center.Y < NPC.Center.Y)
                {
                    if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                    else NPC.velocity.Y -= 0.07f;
                }
                if (Main.player[NPC.target].Center.Y > NPC.Center.Y)
                {
                    if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                    else NPC.velocity.Y += 0.07f;
                }
                if (NPC.ai[1] >= 0 && NPC.ai[2] > 120 && NPC.ai[2] < 600)
                {
                    float num48 = 13f;
                    float invulnDamageMult = 1.48f;
                    int type = ModContent.ProjectileType<WaterTrail>();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.SplashWeak, NPC.Center);
                    float rotation = (float)Math.Atan2(NPC.Center.Y - 80 - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                    //yes do it manually. im not using a loop. i don't care //Understandable, have a nice day.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1), (float)((Math.Sin(rotation + 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, (float)((Math.Cos(rotation + 0.8) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 80, (float)((Math.Cos(rotation - 0.8) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                    }
                    NPC.ai[1] = -180;
                }
                if (NPC.ai[3] == 100)
                {
                    //Use ice spirit attack
                    iceSpiritTimer = 900;
                    turtleTimer += 500;
                    NPC.ai[3] = 1;
                    //if (NPC.life > (NPC.lifeMax / 2) + 100 || NPC.life < (NPC.lifeMax / 2) - 950)
                    if (NPC.life > 550)
                    {
                        NPC.life -= 350; //amount boss takes damage when becoming enraged
                    }
                        
                    if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                    NPC.netUpdate = true;
                }
                if (NPC.ai[1] >= 0)
                {
                    NPC.ai[3] = 0;
                    for (int i = 0; i < 40; i++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, 0, 0, 0, new Color(), 3f);
                    }
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(breathTimer);
            writer.Write(ice3Timer);
            writer.Write(iceSpiritTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            breathTimer = reader.ReadSingle();
            ice3Timer = reader.ReadSingle();
            iceSpiritTimer = reader.ReadSingle();
        }

        public override void FindFrame(int frameHeight)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X <= 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 4.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
            }
            else
            {
                NPC.alpha = 200;
            }
        }
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            NPC.ai[0] += hit.Damage;
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            NPC.ai[0] += hit.Damage;
        }
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            hitTime = 0;
            if (NPC.ai[0] > (NPC.lifeMax / 10))
            {
                UsefulFunctions.BroadcastText("The Sorrow has taken damage too fast, its natural defenses activate...", Color.Orange);

                NPC.ai[3] = 1; //begin inisibility/high defense state
                for (int i = 0; i < 50; i++)
                { //dustsplosion on invisibility
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, default, 3f);
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, 0, 0, 100, default, 3f);
                }
                NPC.ai[1] = -180;
                NPC.ai[0] = 0; //reset damage counter
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TheSorrowBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CrestOfWater>(), 1, 2, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.AdamantiteDrill));
            npcLoot.Add(notExpertCondition);
        }
        public override void OnKill()
        {
            for (int num36 = 0; num36 < 100; num36++)
            {
                int dust = Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 29, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            for (int num36 = 0; num36 < 100; num36++)
            {
                Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 132, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, Color.Orange, 3f);
            }
        }
    }
}