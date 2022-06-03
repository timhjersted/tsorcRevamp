using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class HydrisElemental : ModNPC
    {
        public override void SetDefaults()
        {

            NPC.npcSlots = 1;
            NPC.width = 18;
            NPC.height = 40;
            AnimationType = 120;
            Main.npcFrameCount[NPC.type] = 15;
            NPC.knockBackResist = 0.2f;

            NPC.aiStyle = 3;
            NPC.timeLeft = 750;
            NPC.damage = 100;
            NPC.defense = 42;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 1700;
            NPC.scale = 1f;
            NPC.value = 1200;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.HydrisElementalBanner>();

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
        }

        //Spawns in the Underground and Cavern before 3.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Water) return 0f;

            if (tsorcRevampWorld.SuperHardMode)
            {
                if ((spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && spawnInfo.Player.position.Y > Main.rockLayer && spawnInfo.Player.position.Y < Main.maxTilesY - 200 && !spawnInfo.Player.ZoneDungeon && Main.rand.Next(1500) == 0)
                {
                    return 1;
                }
                else return 0;
            }
            else return 0;
        }





        public override void OnHitPlayer(Player player, int target, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                player.AddBuff(13, 1800, false); //battle
                player.AddBuff(33, 1800, false); //weak
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

            if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DyingWindShard>());
        }
    }
}