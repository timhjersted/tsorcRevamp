using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
    class LichKingDisciple : ModNPC
    {
        public override void SetDefaults()
        {
            npc.npcSlots = 1;
            Main.npcFrameCount[npc.type] = 3;
            animationType = 29;
            npc.aiStyle = 0;
            npc.damage = 40;
            npc.defense = 20;
            npc.height = 44;
            npc.boss = true;
            npc.timeLeft = 22500;
            npc.lifeMax = 20000;
            npc.scale = 1;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.lavaImmune = true;
            npc.value = 20000;
            npc.width = 28;
            npc.knockBackResist = 0.2f;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
        }


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lich King Disciple");
        }


        int frozenSawDamage = 33;
        int crazedPurpleCrushDamage = 27;

        //We can override this even further on a per-NPC basis here
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.damage = (int)(npc.damage * 1.3 / 2);
            npc.defense = npc.defense += 12;
            npc.lifeMax = (int)(npc.lifeMax * 1.3 / 2);
            frozenSawDamage = (int)(frozenSawDamage * 1.3 / 2);
            crazedPurpleCrushDamage = (int)(crazedPurpleCrushDamage * 1.3 / 2);
        }

        bool OptionSpawned = false;
        int OptionId = 0;


        #region AI
        public override void AI()
        {
            if (OptionSpawned == false)
            {
                OptionId = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<LichKingSerpentHead>(), npc.whoAmI);
                if (Main.netMode == NetmodeID.Server && OptionId < 200)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, OptionId, 0f, 0f, 0f, 0);
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
                    float num48 = 2f;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    int type = ModContent.ProjectileType<Projectiles.Enemy.FrozenSaw>();
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, frozenSawDamage, 0f);
                    Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 20);
                    npc.ai[0] = 0;
                    npc.ai[2]++;
                }
            }

            if (npc.ai[1] >= 10)
            {
                npc.velocity.X *= 0.87f;
                npc.velocity.Y *= 0.17f;
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
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.TargetClosest(true);
                }
                if (Main.player[npc.target].dead)
                {
                    npc.position.X = 0;
                    npc.position.Y = 0;
                    if (npc.timeLeft > 10)
                    {
                        npc.timeLeft = 5;
                        return;
                    }
                }
                else
                {


                    Player Pt = Main.player[npc.target];
                    Vector2 NC = npc.position + new Vector2(npc.width / 2, npc.height / 2);
                    Vector2 PtC = Pt.position + new Vector2(Pt.width / 2, Pt.height / 2);
                    npc.position.X = Pt.position.X + (float)((600 * Math.Cos(npc.ai[3])) * -1);
                    npc.position.Y = Pt.position.Y - 65 + (float)((30 * Math.Sin(npc.ai[3])) * -1);

                    float MinDIST = 300f;
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
                    npc.velocity.X = (float)(Math.Cos(rotation) * 12) * -1;
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 12) * -1;


                }
            }

            //end of W1k's Death code

            //beginning of Omnir's Ultima Weapon projectile code

            npc.ai[3]++;
            /** In the original mod, this attack would just never happen at all. With some editing it does, but... it adds so much projectile spam to the fight that i'm leaving it disabled for now.
            if (npc.ai[3] >= 100) //how often the crystal attack can happen in frames per second
            {
                if (Main.rand.Next(2) == 0 && !(ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)) //1 in 2 chance boss will use attack when it flies down on top of you
                {
                    float num48 = 5f;
                    Vector2 vector9 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 470 + (npc.height / 2));
                    float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                    float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                    num51 = num48 / num51;
                    speedX *= num51;
                    speedY *= num51;
                    int type = ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>();//44;//0x37; //14;
                    int num54 = Projectile.NewProjectile(vector9.X, vector9.Y, speedX, speedY, type, crazedPurpleCrushDamage, 0f);
                    Main.projectile[num54].timeLeft = 1750;
                    //Main.projectile[num54].aiStyle = 4;
                    Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 25);
                    npc.ai[3] = 0;
                }

                if (Main.rand.Next(15) == 0) //1 in 20 chance boss will summon an NPC
                {
                    int Random = Main.rand.Next(80);
                    int Paraspawn = 0;
                    if (Random == 0) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X - 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, NPCID.IlluminantBat, 0);
                    if (Random == 0) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X + 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, NPCID.IlluminantBat, 0);
                    Main.npc[Paraspawn].velocity.X = npc.velocity.X;
                    npc.active = true;

                }
            }**/

            npc.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (npc.ai[3] >= 800)
            {
                if (npc.ai[1] == 0) npc.ai[1] = 1;
                else npc.ai[1] = 0;
            }




            if (Main.player[npc.target].dead)
            {
                npc.velocity.Y -= 0.04f;
                if (npc.timeLeft > 10)
                {
                    npc.timeLeft = 10;
                    return;
                }
            }
        }
        #endregion

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
            if (npc.life <= 0)
            {
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 1"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);
            }
            if (!tsorcRevampWorld.Slain.ContainsKey(npc.type))
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 2000);
            }


        }
    }
}