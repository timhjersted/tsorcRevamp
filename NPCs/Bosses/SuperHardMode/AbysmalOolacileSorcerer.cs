using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Audio;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Utilities;
using tsorcRevamp.Projectiles.Enemy.OolacileSorcerer;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class AbysmalOolacileSorcerer : ModNPC
    {
        int darkBeadDamage = 56;
        int darkOrbDamage = 59;
        int seekerDamage = 50;
        public static Effect effect;
        float auraBonus = 1f;
        int expandRadius = 0;
        private int currentAttackType = 0;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;

            // Set NeedsExpertScaling for lifeMax scaling in multiplayer, otherwise scaling is skipped since NPC.damage=0.
            NPCID.Sets.NeedsExpertScaling[NPC.type] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 0;
            NPC.defense = 80;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 230000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.boss = true;
            NPC.scale = 1.35f;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = 430000;
            NPC.rarity =38;
            NPC.width = 28;
            NPC.knockBackResist = 0f;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.AbysmalOolacileSorcerer.DespawnHandler"), Color.DarkRed, DustID.Firework_Red);
        }
        public float DarkBeadShotTimer
        {
            get => NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public float TeleportTimer
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        public float DarkBeadShotCounter
        {
            get => NPC.ai[2];
            set => NPC.ai[2] = value;
        }
        public float SecondAttackCounter
        {
            get => NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        float NPCSpawningTimer;
        float NPCSpawningTimer2;

        /*#region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = spawnInfo.SpawnTileY < Main.worldSurface;
            bool InBrownLayer = spawnInfo.SpawnTileY >= Main.worldSurface && spawnInfo.SpawnTileY < Main.rockLayer;
            bool InGrayLayer = spawnInfo.SpawnTileY >= Main.rockLayer && spawnInfo.SpawnTileY < (Main.maxTilesY - 200) * 16;
            bool InHell = spawnInfo.SpawnTileY >= (Main.maxTilesY - 200) * 16;
            bool Ocean = spawnInfo.SpawnTileX < 3600 || spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;

            // these are all the regular stuff you get , now lets see......

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Witchking>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AbysmalOolacileSorcerer>())) && AboveEarth && Main.rand.NextBool(200)) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Witchking>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AbysmalOolacileSorcerer>())) && InBrownLayer && Main.rand.NextBool(500)) return 1;

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Witchking>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AbysmalOolacileSorcerer>())) && AboveEarth && Main.rand.NextBool(50)) return 1;

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Witchking>())) && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AbysmalOolacileSorcerer>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Artorias>())) && AboveEarth && Main.rand.NextBool(2850)) return 1;

            return 0;
        }
        #endregion
        */


        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            DarkBeadShotTimer++; //Counts up each tick. Used to space out shots
            TeleportTimer++; //When this hits 200 (120 if low health) the boss teleports
            SecondAttackCounter++; //When this hits 60 the boss has will begin randomly deciding whether to fire extra projectiles.

            if (NPC.life > NPC.lifeMax / 4)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 210, Color.Black, 2f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 140, Color.Black, 3f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                FireProjectiles();
            }

            if (TeleportTimer >= 20) //How long it should float after teleporting before coming to a stop
            {
                NPC.velocity.X *= 0.27f;
                NPC.velocity.Y *= 0.17f;
            }

            OolacileTeleport();
        }

        public void FireProjectiles()
        {
            if (DarkBeadShotCounter == 0 && TeleportTimer <= 20) 
            {
                currentAttackType = Main.rand.Next(1, 4); 
                DarkBeadShotTimer = 0;
            }

            if (currentAttackType == 1)
            {
                if (DarkBeadShotTimer >= 9 && DarkBeadShotCounter < 6)
                {
                    Vector2 vel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 10f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                        ModContent.ProjectileType<OolacileBolt>(), darkBeadDamage, 0f, Main.myPlayer);
                    }
                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

                    DarkBeadShotCounter++;
                    DarkBeadShotTimer = 0;
                }
            }

            else if (currentAttackType == 2)
            {
                if (DarkBeadShotTimer >= 25 && DarkBeadShotCounter < 2)
                {
                    Vector2 dir = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1f);
                    float baseRot = dir.ToRotation();

                    for (int i = -1; i <= 1; i++)
                    {
                        float offset = MathHelper.ToRadians(16f * i);
                        Vector2 vel = baseRot.ToRotationVector2() * 10f;
                        vel = vel.RotatedBy(offset);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                            ModContent.ProjectileType<OolacileBolt>(), darkBeadDamage, 0f, Main.myPlayer);
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                    DarkBeadShotCounter++;
                    DarkBeadShotTimer = 0;
                }
            }

            else if (currentAttackType == 3)
            {
                if (DarkBeadShotCounter == 0 && DarkBeadShotTimer >= 55)
                {
                    Vector2 dir = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1f);
                    float baseRot = dir.ToRotation();

                    for (int i = -2; i <= 2; i++)
                    {
                        float offset = MathHelper.ToRadians(18f * i);
                        Vector2 vel = baseRot.ToRotationVector2() * 11f;
                        vel = vel.RotatedBy(offset);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                            ModContent.ProjectileType<OolacileBolt>(), darkBeadDamage, 0f, Main.myPlayer);
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

                    for (int i = 0; i < 30; i++)
                    {
                        Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(60, 60), 114, Main.rand.NextVector2CircularEdge(4, 4), Scale: 2f);
                        d.noGravity = true;
                    }

                    DarkBeadShotCounter = 1;
                    DarkBeadShotTimer = 0;
                }
            }

            if (SecondAttackCounter >= 60)
            {
                if (Main.rand.NextBool(240))
                {
                    Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 2);
                    projVelocity.Y -= 5;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.OolacileDarkOrb>(), darkOrbDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24, NPC.Center);
                    NPCSpawningTimer = 1f;
                    SecondAttackCounter = 0;
                }

                if (Main.rand.NextBool(30))
                {
                    Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 8);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.OolacileSeeker>(), seekerDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPCSpawningTimer = 1f;
                }
            }
        }

        public void OolacileTeleport()
        {
            //bool clearLineofSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);

            if ((TeleportTimer >= 150 && NPC.life > NPC.lifeMax / 4) || (TeleportTimer >= 120 && NPC.life <= NPC.lifeMax / 3))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int i = 0; i < 25; i++)
                {
                    Vector2 dustVel = Main.rand.NextVector2CircularEdge(14, 14);
                    int red = Dust.NewDust(NPC.position, NPC.width, NPC.height, 114, dustVel.X, dustVel.Y, Scale: 2f);
                    Main.dust[red].noGravity = true;
                }
                DarkBeadShotCounter = 0;
                TeleportTimer = 0;

                //region teleportation - can't believe I got this to work.. yayyyyy :D lol

                int target_x_blockpos = (int)Main.player[NPC.target].position.X / 16; // corner not center
                int target_y_blockpos = (int)Main.player[NPC.target].position.Y / 16; // corner not center
                int x_blockpos = (int)NPC.position.X / 16; // corner not center
                int y_blockpos = (int)NPC.position.Y / 16; // corner not center
                int tp_radius = 30; // radius around target(upper left corner) in blocks to teleport into
                int tp_counter = 0;
                bool endLoop = false;
                if (Math.Abs(NPC.position.X - Main.player[NPC.target].position.X) + Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 9000000f)
                { // far away from target; 4000 pixels = 250 blocks
                    tp_counter = 100;
                    endLoop = false; // always telleport was true for no teleport
                }
                while (!endLoop) // loop always ran full 100 time before I added "flag7 = true" below
                {
                    if (tp_counter >= 100) // run 100 times
                        break; //return;
                    tp_counter++;

                    int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                    int tp_y_target = Main.rand.Next((target_y_blockpos - tp_radius) - 62, (target_y_blockpos + tp_radius) - 26);  //  pick random tp point (centered on corner)
                    for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
                    { // (tp_x_target,m) is block under its feet I think
                        if ((m < target_y_blockpos - 21 || m > target_y_blockpos + 21 || tp_x_target < target_x_blockpos - 21 || tp_x_target > target_x_blockpos + 21) && (m < y_blockpos - 8 || m > y_blockpos + 8 || tp_x_target < x_blockpos - 8 || tp_x_target > x_blockpos + 8) && !Main.tile[tp_x_target, m].HasTile)
                        { // over 21 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                            bool safe_to_stand = true;
                            bool dark_caster = false; // not a fighter type AI...
                            if (dark_caster && Main.tile[tp_x_target, m - 1].WallType == 0) // Dark Caster & ?outdoors
                                safe_to_stand = false;
                            else if (Main.tile[tp_x_target, m - 1].LiquidType == LiquidID.Lava) // feet submerged in lava
                                safe_to_stand = false;

                            if (safe_to_stand && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                            { //  3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
                              // safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && // removed safe enviornment && solid below feet

                                NPC.position.X = (float)(tp_x_target * 16 - NPC.width / 2); // center x at target
                                NPC.position.Y = (float)(m * 16 - NPC.height); // y so block is under feet			
                                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 5 + (NPC.height / 2));
                                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                                NPC.velocity.X = (float)(Math.Cos(rotation) * 1) * -1;
                                NPC.velocity.Y = (float)(Math.Sin(rotation) * 1) * -1;

                                NPC.netUpdate = true;

                                //npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                endLoop = true; // end the loop (after testing every lower point :/)
                                TeleportTimer = 0;
                            }
                        } // END over 17 blocks distant from player...
                    } // END traverse y down to edge of radius
                } // END try 100 times
            }
        }

        #endregion
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.OolacileSorcererBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 2, 4));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HealingElixir>(), 1, 5, 10));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PurgingStone>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CursedSoul>(), 1, 5, 8));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfOccultist>(), 1, 3, 6));
            npcLoot.Add(notExpertCondition);
        }

        public override void FindFrame(int currentFrame)
        {
            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -1 && NPC.velocity.X < 1) && (NPC.velocity.Y > -1 && NPC.velocity.Y < 1))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else
            {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            auraBonus *= 0.9f;
            auraBonus += 0.1f; 

            Color rgbColor = new Color(130, 8, 30); 

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            float colorIntensity = 0.3f + auraBonus * 0.2f;
            int auraSize = 135 + expandRadius / 4 + 30; 

            Rectangle sourceRectangle = new Rectangle(0, 0, auraSize, auraSize);
            Vector2 origin = sourceRectangle.Size() / 2f;

            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseVoronoi.Width);
            effect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4() * colorIntensity * 1.2f);
            effect.Parameters["ringProgress"].SetValue(0.6f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 1.2f);
            effect.Parameters["scaleFactor"].SetValue(3.5f);

            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseVoronoi, NPC.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, NPC.scale * 1.1f, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return true;
        }

        #region Gore
        public override void OnKill()
        {
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.AbysmalOolacileSorcerer.Defeated"), 160, 160, 160);
            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1.1f });
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 1").Type, 1.35f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 2").Type, 1.35f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 3").Type, 1.35f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 2").Type, 1.35f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 3").Type, 1.35f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 1, UsefulFunctions.ColorToFloat(Color.OrangeRed));
            }
        }
        #endregion
    }
}