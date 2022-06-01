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
            DisplayName.SetDefault("Corrupted Jellyfish");
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.PinkJellyfish];
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.PinkJellyfish);
            AnimationType = NPCID.PinkJellyfish;
            aiType = NPCID.PinkJellyfish;
            NPC.lifeMax = 118;
            NPC.damage = 35;
            NPC.scale = 1f;
            NPC.knockBackResist = .7f;
            NPC.value = 380;
            NPC.defense = 40;
            NPC.buffImmune[BuffID.Confused] = true;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.CorruptedJellyfishBanner>();
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (Main.hardMode /*|| ModWorld.superHardmode*/)
            {
                if ((spawnInfo.Player.ZoneCorrupt && spawnInfo.Water) && (spawnInfo.Player.ZoneRockLayerHeight || spawnInfo.Player.ZoneDirtLayerHeight)) //I've added dirt layer as otherwise they would be practically non-existent on the tsorc map
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
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 15; i++)
            {
                int dustType = 98;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 98, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 70, default(Color), .8f);
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