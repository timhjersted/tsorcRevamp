using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class Gaibon : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 2;
            NPC.width = 70;
            NPC.height = 100;
            NPC.scale = 0.6f;
            DrawOffsetY = 20;
            //It genuinely had none in the original.
            NPC.defense = 0;
            Music = 12;
            NPC.defense = 10; //should this be here?
            NPC.boss = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/NPCKilled/Gaibon_Roar");
            NPC.lifeMax = 4000;
            NPC.knockBackResist = 0.9f;
            NPC.value = 130000;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            despawnHandler = new NPCDespawnHandler(DustID.Torch);

            CurrentMove = Bombardment;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Gaibon");
        }

        //Since burning spheres are an NPC, not a projectile, this damage does not get doubled!
        int burningSphereDamage = 120;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.damage = (int)(NPC.damage * 1.3 / 2);
            NPC.defense = NPC.defense += 12;
            //For some reason, its contact damage doesn't get doubled due to expert mode either apparently?
            //burningSphereDamage = (int)(burningSphereDamage / 2);
        }


        NPCDespawnHandler despawnHandler;
        bool slograDead = false;

        public int Timer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        public Player Target
        {
            get => Main.player[NPC.target];
        }

        List<Action> MoveList
        {
            get => new List<Action>() { Bombardment, Bursts, Scatter, Lob };
        }

        public Action CurrentMove;
        Vector2 acceleration = Vector2.Zero;
        float accelerationMagnitude = 5f / 60f; //Jerk is change in acceleration
        float topSpeed = 10;
        float flyingTime = 0;

        Vector2 targetPointValue;
        Vector2 targetPoint
        {
            get
            {
                return targetPointValue;
            }
            set
            {
                flyingTime = 0;
                targetPointValue = value;
            }
        }
        public override void AI()
        {
            flyingTime++;
            UsefulFunctions.DustRing(targetPoint, 80, DustID.ShadowbeamStaff);
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            CurrentMove();
            FlyTowardTarget();

            if (slograDead)
            {
                NPC.knockBackResist = 0;
                //Throw tridents
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {                    
                    if (Main.GameUpdateCount % 60 == 0)
                    {
                        int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y - 48, NPCID.BurningSphere, 0);
                        Main.npc[spawned].damage = burningSphereDamage;
                        Main.npc[spawned].velocity += Main.player[NPC.target].velocity;
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.4f }, NPC.Center);
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                        }
                    }
                }
            }

            //If super far away from the player, warp to them
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 5000)
            {
                NPC.Center = new Vector2(Main.player[NPC.target].Center.X, Main.player[NPC.target].Center.Y + 500);
            }

            //If Slogra is dead, we don't need to keep calling AnyNPCs.
            if (!slograDead)
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<Slogra>()))
                {
                    slograDead = true;
                }
            }

            if (Target.Center.X > NPC.Center.X)
            {
                NPC.direction = 1;
            }
            else
            {
                NPC.direction = -1;
            }

            //Dust ID = 262           
        }


        
        
        bool movingLeft = true;
        int passCount = 0;
        void Bombardment()
        {
            topSpeed = 7;
            Timer++;
            targetPoint = Target.Center;
            if (movingLeft)
            {
                targetPoint += new Vector2(-550, -300);   
            }
            else
            {
                targetPoint += new Vector2(550, -300);
            }

            

            if(Vector2.Distance(NPC.Center, targetPoint) < 80)
            {
                movingLeft = !movingLeft;
                passCount++;
            }

            if (Timer % 10 == 0)
            {

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {                    
                    int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<NPCs.Enemies.GaibonFireball>(), ai0: burningSphereDamage, ai1: 0, ai2: 5);
                    Main.npc[spawned].damage = burningSphereDamage;
                    Main.npc[spawned].velocity = new Vector2(0, 8);
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.3f }, NPC.Center);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                    }
                }
            }

            if(passCount >= 4)
            {
                ChangeMove();
            }
        }

        bool reachedTarget = false;
        float chargeTime = 70f;
        float shotsFired = 0;
        void Bursts()
        {
            chargeTime = 70f;
            topSpeed = 10;


            targetPoint = Target.Center;
            if (movingLeft && shotsFired == 0 || !movingLeft && shotsFired == 2)
            {
                targetPoint += new Vector2(-550, -300);
            }
            if (!movingLeft && shotsFired == 0 || movingLeft && shotsFired == 2)
            {
                targetPoint += new Vector2(550, -300);
            }
            if(shotsFired == 1)
            {
                targetPoint += new Vector2(0, -350);
            }


            if (Vector2.Distance(NPC.Center, targetPoint) < 150)
            {
                Timer++;
                targetPoint = NPC.Center; //Slow to a stop
                NPC.velocity *= 0.9f;
                if (Target.Center.X > NPC.Center.X)
                {
                    NPC.direction = 1;
                }
                else
                {
                    NPC.direction = -1;
                }


                if (Timer > chargeTime && Timer % 15 == 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 1f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 position = NPC.Center + new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 5);
                            Vector2 velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Target.Center, 8);
                            int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)position.X, (int)position.Y, ModContent.NPCType<NPCs.Enemies.GaibonFireball>(), ai0: burningSphereDamage, ai1: velocity.X, ai2: velocity.Y, Target: NPC.target);
                            Main.npc[spawned].damage = burningSphereDamage;
                            Main.npc[spawned].velocity = new Vector2(0, 8);
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                            }
                        }
                    }
                }

                if (Timer >= 115)
                {
                    shotsFired++;
                    Timer = 0;
                }

                float radius = chargeTime - Timer;
                if (radius < 0)
                {
                    radius = 0;
                }
                for (int j = 0; j < 20 * ((float)Timer / chargeTime); j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(80 + radius * 20, 80 + radius * 20);
                    Vector2 dustPos = NPC.Center + dir;
                    Vector2 dustVel = dir.RotatedBy(MathHelper.Pi);

                    if (Timer > chargeTime)
                    {
                        dustVel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Target.Center, 0.75f);
                    }

                    dustVel.Normalize();
                    dustVel *= 6;
                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1f).noGravity = true;
                }

            }

            if(shotsFired >= 3)
            {
                shotsFired = 0;
                ChangeMove();
            }

        }

        void Scatter()
        {
            NPC.knockBackResist = 0;
            Timer++;
            if (Vector2.Distance(NPC.Center, targetPoint) > 150 || Timer == 1)
            {
                targetPoint = Target.Center;
                targetPoint += new Vector2(0, -250);
            }
            else
            {
                targetPoint = NPC.Center;
                NPC.velocity *= 0.9f;                

                if (Timer >= 120 && Timer % 120 == 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 1f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 15);
                            Vector2 velocity = position;
                            position += NPC.Center;
                            velocity.Normalize();
                            velocity *= 6;
                            int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)position.X, (int)position.Y, ModContent.NPCType<NPCs.Enemies.GaibonFireball>(), ai0: burningSphereDamage, ai1: velocity.X, ai2: velocity.Y, Target: NPC.target);
                            Main.npc[spawned].damage = burningSphereDamage;
                            Main.npc[spawned].velocity = new Vector2(0, 8);
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                            }
                        }
                    }
                }

                float radius = chargeTime - Timer;
                if (radius < 0)
                {
                    radius = 0;
                }
                for (int j = 0; j < 20 * ((float)Timer / 120); j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(80 + radius * 20, 80 + radius * 20);
                    Vector2 dustPos = NPC.Center + dir;
                    Vector2 dustVel = dir.RotatedBy(MathHelper.Pi);
                    dustVel.Normalize();
                    dustVel *= 6;
                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1f).noGravity = true;
                }

            }

            if(Timer >= 360)
            {
                ChangeMove();
            }
        }

        void Lob()
        {
            Timer++;
            topSpeed = Vector2.Distance(NPC.Center, Target.Center) / 50f;
            targetPoint = Target.Center;
            NPC.knockBackResist = 2f;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {                
                if (Timer % 120 == 0)
                {
                    Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8, .1f, true, true);
                    velocity += Target.velocity / 1.5f;
                    if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.CrystalFire>(), burningSphereDamage / 4, 0.5f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.CrystalFire>(), burningSphereDamage / 4, 0.5f, Main.myPlayer);
                    }
                }
            }

            if(Timer > 600)
            {
                ChangeMove();
            }
        }

        int count = 0;
        int movePhase = 0;
        int flightTimer = 0;
        void Charge()
        {
            if (count == 4)
            {
                movePhase = 0;
                count = 0;
                ChangeMove();
            }

            if (movePhase == 0)
            {
                topSpeed = 10;
                if (movingLeft)
                {
                    targetPoint = new Vector2(Main.player[NPC.target].Center.X - 500, Main.player[NPC.target].Center.Y - 300);
                }
                if (!movingLeft)
                {
                    targetPoint = new Vector2(Main.player[NPC.target].Center.X + 500, Main.player[NPC.target].Center.Y - 300);
                }
                if (Vector2.Distance(NPC.Center, targetPoint) < 150)
                {
                    Timer++;
                    NPC.velocity *= 0.9f;
                    targetPoint = NPC.Center;
                }

                if(Timer > 20)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 1f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            Vector2 position = new Vector2(0, 80).RotatedBy(i * MathHelper.Pi / 15);
                            Vector2 velocity = position;
                            position += NPC.Center;
                            velocity.Normalize();
                            velocity *= 6;
                            int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)position.X, (int)position.Y, ModContent.NPCType<NPCs.Enemies.GaibonFireball>(), ai0: burningSphereDamage, ai1: velocity.X, ai2: velocity.Y, Target: NPC.target);
                            Main.npc[spawned].damage = burningSphereDamage;
                            Main.npc[spawned].velocity = new Vector2(0, 8);
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
                            }
                        }
                    }
                    Timer = 0;
                    movePhase = 1;
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/GaibonSpit2") with { Volume = 0.4f }, NPC.Center);
                }
            }

            if(movePhase == 1)
            {
                flightTimer++;
                topSpeed = 25;
                targetPoint = Target.Center;

                if (Vector2.Distance(NPC.Center, targetPoint) < 200 || flightTimer > 180)
                {
                    flightTimer = 0;
                    reachedTarget = true;
                }
                if (reachedTarget)
                {
                    Timer++;
                }                

                if (Timer > 40)
                {
                    movingLeft = !movingLeft;
                    reachedTarget = false;
                    Timer = 0;
                    movePhase = 0;
                    count++;
                }
            }
        }

        void FlyTowardTarget()
        {
            if (targetPoint != Vector2.Zero)
            {
                accelerationMagnitude = 0.7f + flyingTime / 60;
                acceleration = UsefulFunctions.GenerateTargetingVector(NPC.Center, targetPoint, accelerationMagnitude);
                if (!acceleration.HasNaNs())
                {
                    NPC.velocity += acceleration;
                }
                if (NPC.velocity.Length() > topSpeed)
                {
                    NPC.velocity.Normalize();
                    NPC.velocity *= topSpeed;
                }
            }
        }

        void ChangeMove()
        {
            NPC.knockBackResist = 0.9f;
            Timer = 0;
            List<Action> possibleMoves = MoveList;

            if (CurrentMove != null && CurrentMove != Charge)
            {
                possibleMoves.Remove(CurrentMove);
            }

            CurrentMove = possibleMoves[Main.rand.Next(0, possibleMoves.Count)];

            if (slograDead && Main.rand.NextBool())
            {
                //possibleMoves.Remove(Scatter);
                CurrentMove = Charge;
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (slograDead)
            {
                modifiers.FinalDamage *= 1.5f;
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (slograDead)
            {
                modifiers.FinalDamage *= 1.5f;
            }
            if (projectile.minion)
            {
                modifiers.Knockback *= 0;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }

            NPC.spriteDirection = NPC.direction;

            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 6.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (slograDead)
            {
                if (texture == null || texture.IsDisposed)
                {
                    texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
                }

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
                data.Apply(null);
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Rectangle sourceRectangle = NPC.frame;
                Vector2 origin = sourceRectangle.Size() / 2f;
                Vector2 offset = new Vector2(0, -14);
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition + offset, sourceRectangle, Color.White, NPC.rotation, origin, 0.7f, effects, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
            }
            return base.PreDraw(spriteBatch, screenPos, drawColor);
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
            npcLoot.Add(new ItemDropWithConditionRule(ModContent.ItemType<Items.BossBags.GaibonBag>(), 1, 1, 1, new GaibonDropCondition()));
        }

        #region gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 1").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 3").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 4").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 3").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gaibon Gore 4").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
            }
            if (!NPC.AnyNPCs(ModContent.NPCType<Slogra>()))
            {
                if (!Main.expertMode)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.PoisonbiteRing>(), 1);
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.BloodbiteRing>(), 1);
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<DarkSoul>(), (200 + Main.rand.Next(300)));
                }
            }
            else
            {
                int slograID = NPC.FindFirstNPC(ModContent.NPCType<Slogra>());
                int speed = 30;
                for (int i = 0; i < 200; i++)
                {
                    Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.Pi);
                    Vector2 dustPos = NPC.Center + dir * 3 * 16;
                    float distanceFactor = Vector2.Distance(NPC.position, Main.npc[slograID].position) / speed;
                    Vector2 speedRand = Vector2.UnitX.RotatedByRandom(MathHelper.Pi) * 10;
                    float speedX = (((Main.npc[slograID].position.X + (Main.npc[slograID].width * 0.5f)) - NPC.position.X) / distanceFactor) + speedRand.X;
                    float speedY = (((Main.npc[slograID].position.Y + (Main.npc[slograID].height * 0.5f)) - NPC.position.Y) / distanceFactor) + speedRand.Y;
                    Vector2 dustSpeed = new Vector2(speedX, speedY);
                    Dust dustObj = Dust.NewDustPerfect(dustPos, 173, dustSpeed, 200, default, 3);
                    dustObj.noGravity = true;
                }
            }
        }
        #endregion

    }

    public class GaibonDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<Slogra>());
        }

        public bool CanShowItemDropInUI()
        {
            return false;
        }

        public string GetConditionDescription()
        {
            return "Drops if Slogra is dead";
        }
    }
}