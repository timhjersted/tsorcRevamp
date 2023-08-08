using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class ObsidianJellyfish : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.PinkJellyfish];
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.PinkJellyfish);
            AnimationType = NPCID.PinkJellyfish;
            AIType = NPCID.PinkJellyfish;
            NPC.lifeMax = 5;
            NPC.damage = 35;
            NPC.scale = 1f;
            NPC.knockBackResist = 0.9f;
            NPC.value = 300;
            NPC.defense = 999;
            NPC.buffImmune[BuffID.Confused] = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ObsidianJellyfishBanner>();

            if (Main.hardMode)
            {
                NPC.knockBackResist = 0.7f;
                NPC.lifeMax = 10;
                NPC.defense = 999;
                NPC.value = 600;
                NPC.damage = 50;
            }
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool TropicalOcean = spawnInfo.Player.position.X < 3600 && spawnInfo.Water;
            float chance = 0;

            if (Main.hardMode && TropicalOcean && spawnInfo.Player.ZoneJungle) return 0.045f;

            if (!Main.hardMode /*|| ModWorld.superHardmode*/)
            {
                if ((spawnInfo.Water && (spawnInfo.Player.ZoneMeteor || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) && (spawnInfo.Player.ZoneRockLayerHeight || spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneOverworldHeight)) //I've added dirt layer as otherwise they would be practically non-existent on the tsorc map
                {
                    chance = .35f;
                }
            }

            if (Main.hardMode /*|| ModWorld.superHardmode*/)
            {
                if ((spawnInfo.Water && (spawnInfo.Player.ZoneMeteor || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneDungeon)) && (spawnInfo.Player.ZoneRockLayerHeight || spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneOverworldHeight)) //I've added dirt layer as otherwise they would be practically non-existent on the tsorc map
                {
                    chance = .45f;
                }
            }
            return chance;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.ObsidianSkin, 30 * 60, true);

            if (Main.rand.NextBool(4) && !Main.hardMode)
            {
                target.AddBuff(BuffID.Bleeding, 60 * 60, true);
            }

            if (Main.hardMode)
            {
                target.AddBuff(BuffID.Bleeding, 60 * 60, true);
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 15; i++)
            {
                int DustType = 98;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.04f;
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