using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.WyvernMage
{
    [AutoloadBossHead]
    class WyvernMage : ModNPC
    {
        public override void SetDefaults()
        {
            npc.scale = 1;
            npc.npcSlots = 150;
            Main.npcFrameCount[npc.type] = 3;
            npc.width = 28;
            npc.height = 44;
            npc.damage = 20;
            npc.defense = 20;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.lifeMax = 13000;
            npc.timeLeft = 22500;
            npc.friendly = false;
            npc.noTileCollide = false;
            npc.noGravity = true;
            npc.knockBackResist = 0.2f;
            npc.lavaImmune = true;
            npc.boss = true;
            npc.value = 150000;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
            bossBag = ModContent.ItemType<Items.BossBags.WyvernMageBag>();
            despawnHandler = new NPCDespawnHandler("The Wyvern Mage stands victorious...", Color.DarkCyan, DustID.Demonite);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wyvern Mage");
        }

        bool OptionSpawned = false;
        int OptionId = 0;
        int frozenSawDamage = 30;
        int lightningDamage = 70;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)((float)npc.lifeMax * 0.7f * bossLifeScale);
            npc.lifeMax = (int)(npc.lifeMax * 1.3 / 2);
            frozenSawDamage = (int)(frozenSawDamage * 1.3 / 2);
            lightningDamage = (int)(lightningDamage * 1.3 / 2);
        }


        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);
            if (OptionSpawned == false)
            {
                OptionId = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<Bosses.WyvernMage.MechaDragonHead>(), npc.whoAmI);
                if (Main.netMode == NetmodeID.Server && OptionId < 200)
                {
                    //NetMessage.SendData(23, -1, -1, "", OptionId, 0f, 0f, 0f, 0);
                }
                Main.npc[OptionId].velocity.Y = -10;
                OptionSpawned = true;
            }
            

            npc.netUpdate = false;
            npc.ai[0]++; // Timer Scythe
            npc.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (npc.life > 3000)
            {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Wraith, npc.velocity.X, npc.velocity.Y, 150, Color.Black, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (npc.life <= 3000)
            {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Wraith, npc.velocity.X, npc.velocity.Y, 100, Color.Black, 2f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != NetmodeID.Server)
            {
                if (npc.ai[0] >= 5 && npc.ai[2] < 3)
                {
                    float num48 = 4f;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    int type = ModContent.ProjectileType<Projectiles.Enemy.FrozenSaw>();
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, frozenSawDamage, 0f, 0);
                    Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 20);
                    npc.ai[0] = 0;
                    npc.ai[2]++;
                }
            }

            if (npc.ai[1] >= 10)
            {
                npc.velocity.X *= 0.77f;
                npc.velocity.Y *= 0.27f;
            }

            if ((npc.ai[1] >= 200 && npc.life > 2000) || (npc.ai[1] >= 120 && npc.life <= 2000))
            {
                Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 8);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Wraith, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 4f);
                    Main.dust[dust].noGravity = false;
                }
                npc.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                npc.ai[2] = 0;
                npc.ai[1] = 0;
                
                Player Pt = Main.player[npc.target];
                Vector2 NC;
                Vector2 PtC = Pt.position + new Vector2(Pt.width / 2, Pt.height / 2);
                npc.position.X = Pt.position.X + (float)((600 * Math.Cos(npc.ai[3])) * -1);
                npc.position.Y = Pt.position.Y - 45 + (float)((30 * Math.Sin(npc.ai[3])) * -1);

                float MinDIST = 200f;
                float MaxDIST = 600f;
                Vector2 Diff = npc.position - Pt.position;
                if (Diff.Length() > MaxDIST)
                {
                    Diff *= MaxDIST / Diff.Length();
                }
                if (Diff.Length() < MinDIST)
                {
                    Diff *= MinDIST / Diff.Length();
                }
                npc.position = Pt.position + Diff;

                NC = npc.position + new Vector2(npc.width / 2, npc.height / 2);

                float rotation = (float)Math.Atan2(NC.Y - PtC.Y, NC.X - PtC.X);
                npc.velocity.X = (float)(Math.Cos(rotation) * 13) * -1;
                npc.velocity.Y = (float)(Math.Sin(rotation) * 13) * -1;
                
            }

            //end of W1k's Death code


            if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
            {
                #region legacy
                //beginning of Omnir's Ultima Weapon projectile code
                npc.ai[3]++;

                if (npc.ai[3] >= 80) //how often the crystal attack can happen in frames per second
                {

                    if (Main.rand.Next(2) == 0) //1 in 2 chance boss will use attack when it flies down on top of you
                    {
                        float num48 = 0.9f;
                        Vector2 vector9 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 220 + (npc.height / 2));
                        float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                        float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                        if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                        {
                            float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                            num51 = num48 / num51;
                            speedX *= num51;
                            speedY *= num51;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>();//44;//0x37; //14;
                            int num54 = Projectile.NewProjectile(vector9.X, vector9.Y, speedX, speedY, type, lightningDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 250;
                            Main.projectile[num54].aiStyle = 4;
                            Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 25);
                            npc.ai[3] = 0; ;
                        }
                    }

                    if (Main.rand.Next(35) == 0) //1 in 20 chance boss will summon an NPC
                    {
                        int Random = Main.rand.Next(80);
                        int Paraspawn = 0;
                        if (Random == 0) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X - 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, ModContent.NPCType<Enemies.BarrowWight>(), 0);
                        if (Random == 0) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X + 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, ModContent.NPCType<Enemies.BarrowWight>(), 0);
                        Main.npc[Paraspawn].velocity.X = npc.velocity.X;
                        npc.active = true;
                    }
                }
                #endregion legacy
            }
            
            else
            {

                #region revamped

                npc.ai[3]++;
                if (npc.ai[1] == 10) //If the boss just teleported
                {
                    // Main.NewText("Rolling to fire a storm bolt! npc.ai[3] = " + npc.ai[3], 175, 255, 75);
                    if (Main.rand.Next(2) == 0) //1 in 2 chance boss will use attack when it flies down on top of you
                    {
                        float projectileSpeed = 0.01f;
                        Vector2 vector9 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 220 + (npc.height / 2));
                        float speedX = projectileSpeed * ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector9.X); //+ Main.rand.Next(-20, 0x15);
                        float speedY = projectileSpeed * ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector9.Y); // + Main.rand.Next(-20, 0x15);

                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>();//44;//0x37; //14;
                        int projectileID = Projectile.NewProjectile(vector9.X, vector9.Y, speedX, speedY, type, lightningDamage, 0f, Main.myPlayer);
                        Main.projectile[projectileID].timeLeft = 250;

                        Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 25);
                        npc.ai[3] = 0;

                        
                    }
                    
                    if (Main.rand.Next(14) == 0) //1 in 15 chance boss will summon an NPC
                    {
                        int Paraspawn = 0;
                        Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X - 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, ModContent.NPCType<Enemies.BarrowWight>(), 0);
                        Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X + 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, ModContent.NPCType<Enemies.BarrowWight>(), 0);
                        Main.npc[Paraspawn].velocity.X = npc.velocity.X;
                        npc.active = true;
                    }
                }               
            }
            #endregion edited

            npc.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (npc.ai[3] >= 900)
            {
                if (npc.ai[1] == 0) npc.ai[1] = 1;
                else npc.ai[1] = 0;
            }
        }

        #endregion revamped


        public override void FindFrame(int currentFrame)
        {

            if ((npc.velocity.X > -9 && npc.velocity.X < 9) && (npc.velocity.Y > -9 && npc.velocity.Y < 9))
            {
                npc.frameCounter = 0;
                npc.frame.Y = 0;
                if (npc.position.X > Main.player[npc.target].position.X)
                {
                    npc.spriteDirection = -1;
                }
                else
                {
                    npc.spriteDirection = 1;
                }
            }

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



        public override void NPCLoot()
        {
            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
            Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 1"), 1f);
            Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
            Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
            Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);
            Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);

            if (Main.expertMode)
            {
                npc.DropBossBags();
            }
            else
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 2);
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.LionheartGunblade>(), 1);
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.LampTome>(), 1);
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.GemBox>(), 1);
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.PoisonbiteRing>(), 1);
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BloodbiteRing>(), 1);

                if (!(tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<WyvernMage>())))
                { //If the boss has not yet been killed
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 15000); //Then drop the souls
                }
            }
        }
    }
}