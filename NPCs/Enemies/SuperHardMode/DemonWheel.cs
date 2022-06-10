using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class DemonWheel : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 34;
            NPC.height = 34;
            NPC.aiStyle = 21;
            Main.npcFrameCount[NPC.type] = 8;
            NPC.timeLeft = 750;
            AIType = 72;
            NPC.damage = 120;
            NPC.defense = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 1000;
            NPC.alpha = 100;
            NPC.scale = 1.2f;
            NPC.noGravity = true;
            NPC.behindTiles = true;
            NPC.dontTakeDamage = true;

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
        }
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage / 2);
        }

        //Super high because they seem to like spawning, rolling out of range, and instantly despawning
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            if (player.ZoneDungeon && tsorcRevampWorld.SuperHardMode) return 1;

            if (player.ZoneDungeon && Main.hardMode) return 0.2f;

            if ((player.ZoneCorrupt || player.ZoneCrimson) && tsorcRevampWorld.SuperHardMode) return 1;

            if (player.ZoneUnderworldHeight && tsorcRevampWorld.SuperHardMode) return 0.5f;


            return 0;
        }

        int lifespan = 1800;
        public override void AI()
        {
            lifespan--;
            if(lifespan == 0)
            {
                NPC.active = false;

                for(int i = 0; i < 60; i++)
                {
                    Dust.NewDustPerfect(NPC.Center, DustID.ShadowbeamStaff, Main.rand.NextVector2CircularEdge(15, 15), default, default, 1.5f);
                }
            }


            

            float red = 1.0f;
            float green = 0.0f;
            float blue = 1.0f;

            Lighting.AddLight((int)((NPC.position.X + (float)(NPC.width / 2)) / 16f), (int)((NPC.position.Y + (float)(NPC.height / 2)) / 16f), red, green, blue);

        }

    }
}