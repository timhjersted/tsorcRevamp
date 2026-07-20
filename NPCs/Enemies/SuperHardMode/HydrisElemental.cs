using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class HydrisElemental : ModNPC
    {
        private const float FighterTopSpeed = 3f;
        private const float LeapSpeedX = 3f;
        private const float LeapSpeedY = 3.4f;
        private const float LeapMinimumSpeed = 2.5f;
        private const float LandingTraction = 0.58f;
        private const int LeapCooldownFrames = 75;
        private const int LandingTractionFrames = 16;

        private bool leapInAir;
        private int leapCooldown;
        private int landingTractionTimer;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            NPC.width = 18;
            NPC.height = 40;
            AnimationType = 120;
            NPC.knockBackResist = 0.2f;
            NPC.aiStyle = 3;
            NPC.timeLeft = 750;
            NPC.damage = 50;
            NPC.defense = 42;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lavaImmune = true;
            NPC.lifeMax = 1600;
            NPC.value = 4000; // 120
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.HydrisElementalBanner>();
        }

        //Spawns in the Underground and Cavern before 3.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Water) return 0f;

            if (tsorcRevampWorld.SuperHardMode)
            {
                if ((spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && spawnInfo.Player.position.Y > Main.rockLayer && spawnInfo.Player.position.Y < Main.maxTilesY - 200 && !spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(1500))
                {
                    return 1;
                }
                else return 0;
            }
            else return 0;
        }





        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Battle, 30 * 60, false); //battle
                target.AddBuff(BuffID.Weak, 40 * 60, false); //weak
            }

        }



        public override void AI()
        {
            if (leapCooldown > 0)
            {
                leapCooldown--;
            }

            if (leapInAir && NPC.velocity.Y == 0f)
            {
                leapInAir = false;
                landingTractionTimer = LandingTractionFrames;
            }

            tsorcRevampAIs.FighterAI(NPC, FighterTopSpeed, 0.08f, canTeleport: true, enragePercent: 0.5f, enrageTopSpeed: 5f);

            if (landingTractionTimer > 0 && NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= LandingTraction;
                if (Math.Abs(NPC.velocity.X) < 0.1f)
                {
                    NPC.velocity.X = 0f;
                }
                landingTractionTimer--;
            }

            if (leapCooldown <= 0)
            {
                float velocityYBeforeLeap = NPC.velocity.Y;
                tsorcRevampAIs.LeapAtPlayer(NPC, LeapSpeedX, LeapSpeedY, LeapMinimumSpeed, 128);

                if (velocityYBeforeLeap == 0f && NPC.velocity.Y < 0f)
                {
                    leapInAir = true;
                    leapCooldown = LeapCooldownFrames;
                    landingTractionTimer = 0;
                }
            }
        }

        public override void OnKill()
        {

            Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, 0.3f, 0.3f, 200, default(Color), 1f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 3f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
            Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        { 
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<WhiteTitanite>(), 4, 1, 2, 50));
        }
    }
}
