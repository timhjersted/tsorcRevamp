using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class CorruptedHornet : ModNPC
    {
        int cursedFlameDamage = 50;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            AnimationType = 42;
            AIType = 42;
            NPC.width = 34;
            NPC.height = 12;
            NPC.knockBackResist = .2f;
            NPC.value = 4000; // life / 2.5 : was 113
            NPC.aiStyle = 5;
            NPC.timeLeft = 750;
            NPC.damage = 48;
            NPC.defense = 40;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.noGravity = true;
            NPC.lifeMax = 1000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.CorruptedHornetBanner>();
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            cursedFlameDamage = (int)(cursedFlameDamage * tsorcRevampWorld.SHMScale);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneOverworldHeight && Main.rand.NextBool(2))
                {
                    return 1;
                }
                else return 0;
            }
            else return 0;
        }



        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Poisoned, 120 * 60, false);

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Confused, 3 * 60, false); 
               
            }
        }


        public override void AI()
        {
            NPC.ai[3]++;
            if (NPC.ai[3] >= 200) //200 was 240
            {
                if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    float num87 = 8f;
                    Vector2 vector12 = new Vector2(NPC.position.X + (float)NPC.width * 0.5f, NPC.position.Y + (float)(NPC.height / 2));
                    float num88 = Main.player[NPC.target].position.X + (float)Main.player[NPC.target].width * 0.5f - vector12.X + (float)Main.rand.Next(-20, 21);
                    float num89 = Main.player[NPC.target].position.Y + (float)Main.player[NPC.target].height * 0.5f - vector12.Y + (float)Main.rand.Next(-20, 21);
                    if ((num88 < 0f && NPC.velocity.X < 0f) || (num88 > 0f && NPC.velocity.X > 0f))
                    {
                        float num90 = (float)Math.Sqrt((double)(num88 * num88 + num89 * num89));
                        num90 = num87 / num90;
                        num88 *= num90;
                        num89 *= num90;
                        int num93 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector12.X, vector12.Y, num88, num89, ProjectileID.CursedFlameHostile, cursedFlameDamage, 0f, Main.myPlayer);
                        Main.projectile[num93].timeLeft = 300;
                        NPC.ai[3] = 101f;
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.ai[3] = 0f;
                    }
                }
                NPC.ai[3] = 0;
            }
        }
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Corrupt Hornet Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Corrupt Hornet Gore 2").Type, 1.1f);
            }
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 100, default(Color), 1f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 100, default(Color), 1f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 100, default(Color), 1f);
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FlameOfTheAbyss>(), 2));
        }
    }
}