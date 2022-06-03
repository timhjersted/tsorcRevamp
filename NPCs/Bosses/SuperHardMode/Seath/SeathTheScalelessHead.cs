using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath
{
    [AutoloadBossHead]
    class SeathTheScalelessHead : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 44; //44 works for both
            NPC.height = 44; //was 32 tried 64
            DrawOffsetY = 49; //was 60
            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 22500;
            NPC.damage = 130;
            NPC.defense = 50;
            NPC.HitSound = SoundID.NPCHit7;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.lifeMax = 125000;
            Music = 12;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 500000;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = false;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            despawnHandler = new NPCDespawnHandler("Seath consumes your soul...", Color.Cyan, DustID.BlueFairy);
        }


        int breathDamage = 33;
        int frozenTearDamage = 44;
        int meteorDamage = 50;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage / 2);
            breathDamage = (int)(breathDamage / 2);
            frozenTearDamage = (int)(frozenTearDamage / 2);
            meteorDamage = (int)(meteorDamage / 2);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seath the Scaleless");
        }


        int breathCD = 110;
        bool breath = false;
        bool firstCrystalSpawned = false;
        bool secondCrystalSpawned = false;
        bool finalCrystalsSpawned = false;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

            bool Sky = spawnInfo.SpawnTileY <= (Main.rockLayer * 4);
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
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;


            if (tsorcRevampWorld.SuperHardMode && (Sky || AboveEarth) && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<SeathTheScalelessHead>()) && FrozenOcean && Main.rand.Next(100) == 1) return 1;

            if (Main.hardMode && P.townNPCs > 2f && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Artorias>()) && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<SeathTheScalelessHead>()) && !Main.dayTime && Main.rand.Next(1000) == 1)
            {
                UsefulFunctions.BroadcastText("The village is under attack!", 175, 75, 255);
                UsefulFunctions.BroadcastText("Seath the Scaleless has come to destroy all...", 175, 75, 255);
                return 1;
            }
            return 0;
        }
        #endregion



        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (NPC.AnyNPCs(ModContent.NPCType<PrimordialCrystal>()))
            {
                NPC.dontTakeDamage = true;
            }
            else
            {
                NPC.dontTakeDamage = false;
            }

            //Crystal spawning
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Collision.CanHit(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1) || Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                {
                    int crystalVelocity = 16;
                    if (!firstCrystalSpawned && NPC.life <= (2 * NPC.lifeMax / 3) || !secondCrystalSpawned && NPC.life <= (NPC.lifeMax / 3))
                    {
                        int crystal = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X, (int)NPC.position.Y, ModContent.NPCType<PrimordialCrystal>(), default, NPC.whoAmI);
                        Main.npc[crystal].velocity = Main.rand.NextVector2CircularEdge(-crystalVelocity, crystalVelocity);
                        if (NPC.life >= (NPC.lifeMax / 2))
                        {
                            firstCrystalSpawned = true;
                        }
                        else
                        {
                            secondCrystalSpawned = true;
                        }
                        UsefulFunctions.BroadcastText("Seath calls upon a Primordial Crystal...", Color.Cyan);
                    }

                    if (!finalCrystalsSpawned && NPC.life <= (NPC.lifeMax / 6))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int crystal = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X, (int)NPC.position.Y, ModContent.NPCType<PrimordialCrystal>(), default, NPC.whoAmI);
                            Main.npc[crystal].velocity = Main.rand.NextVector2CircularEdge(-crystalVelocity, crystalVelocity);
                        }
                        finalCrystalsSpawned = true;
                        UsefulFunctions.BroadcastText("Seath calls upon his final Primordial Crystals...", Color.Cyan);
                    }
                }
            }

            Player nT = Main.player[NPC.target];
            if (Main.rand.Next(255) == 0)
            {
                breath = true;
                // Terraria.Audio.SoundEngine.PlaySound(15, -1, -1, 0); This roar sound got very annoying
            }
            if (breath)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //PROJECTILE START POINT - 108f
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), npc.position.X + (float)npc.width / 2f, npc.position.Y + (float)npc.height / 2f, npc.velocity.X * 3f + (float)Main.rand.Next(-2, 3), npc.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.FrozenDragonsBreath>(), breathDamage, 1.2f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.position.X + (float)NPC.width, NPC.position.Y + (float)NPC.height * 0.5f, NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.FrozenDragonsBreath>(), breathDamage, 1.2f, Main.myPlayer);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                breathCD--;

            }
            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 110;
                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
            }
            if (Main.rand.Next(820) == 0)
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 800 + Main.rand.Next(1600), (float)nT.position.Y - 500f, (float)(-40 + Main.rand.Next(80)) / 10, 10.1f, ModContent.ProjectileType<Projectiles.Enemy.FrozenTear>(), frozenTearDamage, 2f, Main.myPlayer); //10.1f was 14.9f is speed
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                }
            }
            if (Main.rand.Next(1560) == 0)
            {
                for (int pcy = 0; pcy < 10; pcy++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)nT.position.X - 500 + Main.rand.Next(1000), (float)nT.position.Y - 500f, (float)(-100 + Main.rand.Next(200)) / 10, 11.5f, ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), meteorDamage, 2f, Main.myPlayer); //9.5f was 14.9f
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
            }
            if (Main.rand.Next(2) == 0)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.BlueFairy, NPC.velocity.X / 4f, NPC.velocity.Y / 4f, 100, default(Color), 1f);
                Main.dust[d].noGravity = true;
            }

            int[] bodyTypes = new int[] { ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody2>(), ModContent.NPCType<SeathTheScalelessBody3>(), ModContent.NPCType<SeathTheScalelessBody3>() };
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<SeathTheScalelessHead>(), bodyTypes, ModContent.NPCType<SeathTheScalelessTail>(), 13, 6f, 10f, 0.17f, true, false, true, false, false);
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

        //Make it take damage as if its whole body was one entity
        //Whenever any of its parts takes a hit, it sets all other living parts to be immune for 10 frames
        //Does *not* apply to true melee attacks! 100% intentional, easy to change by calling SetImmune in OnHitByItem too if necessary
        public static void SetImmune(Projectile projectile, NPC hitNPC)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC currentNPC = Main.npc[i];
                if (currentNPC.type == ModContent.NPCType<SeathTheScalelessHead>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessBody>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessBody2>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessBody3>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessLegs>() || currentNPC.type == ModContent.NPCType<SeathTheScalelessTail>())
                {
                    currentNPC.immune[projectile.owner] = 10;
                }
            }
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            SetImmune(projectile, NPC);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.SeathBag>()));
        }
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (NPC.life <= 0)
            {
                //Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), "Seath the Scaleless Head Gore", 1f, -1);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Seath the Scaleless Head Gore").Type, 1f);
                //Main.gore[a].timeLeft = 1800; int a = Gore.New..etc

                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DragonEssence>(), 35 + Main.rand.Next(5));
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 7000);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.BequeathedSoul>(), 2);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.BlueTearstoneRing>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.PurgingStone>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.DragonWings>());
                //npc.netUpdate = true;
            }
        }

        public static Texture2D texture;
        public static void SeathInvulnerableEffect(NPC npc, SpriteBatch spriteBatch, ref Texture2D texture, float scale = 1.5f)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(npc.ModNPC.Texture);
            }
            if (npc.dontTakeDamage)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer);
                data.Apply(null);
                SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                Vector2 origin = sourceRectangle.Size() / 2f;
                spriteBatch.Draw(texture, npc.Center - Main.screenPosition, sourceRectangle, Color.White, npc.rotation, origin, scale, effects, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);

            }

        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            SeathInvulnerableEffect(NPC, spriteBatch, ref texture, 1.1f);
            return true;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            SeathTheScalelessHead.SeathInvulnerableEffect(NPC, spriteBatch, ref texture, 1.1f);
        }
    }
}