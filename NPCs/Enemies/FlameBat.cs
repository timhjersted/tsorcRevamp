using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class FlameBat : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.OnFire
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            NPC.width = 16;
            NPC.height = 18;
            NPC.aiStyle = NPCAIStyleID.Bat;
            NPC.timeLeft = 750;
            NPC.damage = 45;
            NPC.defense = 10;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath4;
            NPC.lifeMax = 115;
            NPC.knockBackResist = 0.55f;
            NPC.noGravity = true;
            NPC.value = 270;
            NPC.lavaImmune = true;
            AnimationType = 93;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.FlameBatBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.ZoneUnderworldHeight && Main.hardMode && Main.rand.NextBool(3))
            {
                return 1;
            }
            return 0;
        }

        public override void AI()
        {
            int num9 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, 0.1f, 0.1f, 100, default(Color), 2f);
            Main.dust[num9].noGravity = true;
        }
    }
}