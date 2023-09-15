using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using static tsorcRevamp.oSpawnHelper;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;
using Terraria.DataStructures;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.NPCs.Enemies
{
    class BarrowWight : ModNPC
    {
        bool chargeDamageFlag = false;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.Poisoned,
                    BuffID.OnFire,
                    BuffID.Confused,
                    BuffID.CursedInferno
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.width = 58;
            NPC.height = 48;
            NPC.aiStyle = 22;
            NPC.damage = 30;
            NPC.defense = 0;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 180;
            NPC.knockBackResist = 0;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = 900; // was 85
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BarrowWightBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {

            Player p = spawnInfo.Player;
            int playerXTile = (int)(p.Bottom.X + 8f) / 16;
            if (p.townNPCs > 0f || p.ZoneMeteor) return 0;
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPCID.SkeletronHead)) && (oSurface(p) || oUnderSurface(p) || oUnderground(p) || oCavern(p)) && (playerXTile > Main.maxTilesX * 0.2f && playerXTile < Main.maxTilesX * 0.35f || playerXTile > Main.maxTilesX * 0.65f && playerXTile < Main.maxTilesX * 0.8f)) return 0.005f;
            if (!Main.hardMode && p.ZoneDungeon) return .00833f;
            if (!tsorcRevampWorld.SuperHardMode && Main.hardMode && oSky(p)) return 0.0567f;
            if (!tsorcRevampWorld.SuperHardMode && Main.hardMode && p.ZoneDungeon) return 0.033f;
            if (tsorcRevampWorld.SuperHardMode && oSky(p)) return 0.025f;
            if (tsorcRevampWorld.SuperHardMode && p.ZoneDungeon) return 0.008f;

            return 0;
        }


        int chargeTelegraphTimer = 0;

        public override void AI()
        {
            //the following line makes barrow wights completely break in multiplayer. it has been modified.
            //npc.ai[1] += Main.rand.Next(2, 5) * 0.1f * npc.scale;

            NPC.ai[1] += 0.3f;
            if (NPC.ai[1] >= 10f)
            {
                NPC.TargetClosest(true);
                

                // charge forward code 
                if (Main.rand.NextBool(400) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    chargeDamageFlag = true;
                    
                }
                if (chargeDamageFlag == true)
                {
                    chargeTelegraphTimer++;
                    Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    if (Main.rand.NextBool(2))
                    {
                        int pink = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 1.5f);

                        Main.dust[pink].noGravity = true;
                    }

                    if (chargeTelegraphTimer >= 120 && chargeTelegraphTimer <= 130)
                    {

                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = (float)(Math.Cos(rotation) * 7) * -1; //7 was 11
                        NPC.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                        NPC.ai[1] = 1f;

                        NPC.netUpdate = true;
                    }

                    if (chargeTelegraphTimer > 130)
                    {
                        chargeDamageFlag = false;
                        chargeTelegraphTimer = 0;     
                    }

                }
                




            }
            if (NPC.justHit)
            {
                NPC.ai[2] = 0f;
            }
            if (NPC.ai[2] >= 0f)
            {
                int num258 = 16;
                bool flag26 = false;
                bool flag27 = false;
                if (NPC.position.X > NPC.ai[0] - (float)num258 && NPC.position.X < NPC.ai[0] + (float)num258)
                {
                    flag26 = true;
                }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                    {
                        flag26 = true;
                    }
                }
                num258 += 24;
                if (NPC.position.Y > NPC.ai[1] - (float)num258 && NPC.position.Y < NPC.ai[1] + (float)num258)
                {
                    flag27 = true;
                }
                if (flag26 && flag27)
                {
                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.ai[2] = -200f;
                        NPC.direction *= -1;
                        NPC.velocity.X = NPC.velocity.X * -1f;
                        NPC.collideX = false;
                    }
                }
                else
                {
                    NPC.ai[0] = NPC.position.X;
                    NPC.ai[1] = NPC.position.Y;
                    NPC.ai[2] = 0f;
                }
                NPC.TargetClosest(true);
            }
            else
            {
                NPC.ai[2] += 1f;
                if (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) > NPC.position.X + (float)(NPC.width / 2))
                {
                    NPC.direction = -1;
                }
                else
                {
                    NPC.direction = 1;
                }
            }
            if (NPC.position.Y > Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y -= .05f;
            }
            if (NPC.position.Y < Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y += .05f;
            }
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.4f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                {
                    NPC.velocity.X = 1f;
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                {
                    NPC.velocity.X = -1f;
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 1f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -1f;
                }
            }
            float topSpeed = .5f;
            if (NPC.direction == -1 && NPC.velocity.X > -topSpeed)
            {
                NPC.velocity.X = NPC.velocity.X - 0.1f;
                if (NPC.velocity.X > topSpeed)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.1f;
                }
                else
                {
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.05f;
                    }
                }
                if (NPC.velocity.X < -topSpeed)
                {
                    NPC.velocity.X = -topSpeed;
                }
            }
            else
            {
                if (NPC.direction == 1 && NPC.velocity.X < topSpeed)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.1f;
                    if (NPC.velocity.X < -topSpeed)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.1f;
                    }
                    else
                    {
                        if (NPC.velocity.X < 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X - 0.05f;
                        }
                    }
                    if (NPC.velocity.X > topSpeed)
                    {
                        NPC.velocity.X = topSpeed;
                    }
                }
            }
            if (NPC.directionY == -1 && NPC.velocity.Y > -2.5)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if (NPC.velocity.Y > 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.05f;
                }
                else
                {
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.03f;
                    }
                }
                if (NPC.velocity.Y < -2.5)
                {
                    NPC.velocity.Y = -2.5f;
                }
            }
            else
            {
                if (NPC.directionY == 1 && NPC.velocity.Y < 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                    if (NPC.velocity.Y < -2.5)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.05f;
                    }
                    else
                    {
                        if (NPC.velocity.Y < 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y - 0.03f;
                        }
                    }
                    if (NPC.velocity.Y > 2.5)
                    {
                        NPC.velocity.Y = 2.5f;
                    }
                }
            }
            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0.25f);
            return;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.BrokenArmor, 20 * 60);
                target.AddBuff(BuffID.Chilled, 20 * 60);
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 600 * 60);
            }
        }

        #region Frames
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
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
        #endregion

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Barrow Wight Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Barrow Wight Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Barrow Wight Gore 2").Type, 1f);
                }
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 45, 0.3f, 0.3f, 200, default, 1f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default, 3f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 45, 0.2f, 0.2f, 200, default, 4f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default, 4f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default, 4f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OilPot>(), 1, 1, 4));
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 55));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.SpelunkerPotion, 35));       
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 40));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Summon.SpiritBell>(), 20));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofFlight));
            npcLoot.Add(hmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.SuperHardmodeRule, ModContent.ItemType<BarrowBlade>(), 5));
        }

        /* what the hell IS this? i cant find anything about it
         public int MagicDefenseValue() 
            { 
                return 5;
            } 
          
         */
    }
}
