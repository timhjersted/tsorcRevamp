using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class Death : ModNPC
    {
        int shadowShotDamage = 98;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.ShadowFlame] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<CrimsonBurn>()] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<DarkInferno>()] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.aiStyle = 0;
            NPC.width = 100;
            NPC.height = 100;
            NPC.damage = 254;
            NPC.defense = 45;
            NPC.scale = 1.1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 32000;
            NPC.friendly = false;
            NPC.boss = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0;
            NPC.value = 150000;

            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Death.DespawnHandler"), Color.DarkMagenta, DustID.Demonite);
        }



        float nextWarpAngle = 0;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            NPC.netUpdate = false;
            NPC.ai[0]++; // Timer Scythe
            NPC.ai[1]++; // Timer Teleport
                         // npc.ai[2] Shots

            Lighting.AddLight(NPC.Center, Color.MediumPurple.ToVector3() * 2);
            //great dust for bright effect that can be color matched
            int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.AncientLight, NPC.velocity.X, NPC.velocity.Y, 150, Color.Purple, 0.9f);
            Main.dust[dust2].noGravity = true;

            if (NPC.life > 5000)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 180, color, 4f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= 5000)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 150, color, 6f);
                Main.dust[dust].noGravity = true;
            }


            if (NPC.ai[0] >= 12 && NPC.ai[2] < 5)
            {
                float speed = 0.5f;
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                int type = ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>();
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * speed) * -1), (float)((Math.Sin(rotation) * speed) * -1), type, shadowShotDamage, 0f, Main.myPlayer);
                }
                NPC.ai[0] = 0;
                NPC.ai[2]++;
            }


            if (NPC.ai[1] >= 40)
            {
                NPC.velocity.X *= 0.97f;
                NPC.velocity.Y *= 0.97f;
            }

            if ((NPC.ai[1] >= 150 && NPC.life > 2000) || (NPC.ai[1] >= 100 && NPC.life <= 2000))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int i = 0; i < 10; i++)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, color, 4f);
                    Main.dust[dust].noGravity = true;
                }

                NPC.ai[2] = 0;
                NPC.ai[1] = 0;

                NPC.position.X = Main.player[NPC.target].position.X + (float)((600 * Math.Cos(nextWarpAngle)) * -1);
                NPC.position.Y = Main.player[NPC.target].position.Y + (float)((600 * Math.Sin(nextWarpAngle)) * -1);
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                NPC.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
                nextWarpAngle = (float)(Main.rand.Next(360) * (Math.PI / 180));
                NPC.netUpdate = true;
            }
            //this made death always look in one direction
            //if (NPC.velocity.X > 0)
            //{
            //     NPC.spriteDirection = 1;
            // }
            // else NPC.spriteDirection = -1;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(nextWarpAngle);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            nextWarpAngle = reader.ReadSingle();
        }
        public override void FindFrame(int currentFrame)
        {
            NPC.frameCounter += 0.25f;

            if (NPC.frameCounter >= 4)
            {
                NPC.frameCounter = 0;
            }

            int frameHeight = 1;
            if (!Main.dedServ)
            {
                frameHeight = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }


            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
            {
                NPC.frame.Y = frameHeight * (int)NPC.frameCounter;
            }
            else
            {
                NPC.frame.Y = frameHeight * ((int)NPC.frameCounter + 4);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Death Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Death Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Death Gore 3").Type, 1f);
                }
                for (int i = 0; i < 50; i++)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, color, 4f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }
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
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.DeathBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>(), 1, 2, 4));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<GreatMagicShieldScroll>(), 6));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MagicBarrierScroll>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Laevateinn>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Humanity>()));
            npcLoot.Add(notExpertCondition);
        }
    }
}