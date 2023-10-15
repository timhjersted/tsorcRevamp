using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class HydrisElemental : ModNPC
    {
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
            NPC.lifeMax = 1000;
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
                target.AddBuff(BuffID.Weak, 30 * 60, false); //weak
            }

        }



        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 4.8f, 0.08f, canTeleport: true, enragePercent: 0.4f, enrageTopSpeed: 5.6f);
            tsorcRevampAIs.LeapAtPlayer(NPC, 6, 5, 2, 128);
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
            npcLoot.Add(new CommonDrop(ModContent.ItemType<DyingWindShard>(), 100, 1, 1, 50));
        }
    }
}