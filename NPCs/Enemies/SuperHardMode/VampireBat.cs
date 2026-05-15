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
            NPC.timeLeft = 1750;
            NPC.damage = 63;
            NPC.defense = 60;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath4;
            NPC.lifeMax = 1300;
            NPC.scale = 1;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.4f;
            NPC.value = 2000; // life / 2 / 2 bc simple : not changed
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.VampireBatBanner>();
        }
        public override float SpawnChance(NPC.Spawner spawner)
        {
            Player p = spawner.Player;
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
            if (NPC.life < NPC.lifeMax)
            {
                NPC.life += 2;
                if (NPC.life > NPC.lifeMax)
                    NPC.life = NPC.lifeMax;
            }
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

            NPC.life += NPC.lifeMax / 2;
            if (NPC.life > NPC.lifeMax)
            {
                NPC.life = NPC.lifeMax;
            }

            NPC.netUpdate = true;
        }
    }
}
