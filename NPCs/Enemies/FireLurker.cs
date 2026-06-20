using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    class FireLurker : ModNPC
    {
        const int MaxFlameOrbs = 3;
        const int FlameOrbSpawnRate = 3 * 60;
        const int FlameTrapRate = 10 * 60;
        const int ReleasedFlameOrbTime = 3 * 60;

        public int lostSoulDamage = 8;
        int flameOrbTimer;
        int flameTrapTimer;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            AnimationType = 28;
            NPC.knockBackResist = 0.4f;
            NPC.aiStyle = -1;//was 3
            NPC.damage = 20;
            NPC.defense = 10;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 120;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 600; // was 43 for 100 life
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.FireLurkerBanner>();
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 60; // Phase 2: SmartFighter4AI movement
            globalNPC.KiteRangeMin = 10f;
            globalNPC.KiteRangeMax = 25f;
            globalNPC.KiteLooseness = 0.5f;
            UsefulFunctions.AddAttack(NPC, 6 * 60, ModContent.ProjectileType<Projectiles.Enemy.FireLurkerFlameOrb>(), lostSoulDamage, 2.1f, SoundID.Item20 with { Volume = 0.35f, Pitch = -0.2f }, 0, -1, -ReleasedFlameOrbTime, telegraphColor: Color.Orange, telegraphTime: 25);

            if (Main.hardMode)
            {
                NPC.lifeMax = 250;
                NPC.defense = 22;
                NPC.value = 1250; // was 65
                NPC.damage = 30;
                lostSoulDamage = 22;
                NPC.knockBackResist = 0.2f;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1200;
                NPC.defense = 70;
                NPC.value = 5000; // was 365 with 1330 life
                NPC.damage = 65;
                lostSoulDamage = 35;
                NPC.knockBackResist = 0.05f;

            }
        }



        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //These are mostly redundant with the new zone definitions, but it still works.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = P.ZoneOverworldHeight;
            bool InBrownLayer = P.ZoneDirtLayerHeight;
            bool InGrayLayer = P.ZoneRockLayerHeight;
            bool InHell = P.ZoneUnderworldHeight;
            bool Ocean = spawnInfo.SpawnTileX < 3600 || spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;
            // P.townNPCs > 0f // is no town NPCs nearby

            if (spawnInfo.Invasion)
            {
                return 0;
            }

            if (spawnInfo.Water) return 0f;

            //ONLY SPAWNS IN HELL PRE-HM, THEN CAN SPAWN IN CORRUPTION IN HM
            if (!Main.hardMode && InHell && Main.rand.NextBool(6)) return 1;

            if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && InHell && Main.rand.NextBool(5)) return 1;

            if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && Corruption && Main.rand.NextBool(20)) return 1;

            if (tsorcRevampWorld.SuperHardMode && InHell && Main.rand.NextBool(20)) return 1; //8 is 3%, 5 is 5, 3 IS 3%???
            return 0;
        }
        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.07f, canTeleport: true, randomSound: SoundID.Mummy, soundFrequency: 1000, enragePercent: 0.36f, enrageTopSpeed: 3f, lavaJumping: true); //sound type was 26

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            SpawnFlameOrbs();
            SpawnFlameTrap();
        }

        void SpawnFlameOrbs()
        {
            flameOrbTimer++;
            if (flameOrbTimer < FlameOrbSpawnRate)
            {
                return;
            }
            flameOrbTimer = 0;

            int slot = GetOpenFlameOrbSlot();
            if (slot == -1)
            {
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Top + new Vector2(0, -56), Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.FireLurkerFlameOrb>(), lostSoulDamage, 0, Main.myPlayer, NPC.whoAmI, slot);
        }

        int GetOpenFlameOrbSlot()
        {
            bool[] occupiedSlots = new bool[MaxFlameOrbs];
            int activeOrbs = 0;
            int orbType = ModContent.ProjectileType<Projectiles.Enemy.FireLurkerFlameOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != orbType || (int)projectile.ai[0] != NPC.whoAmI)
                {
                    continue;
                }

                activeOrbs++;
                if (projectile.ai[1] >= 0)
                {
                    int slot = (int)projectile.ai[1];
                    if (slot >= 0 && slot < MaxFlameOrbs)
                    {
                        occupiedSlots[slot] = true;
                    }
                }
            }

            if (activeOrbs >= MaxFlameOrbs)
            {
                return -1;
            }

            for (int i = 0; i < MaxFlameOrbs; i++)
            {
                if (!occupiedSlots[i])
                {
                    return i;
                }
            }

            return -1;
        }

        void SpawnFlameTrap()
        {
            flameTrapTimer++;
            if (flameTrapTimer < FlameTrapRate)
            {
                return;
            }
            flameTrapTimer = 0;

            NPC.TargetClosest(true);
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                return;
            }

            Vector2 flameTrapPosition = target.Center - new Vector2(target.direction * 62f, 0);
            Projectile.NewProjectile(NPC.GetSource_FromAI(), flameTrapPosition, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.FireLurkerMeteor>(), NPC.damage / 2, 0, Main.myPlayer, lostSoulDamage);
        }

        void ReleaseFlameOrbs()
        {
            NPC.TargetClosest(true);
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                return;
            }

            int orbType = ModContent.ProjectileType<Projectiles.Enemy.FireLurkerFlameOrb>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != orbType || (int)projectile.ai[0] != NPC.whoAmI || projectile.ai[1] < 0)
                {
                    continue;
                }

                projectile.ai[1] = -3 * 60;
                projectile.velocity = UsefulFunctions.Aim(projectile.Center, target.Center, 1.4f);
                projectile.timeLeft = 3 * 60;
                projectile.netUpdate = true;
            }
        }

        #region Find Frame
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.Y == 0f)
            {
                if (NPC.direction == 1)
                {
                    NPC.spriteDirection = 1;
                }
                if (NPC.direction == -1)
                {
                    NPC.spriteDirection = -1;
                }
                if (NPC.velocity.X == 0f)
                {
                    NPC.frame.Y = 0;
                    NPC.frameCounter = 0.0;
                }
                else
                {
                    NPC.frameCounter += (double)(Math.Abs(NPC.velocity.X) * 2f);
                    NPC.frameCounter += 1.0;
                    if (NPC.frameCounter > 6.0)
                    {
                        NPC.frame.Y = NPC.frame.Y + num;
                        NPC.frameCounter = 0.0;
                    }
                    if (NPC.frame.Y / num >= Main.npcFrameCount[NPC.type])
                    {
                        NPC.frame.Y = num * 2;
                    }
                }
            }
            else
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y = num;
                NPC.frame.Y = 0;
            }
        }

        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.OnFire, 10 * 60, false);
            }

            if (Main.rand.NextBool(10))
            {
                target.AddBuff(BuffID.BrokenArmor, 10 * 60, false);
                target.AddBuff(BuffID.Darkness, 3 * 60, false);
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 300 * 60, false); //-20 life if counter hits 100
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ReleaseFlameOrbs();
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            ReleaseFlameOrbs();
        }
        #endregion

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("FireLurkerGore3").Type, 1f);
                for (int i = 0; i < 10; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Blood Splat").Type, 1.1f);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
                if (target.active && !target.dead)
                {
                    Vector2 lostSoulVelocity = UsefulFunctions.Aim(NPC.Center, target.Center, 6f);
                    Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, lostSoulVelocity, ProjectileID.LostSoulHostile, lostSoulDamage, 0, Main.myPlayer);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 12));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 100, 1, 1, 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.FadingSoul>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 10));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 16));
            IItemDropRule drop = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 1, 2);
            SuperHardmodeRule SHM = new();
            IItemDropRule condition = new LeadingConditionRule(SHM);
            condition.OnSuccess(drop);
            npcLoot.Add(condition);
        }

        /*
		#region Draw Projectile
		static Texture2D spearTexture;
		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (spearTexture == null || spearTexture.IsDisposed)
			{
				spearTexture = (Texture2D)ModContent.Request<Texture2D>("Projectiles/Enemy/EnemyBioSpitBall");
			}
			if (customAi1 >= 120)
			{
				Lighting.AddLight(npc.Center, Color.Green.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.NextBool(3))
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, npc.velocity.X, npc.velocity.Y);
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.IchorTorch, npc.velocity.X, npc.velocity.Y);
				}

				SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				if (npc.spriteDirection == -1)
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 10), npc.scale, effects, 0); // facing left (8, 38 work)
				}
				else
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 13), npc.scale, effects, 0); // facing right, first value is height, higher number is higher, 2nd value is width axis
				
				}
			}
		}
		#endregion
		*/
    }
}
