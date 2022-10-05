using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class Death : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            NPC.npcSlots = 10;
            NPC.aiStyle = 0;
            NPC.width = 68;
            NPC.height = 70;
            NPC.damage = 390;
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

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            //NPC.buffImmune[BuffID.OnFire] = true;
            despawnHandler = new NPCDespawnHandler("Death claims you at last...", Color.DarkMagenta, DustID.Demonite);
        }


        int shadowShotDamage = 150;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage * 1.3 / 2);
            NPC.defense = NPC.defense += 12;
            shadowShotDamage = (int)(shadowShotDamage * 1.3 / 2);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            for (int i = 0; i < 200; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Death>())
                {
                    return 0;
                }
            }
            if (Main.hardMode)
            {
                bool nospecialbiome = !spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && !spawnInfo.Player.ZoneHallow && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneDungeon; // Not necessary at all to use but needed to make all this work.

                bool sky = nospecialbiome && ((double)spawnInfo.Player.position.Y < Main.worldSurface * 0.44999998807907104);
                bool surface = nospecialbiome && !sky && (spawnInfo.Player.position.Y <= Main.worldSurface);
                bool underground = nospecialbiome && !surface && (spawnInfo.Player.position.Y <= Main.rockLayer);
                bool underworld = (spawnInfo.Player.position.Y > Main.maxTilesY - 190);
                bool cavern = nospecialbiome && (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25);
                bool undergroundJungle = (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25) && spawnInfo.Player.ZoneJungle;
                bool undergroundEvil = (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25) && (spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson);
                bool undergroundHoly = (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25) && spawnInfo.Player.ZoneHallow;
                if (!Main.dayTime && Main.bloodMoon && surface)
                {
                    if (Main.rand.NextBool(250))
                    {
                        return 1;
                    }
                }
            }
            return 0;
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
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
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

        public override void HitEffect(int hitDirection, double damage)
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
                for (int num36 = 0; num36 < 50; num36++)
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
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.DeathBag>()));
        }
        public override void OnKill()
        {
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 4);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.GreatMagicShieldScroll>(), 4);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.MagicBarrierScroll>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.MidnightRainbowDye, 5);
                if (!tsorcRevampWorld.Slain.ContainsKey(NPC.type))
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 15000);
                }
            }
        }
    }
}