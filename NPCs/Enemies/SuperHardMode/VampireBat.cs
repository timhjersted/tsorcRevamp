using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class VampireBat : ModNPC
    {

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
        }

        public override void SetDefaults()
        {
            NPC.width = 48;
            NPC.height = 36;
            NPC.aiStyle = 14;
            AIType = NPCID.CaveBat;
            NPC.timeLeft = 750;
            NPC.damage = 63;
            NPC.defense = 70;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath4;
            NPC.lifeMax = 800;
            NPC.scale = 1;
            NPC.knockBackResist = 0.4f;
            NPC.value = 2000; // life / 2 / 2 bc simple : not changed
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.VampireBatBanner>();
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player p = spawnInfo.Player;
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (p.ZoneCorrupt || p.ZoneCrimson)
                {
                    return 0.125f;
                }
                else if (Underworld(p))
                {
                    return 0.0167f;
                }
                else if (p.ZoneHallow)
                {
                    return 0.03f;
                }
            }
            return 0;
        }

        public override void AI()
        {
            base.AI();

        }

        public int frame = 0;

        public override void FindFrame(int frameHeight)
        {
            NPC.spriteDirection = NPC.direction;

            if (++NPC.frameCounter >= 4)
            {
                ++frame;
                NPC.frame.Y = frame * frameHeight;
                NPC.frameCounter = 0;

                if (frame >= 7)
                {
                    frame = 0;
                }
            }
        }

        public override void OnKill()
        {
            base.OnKill();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.MoonLeech, 10 * 60);
            target.AddBuff(BuffID.Bleeding, 60 * 60);
        }
    }
}
