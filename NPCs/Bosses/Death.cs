using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class Death : ModNPC
    {
        public override void SetDefaults()
        {

            Main.npcFrameCount[npc.type] = 6;
            npc.npcSlots = 10;
            npc.aiStyle = 0;
            npc.width = 80;
            npc.height = 100;
            npc.damage = 250;
            npc.defense = 45;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath6;
            npc.lifeMax = 25000;
            npc.friendly = false;
            npc.boss = true;
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.knockBackResist = 0;
            npc.value = 150000;

            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            bossBag = ModContent.ItemType<Items.BossBags.DeathBag>();
            despawnHandler = new NPCDespawnHandler("Death claims you at last...", Color.DarkMagenta, DustID.Demonite);
        }


        int shadowShotDamage = 75;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.damage = (int)(npc.damage * 1.3 / 2);
            npc.defense = npc.defense += 12;
            npc.lifeMax = (int)(npc.lifeMax * 1.3 / 2);
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
                bool nospecialbiome = !spawnInfo.player.ZoneJungle && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson && !spawnInfo.player.ZoneHoly && !spawnInfo.player.ZoneMeteor && !spawnInfo.player.ZoneDungeon; // Not necessary at all to use but needed to make all this work.

                bool sky = nospecialbiome && ((double)spawnInfo.player.position.Y < Main.worldSurface * 0.44999998807907104);
                bool surface = nospecialbiome && !sky && (spawnInfo.player.position.Y <= Main.worldSurface);
                bool underground = nospecialbiome && !surface && (spawnInfo.player.position.Y <= Main.rockLayer);
                bool underworld = (spawnInfo.player.position.Y > Main.maxTilesY - 190);
                bool cavern = nospecialbiome && (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25);
                bool undergroundJungle = (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25) && spawnInfo.player.ZoneJungle;
                bool undergroundEvil = (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25) && (spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson);
                bool undergroundHoly = (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25) && spawnInfo.player.ZoneHoly;
                if (!Main.dayTime && Main.bloodMoon && surface)
                {
                    if (Main.rand.Next(250) == 0)
                    {
                        return 1;
                    }
                }
            }
            return 0;
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);
            npc.netUpdate = false;
            npc.ai[0]++; // Timer Scythe
            npc.ai[1]++; // Timer Teleport
                         // npc.ai[2] Shots

            if (npc.life > 5000)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 200, color, 4f);
                Main.dust[dust].noGravity = true;
            }
            else if (npc.life <= 5000)
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 140, color, 6f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != 2)
            {
                if (npc.ai[0] >= 12 && npc.ai[2] < 5)
                {
                    float num48 = 0.5f;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    int type = ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>();
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, shadowShotDamage, 0f, 0);
                    npc.ai[0] = 0;
                    npc.ai[2]++;
                }
            }

            if (npc.ai[1] >= 40)
            {
                npc.velocity.X *= 0.97f;
                npc.velocity.Y *= 0.97f;
            }

            if ((npc.ai[1] >= 150 && npc.life > 2000) || (npc.ai[1] >= 100 && npc.life <= 2000))
            {
                Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 8);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, color, 4f);
                    Main.dust[dust].noGravity = true;
                }
                //if (Main.netMode != 1)
                //{
                npc.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                //}
                npc.ai[2] = 0;
                npc.ai[1] = 0;
                
               
                npc.position.X = Main.player[npc.target].position.X + (float)((600 * Math.Cos(npc.ai[3])) * -1);
                npc.position.Y = Main.player[npc.target].position.Y + (float)((600 * Math.Sin(npc.ai[3])) * -1);
                Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                npc.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
                npc.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
                
            }

            if (npc.velocity.X > 0)
            {
                npc.spriteDirection = 1;
            }
            else npc.spriteDirection = -1;

            //npc.netUpdate = true;
        }

        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }
            if ((npc.velocity.X > -2 && npc.velocity.X < 2) && (npc.velocity.Y > -2 && npc.velocity.Y < 2))
            {
                npc.frameCounter = 0;
                npc.frame.Y = 0;
            }
            else
            {
                npc.frameCounter += 1.0;
            }
            if (npc.frameCounter >= 1.0)
            {
                npc.frame.Y = npc.frame.Y + num;
                npc.frameCounter = 0.0;
            }
            if (npc.frame.Y >= num * Main.npcFrameCount[npc.type])
            {
                npc.frame.Y = 0;
            }
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if(npc.life <= 0)
            {
                Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));

                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Death Gore 1"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Death Gore 2"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Death Gore 3"), 1f);
                for (int num36 = 0; num36 < 50; num36++)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, color, 4f);
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
        public override void NPCLoot()
        {
            if (Main.expertMode)
            {
                npc.DropBossBags();
            }
            else
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 4);
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.WallTome>(), 4);
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GuardianSoul>(), 1);
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.BarrierTome>(), 1);
                Item.NewItem(npc.getRect(), ItemID.MidnightRainbowDye, 5);
                if (!tsorcRevampWorld.Slain.ContainsKey(npc.type))
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 15000);
                }
            }
        }
    }
}