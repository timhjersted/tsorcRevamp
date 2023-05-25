using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Weapons.Magic;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Items.Weapons.Melee;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
    class LichKingDisciple : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 0;
            NPC.defense = 52;
            NPC.height = 44;
            NPC.boss = true;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 50000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = 40000;
            NPC.width = 28;
            NPC.knockBackResist = 0.2f;
            despawnHandler = new NPCDespawnHandler(DustID.GreenFairy);
        }


        int frozenSawDamage = 105;
        //chaos
        int holdTimer = 0;
        #region AI
        NPCDespawnHandler despawnHandler;
        bool OptionSpawned = false;
        int OptionId = 0;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (OptionSpawned == false)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    OptionId = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<LichKingSerpentHead>(), NPC.whoAmI);
                    Main.npc[OptionId].velocity.Y = -10;
                }
                OptionSpawned = true;
            }

            NPC.ai[0]++; // Timer Scythe
            NPC.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            Player player = Main.player[NPC.target];
            //chaos code: announce proximity debuffs once
            if (holdTimer > 1)
            {
                holdTimer--;
            }
            //Proximity Debuffs
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) < 300)
            {
                player.AddBuff(BuffID.Ichor, 10 * 60, false);
                //player.AddBuff(BuffID.PotionSickness, 60, false);

                if (holdTimer <= 0)
                {
                    UsefulFunctions.BroadcastText("The Lich King's Disciple emanates a miasma of Ichor around him!", 199, 21, 133);//medium violet red
                    holdTimer = 9000;
                }

            }

            //causes potion sickness at 20k health
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) < 320 && NPC.life <= NPC.lifeMax / 5 * 2)
            {
                player.AddBuff(BuffID.PotionSickness, 1 * 60, false);
                player.AddBuff(BuffID.Hunter, 1 * 60, false);
                player.AddBuff(BuffID.Blackout, 1 * 60, false);
            }
            if (NPC.life > NPC.lifeMax / 5 * 2)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X - 70, (float)NPC.position.Y - 60), NPC.width * 7, NPC.height * 7 , DustID.Wraith, NPC.velocity.X, NPC.velocity.Y, 150, Color.Black, 2f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= NPC.lifeMax / 5 * 2)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X - 70, (float)NPC.position.Y-60), NPC.width * 7, NPC.height * 7, DustID.Wraith, NPC.velocity.X, NPC.velocity.Y, 100, Color.Yellow, 3f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (NPC.ai[0] >= 5 && NPC.ai[2] < 3)
                {
                    Vector2 projectileVelocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 2);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projectileVelocity.X, projectileVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.FrozenSaw>(), frozenSawDamage, 0f, Main.myPlayer);

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    NPC.ai[0] = 0;
                    NPC.ai[2]++;
                }
            }

            if (NPC.ai[1] >= 10)
            {
                NPC.velocity.X *= 0.87f;
                NPC.velocity.Y *= 0.17f;
            }

            if ((NPC.ai[1] >= 200 && NPC.life > NPC.lifeMax / 50) || (NPC.ai[1] >= 120 && NPC.life <= NPC.lifeMax / 50))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 4f);
                    Main.dust[dust].noGravity = false;
                }

                NPC.ai[1] = 0;
                NPC.ai[2] = 0;

                Player Pt = Main.player[NPC.target];
                NPC.position.X = Pt.position.X + (float)((600 * Math.Cos(NPC.ai[3])) * -1);
                NPC.position.Y = Pt.position.Y - 65 + (float)((30 * Math.Sin(NPC.ai[3])) * -1);
                NPC.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));

                float MinDIST = 300f;
                float MaxDIST = 600f;
                Vector2 Diff = NPC.position - Pt.position;
                if (Diff.Length() > MaxDIST)
                {
                    Diff *= MaxDIST / Diff.Length();
                }
                if (Diff.Length() < MinDIST)
                {
                    Diff *= MinDIST / Diff.Length();
                }
                NPC.position = Pt.position + Diff;

                NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Pt.Center, 12);
            }

            //end of W1k's Death code            
        }
        #endregion

        public override void FindFrame(int currentFrame)
        {

            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
            }

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
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
                }
            }
        }
    }
}