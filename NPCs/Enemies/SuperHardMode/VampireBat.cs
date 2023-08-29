using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static tsorcRevamp.SpawnHelper;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode;

class VampireBat : ModNPC
{

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Vampire Bat");
        Main.npcFrameCount[NPC.type] = 8;
    }

    public override void SetDefaults()
    {
        NPC.width = 48;
        NPC.height = 36;
        NPC.aiStyle = 14;
        AIType = NPCID.CaveBat;
        NPC.timeLeft = 750;
        NPC.damage = 98;
        NPC.defense = 70;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath4;
        NPC.lifeMax = 1500;
        NPC.scale = 1;
        NPC.knockBackResist = 0.5f;
        NPC.value = 1200;
        Banner = NPC.type;
        BannerItem = ModContent.ItemType<Banners.VampireBatBanner>();
    }

    public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
    {
        NPC.lifeMax = (int)(NPC.lifeMax / 2);
        NPC.damage = (int)(NPC.damage / 2);
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

    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 3600);
        target.AddBuff(BuffID.Poisoned, 3600);
        target.AddBuff(BuffID.Bleeding, 3600);
    }
}
