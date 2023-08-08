using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    class Warlock : ModNPC
    {
        int greatEnergyBeamDamage = 18;
        int energyBallDamage = 18;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            AnimationType = 21;
            NPC.knockBackResist = 0.1f;
            NPC.aiStyle = 3;
            NPC.damage = 0;
            NPC.npcSlots = 5;
            NPC.defense = 5;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 1500;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit37;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 15000; // rare enemy so not dividing health by 2 : was 1575
            NPC.rarity = 3;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.WarlockBanner>();
            if (!Main.hardMode)
            {
                NPC.damage = 20;
                NPC.lifeMax = 750;
                NPC.value = 12500;
            }
            UsefulFunctions.AddAttack(NPC, 140, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatEnergyBall>(), energyBallDamage, 8, SoundID.Item28 with { Volume = 0.2f, Pitch = -0.8f });
            UsefulFunctions.AddAttack(NPC, 250, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatEnergyBeamBall>(), greatEnergyBeamDamage, 8, weight: 0.3f);
            UsefulFunctions.AddAttack(NPC, 140, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 1, 0, SoundID.Item17, needsLineOfSight: false, weight: 0.2f);
        }
        //Spawn in the Cavern, mostly before 3/10th and after 7/10th (Width). Does not spawn in the Dungeon, Jungle, Meteor, or if there are Town NPCs
        #region Spawn

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (!NPC.downedBoss1)
            {
                return 0;
            }

            Player P = spawnInfo.Player; //These are mostly redundant with the new zone definitions, but it still works.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = P.ZoneOverworldHeight;
            bool oUnderground = P.ZoneDirtLayerHeight;
            bool oCavern = P.ZoneRockLayerHeight;
            bool InHell = P.ZoneUnderworldHeight;
            bool Ocean = spawnInfo.SpawnTileX < 3600 || spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;
            
            //Ensuring it can't spawn if one already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 0)
                    {
                        return 0;
                    }
                }
            }
            if (!Main.hardMode && oCavern)
            {
                if (Main.rand.NextBool(1200)) return 1;
                else if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.3f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.7f) && Main.rand.NextBool(430))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Warlock.Near"), 175, 75, 255);
                    return 1;
                }

            }
            if (Main.hardMode && (oCavern || oUnderground || Jungle))
            {
                if (Main.rand.NextBool(600)) return 1;
                else if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.3f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.7f) && Main.rand.NextBool(300))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Warlock.Hunt"), 175, 75, 255);
                    return 1;
                }
            }
            return 0;
        }
        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.8f, 0.03f, .2f, canTeleport: true, lavaJumping: true, canDodgeroll: true);


            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackSucceeded == 2)
            {
                NPC.life += 10;
                NPC.HealEffect(10);
                if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                NPC.netUpdate = true;
            }

            //IF HIT BEFORE PINK DUST TELEGRAPH, RESET TIMER, BUT CHANCE TO BREAK STUN LOCK
            //(WORKS WITH 2 TELEGRAPH DUSTS, AT 60 AND 110)
            if (NPC.justHit && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer <= 109)
            {
                if (Main.rand.NextBool(3))
                {
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 110;
                    NPC.netUpdate = true;
                }
                else
                {
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 0;
                    NPC.netUpdate = true;
                }
            }
            if (NPC.justHit && Main.rand.NextBool(8))
            {
                tsorcRevampAIs.QueueTeleport(NPC, 20, true, 60);
            }

            //Transparency. Higher alpha = more invisible
            if (NPC.justHit)
            {
                NPC.alpha = 0;
                NPC.netUpdate = true;
            }
            if (Main.rand.NextBool(230))
            {
                NPC.alpha = 0;
                NPC.netUpdate = true;
            }
            if (Main.rand.NextBool(150))
            {
                NPC.alpha = 230;
                NPC.netUpdate = true;
            }

            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0.4f, 0.4f);

            if (Main.rand.NextBool(600) && NPC.CountNPCS(NPCID.IlluminantBat) < 5)
            {
                int bat = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.IlluminantBat);
                if (!Main.hardMode)
                {
                    Main.npc[bat].life /= 2;
                    Main.npc[bat].lifeMax /= 2;
                    Main.npc[bat].damage /= 2;

                }
                Main.npc[bat].netUpdate = true;
            }

            //BIG ATTACK DUST CIRCLE
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer > 180)
            {
                for (int j = 0; j < 10; j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(32, 32);
                    Vector2 dustPos = NPC.Center + dir;
                    Vector2 dustVel = new Vector2(5, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                    Dust.NewDustPerfect(dustPos, DustID.MagicMirror, dustVel, 200).noGravity = true;
                }
            }
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Warlock Gore 3").Type, 1.1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.LifeforcePotion));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 10));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 3, 2, 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.UnicornHorn, 3));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofLight, 1, 3, 6));
            npcLoot.Add(hmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 2));

            // took all this from an enemy that it didn't make sense for it to have
            npcLoot.Add(ItemDropRule.Common(ItemID.BattlePotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.WaterWalkingPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.SpelunkerPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.GillsPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.HunterPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.ArcheryPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 15));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 10)); //not a typo
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 8, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 10, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FadingSoul>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 8));      

        }
    }
}