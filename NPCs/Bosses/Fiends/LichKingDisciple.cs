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
            npc.lifeMax = 45000;
            npc.scale = 1;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.lavaImmune = true;
            npc.value = 40000;
            npc.width = 28;
            npc.knockBackResist = 0.2f;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            despawnHandler = new NPCDespawnHandler(DustID.GreenFairy);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lich King Disciple");
        }

        int frozenSawDamage = 75;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.damage = (int)(npc.damage * 1.3 / 2);
            npc.defense = npc.defense += 12;
        }

        #region AI
        NPCDespawnHandler despawnHandler;
        bool OptionSpawned = false;
        int OptionId = 0;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);
            if (OptionSpawned == false)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    OptionId = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<LichKingSerpentHead>(), npc.whoAmI);
                    Main.npc[OptionId].velocity.Y = -10;
                }   
                OptionSpawned = true;
            }

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

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.ai[0] >= 5 && npc.ai[2] < 3)
                {
                    Vector2 projectileVelocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 2);
                    Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projectileVelocity.X, projectileVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.FrozenSaw>(), frozenSawDamage, 0f, Main.myPlayer);

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

                npc.ai[1] = 0;
                npc.ai[2] = 0;
                
                Player Pt = Main.player[npc.target];
                npc.position.X = Pt.position.X + (float)((600 * Math.Cos(npc.ai[3])) * -1);
                npc.position.Y = Pt.position.Y - 65 + (float)((30 * Math.Sin(npc.ai[3])) * -1);
                npc.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));

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

                npc.velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Pt.Center, 12);           
            }

            //end of W1k's Death code            
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