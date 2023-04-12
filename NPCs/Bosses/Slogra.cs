using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class Slogra : ModNPC
    {
        public override void SetDefaults()
        {

            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 16;
            NPC.width = 38;
            NPC.height = 32;
            AnimationType = 28;
            NPC.aiStyle = -1;
            NPC.timeLeft = 22750;
            NPC.damage = 70;
            //npc.Music = 12;
            NPC.boss = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/NPCKilled/Gaibon_Roar");
            NPC.lifeMax = 4000;
            NPC.scale = 1.1f;
            NPC.knockBackResist = 0.4f;
            NPC.value = 130000;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            despawnHandler = new NPCDespawnHandler("Slogra returns to the depths...", Color.DarkGreen, DustID.Demonite);

        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Slogra, Lost Soul of the Depths");
        }

        int tridentDamage = 40;
        //Since burning spheres are an NPC, not a projectile, this damage does not get doubled!
        int burningSphereDamage = 120;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.damage = (int)(NPC.damage * 1.3 / 2);
            NPC.defense = NPC.defense += 22;
            tridentDamage = (int)(tridentDamage / 2);
        }


        NPCDespawnHandler despawnHandler;
        bool gaibonDead = false;
        int moveTimer = 0;
        bool dashAttack = false;
        Vector2 pickedTrajectory = Vector2.Zero;
        int baseCooldown = 240;
        int lineOfSightTimer = 0;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            moveTimer++;
            if (gaibonDead)
            {
                NPC.defense = 0; //Speed things up a bit
                baseCooldown = 90;
            }

            //Perform attacks
            if (moveTimer >= baseCooldown)
            {
                if (dashAttack)
                {
                    DashAttack();
                }
                else
                {
                    JumpAttack();
                }                
            }

            //Do basic movement
            if (moveTimer < baseCooldown)
            {
                NPC.ai[3] = 0; //Never get bored
                tsorcRevampAIs.FighterAI(NPC, 4, 0.12f, 0.12f, true);
                //NPC.velocity.Y += 0.015f; //Gravity
            }


            //Throw tridents
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float projectileDelay = 120;
                if (gaibonDead)
                {
                    projectileDelay = 90;
                }
                if (moveTimer % projectileDelay == 30 && moveTimer < baseCooldown)
                {
                    if (gaibonDead)
                    {
                        for(int i = 0; i < 9; i++)
                        {
                            Vector2 targetPoint = Main.player[NPC.target].Center + new Vector2(-480 + 120 * i, 0);
                            Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPoint, 12, .1f, true, true);
                            if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), tridentDamage, 0.5f, Main.myPlayer);
                            }
                        }

                    }
                    else
                    {
                        Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 8, .1f, true, true);
                        if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.player[NPC.target].velocity / 1.5f, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), tridentDamage, 0.5f, Main.myPlayer);
                        }
                    }
                    
                }
            }

            //Spawn dust telegraphing moves
            if (moveTimer < baseCooldown + 70)
            {
                float radius = baseCooldown - moveTimer;
                if (radius < 0)
                {
                    radius = 0;
                }

                for (int j = 0; j < 10 * ((float)moveTimer / (float)baseCooldown); j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(40 + radius, 40 + radius);
                    Vector2 dustPos = NPC.Center + dir;
                    Vector2 dustVel = dir.RotatedBy(MathHelper.Pi);

                    if (moveTimer > baseCooldown)
                    {
                        dustVel = pickedTrajectory;
                    }

                    dustVel.Normalize();
                    dustVel *= 6;
                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 0.75f).noGravity = true;
                }
            }

            //Check if Gaibon is dead. If so we don't need to keep calling AnyNPCs.
            if (!gaibonDead)
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<Gaibon>()))
                {
                    gaibonDead = true;
                }
            }

            //Check if it has line of sight, and if not increase the timer
            if (Collision.CanHitLine(Main.player[NPC.target].Center, 1, 1, NPC.Center, 1, 1))
            {
                lineOfSightTimer = 0;
            }
            else
            {
                lineOfSightTimer++;
            }

            //Jump occasionally if no line of sight
            if (moveTimer < baseCooldown && NPC.velocity.Y == 0 && lineOfSightTimer % 140 == 139)
            {
                NPC.velocity.Y -= 8f;
            }

            //If super far away from the player or no line of sight for too long, warp to them
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 5000 || lineOfSightTimer > 300)
            {
                Vector2 warpPoint = Main.rand.NextVector2CircularEdge(500, 500);
                for (int i = 0; i < 100; i++)
                {
                    warpPoint = Main.rand.NextVector2CircularEdge(500, 500);
                    if (Collision.CanHitLine(Main.player[NPC.target].Center, 1, 1, Main.player[NPC.target].Center + warpPoint, 1, 1))
                    {
                        break;
                    }
                }

                NPC.Center = Main.player[NPC.target].Center + warpPoint;
                moveTimer = 0;
                NPC.netUpdate = true;
            }            
        }

        void DashAttack()
        {
            if (Main.player[NPC.target].CanHit(NPC) || moveTimer >= baseCooldown + 45)
            {
                if (moveTimer == baseCooldown)
                {
                    NPC.velocity.Y = -22;
                }

                if (moveTimer <= baseCooldown + 45)
                {
                    pickedTrajectory = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 18);

                    //Don't fall
                    if (NPC.velocity.Y > 0)
                    {
                        NPC.velocity.Y = 0;
                    }
                    NPC.velocity *= 0.9f;                   
                }

                if(moveTimer == baseCooldown + 45)
                {
                    for(int i = 0; i < 40; i++)
                    {
                        Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(30, 30);
                        Vector2 dustVel = pickedTrajectory.RotatedBy(MathHelper.Pi);
                        dustVel.Normalize();
                        dustVel *= 10;
                        Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 2f).noGravity = true;
                    }
                }

                if (moveTimer > baseCooldown + 45)
                {
                    //Don't keep dashing if too close to a wall
                    if (Collision.CanHitLine(NPC.Center, 2, 2, NPC.Center + pickedTrajectory * 4, 2, 2))
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(30, 30);
                            Vector2 dustVel = pickedTrajectory.RotatedBy(MathHelper.Pi);
                            dustVel.Normalize();
                            dustVel *= 10;
                            Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1.4f).noGravity = true;
                        }
                        NPC.velocity = pickedTrajectory;
                    }
                    else
                    {
                        pickedTrajectory = Vector2.Zero;
                    }
                }


                if (moveTimer < baseCooldown + 160 && moveTimer >= baseCooldown + 45)
                { 
                    if (moveTimer % 10 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (gaibonDead)
                            {
                                int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.BurningSphere, 0);
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
                }
                
                if(moveTimer > baseCooldown + 71 && pickedTrajectory == Vector2.Zero)
                {
                    moveTimer = 0;
                    dashAttack = !dashAttack;
                }
            }
            else
            {
                if (moveTimer == baseCooldown)
                {
                    tsorcRevampAIs.FighterAI(NPC, 7, 0.2f, 0.2f, true);
                    moveTimer--; //If it is are doing the dash attack and don't have line of sight, delay the attack until it does
                }
            }
        }

        void JumpAttack()
        {
            
            if (moveTimer <= baseCooldown + 70)
            {
                pickedTrajectory = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 12, 0.035f, false, false);
                pickedTrajectory.Y -= 12;
            }
            if (moveTimer == baseCooldown + 65)
            {
                NPC.velocity.Y = -5;
            }
            if (moveTimer > baseCooldown + 70 && moveTimer < baseCooldown + 115)
            {
                for (int i = 0; i < 7; i++)
                {
                    Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(30, 30);
                    Vector2 dustVel = NPC.velocity.RotatedBy(MathHelper.Pi);
                    dustVel.Normalize();
                    dustVel *= 10;
                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1.4f).noGravity = true;
                }
            }
           

            if (moveTimer < baseCooldown + 70)
            {
                NPC.velocity *= 0.9f;
            }
            else if (moveTimer == baseCooldown + 70)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(30, 30);
                    Vector2 dustVel = pickedTrajectory.RotatedBy(MathHelper.Pi);
                    dustVel.Normalize();
                    dustVel *= 10;
                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 2f).noGravity = true;
                }
                NPC.velocity = pickedTrajectory;

                if (NPC.velocity == Vector2.Zero) //Then there wasn't a valid ballistic trajectory. Just fling itself in the vague direction of the player instead.
                {
                    NPC.velocity.X = 12 * NPC.direction;
                    NPC.velocity.Y = 12;
                }
            }
            else if (moveTimer < baseCooldown + 160)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (moveTimer % 15 == 0)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 7), ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), tridentDamage, 0.5f, Main.myPlayer);                        
                    }

                    if (moveTimer % 15 == 7)
                    {
                        if (gaibonDead)
                        {
                            int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.BurningSphere, 0);
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
            }
            else
            {
                moveTimer = 0;
                dashAttack = !dashAttack;
            }
            
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (gaibonDead)
            {
                damage = (int)(damage * 1.5f);
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (gaibonDead)
            {
                damage = (int)(damage * 1.5f);
            }

            if (projectile.minion)
            {
                knockback = 0;
            }
        }

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (gaibonDead)
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
                Vector2 offset = new Vector2(0, -8);
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition + offset, sourceRectangle, Color.White, NPC.rotation, origin, 1.3f, effects, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
            }
            float projectileDelay = 120;
            if (gaibonDead)
            {
                projectileDelay = 90;
            }
            if (moveTimer % projectileDelay <= 30 && moveTimer < baseCooldown)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Gaibon>()))
                {
                    ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
                    data.Apply(null);
                }



                if (Projectiles.Enemy.EarthTrident.texture != null && !Projectiles.Enemy.EarthTrident.texture.IsDisposed)
                {
                    float rotation = 0;
                    if(NPC.direction == 1)
                    {
                        rotation += 0.15f;
                    }
                    else
                    {
                        rotation -= 0.15f;
                    }
                    Rectangle sourceRectangle = new Rectangle(0, 0, Projectiles.Enemy.EarthTrident.texture.Width, Projectiles.Enemy.EarthTrident.texture.Height);
                    Vector2 origin = sourceRectangle.Size() / 2f;
                    Main.EntitySpriteDraw(Projectiles.Enemy.EarthTrident.texture,
                        NPC.Center - Main.screenPosition,
                        sourceRectangle, Color.White, rotation, origin, 1, SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                }
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

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(new ItemDropWithConditionRule(ModContent.ItemType<Items.BossBags.SlograBag>(), 1, 1, 1, new SlograDropCondition()));
        }
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Slogra Gore 1").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Slogra Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Slogra Gore 3").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Slogra Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Slogra Gore 3").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
            }
            if (!NPC.AnyNPCs(ModContent.NPCType<Gaibon>()))
            {
                if (!Main.expertMode)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.PoisonbiteRing>(), 1);
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.BloodbiteRing>(), 1);
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<DarkSoul>(), 700);
                }
            }
            else
            {
                int slograID = NPC.FindFirstNPC(ModContent.NPCType<Gaibon>());
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
                    Dust dustObj = Dust.NewDustPerfect(dustPos, 262, dustSpeed, 200, default, 3);
                    dustObj.noGravity = true;
                }
            }
        }
    }

    public class SlograDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<Gaibon>());
        }

        public bool CanShowItemDropInUI()
        {
            return false;
        }

        public string GetConditionDescription()
        {
            return "Drops if Gaibon is dead";
        }
    }
}