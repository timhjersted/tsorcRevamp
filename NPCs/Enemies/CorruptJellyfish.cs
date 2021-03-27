using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
    class CorruptedJellyfish : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Corrupt Jellyfish");
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.PinkJellyfish];
        }

        public override void SetDefaults()
        {
            npc.CloneDefaults(NPCID.PinkJellyfish);
            animationType = NPCID.PinkJellyfish;
            aiType = NPCID.PinkJellyfish;
            npc.lifeMax = 235;
            npc.damage = 70;
            npc.scale = 1f;
            npc.knockBackResist = .7f;
            npc.value = 380;
            npc.defense = 40;
            npc.buffImmune[BuffID.Confused] = true;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (Main.hardMode /*|| ModWorld.superHardmode*/)
            {
                if ((spawnInfo.player.ZoneCorrupt && spawnInfo.water) && (spawnInfo.player.ZoneRockLayerHeight || spawnInfo.player.ZoneDirtLayerHeight)) //I've added dirt layer as otherwise they would be practically non-existent on the tsorc map
                {
                    chance = .25f;
                }
            }
            return chance;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.rand.Next(4) == 0)
            {
                target.AddBuff(BuffID.ObsidianSkin, 1800, true);
                target.AddBuff(BuffID.Bleeding, 3600, true);
            }
        }
        public override void NPCLoot()
        {
            //Gore.NewGore(npc.position, npc.velocity, "Cloud Bat Gore", 1f); Not added gore, never worked with it before -C
        }
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 15; i++)
            {
                int dustType = 98;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (npc.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, 98, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 70, default(Color), .8f);
                }
            }
        }

        //TO-DO
        /*public void AI()
        {
            for (int i = 0; i < npc.buffType.Length; i++)
            {
                if (npc.buffType[i] == Config.buffID["Frozen"])
                {
                    npc.DelBuff(i);
                    i = 0;
                }
            }
            npc.AI(true);
        }*/
    }
}