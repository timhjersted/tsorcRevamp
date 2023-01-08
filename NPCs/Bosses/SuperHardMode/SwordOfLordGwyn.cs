using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    class SwordOfLordGwyn : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sword of Lord Gwyn");
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1;
            NPC.width = 152;
            NPC.height = 152;
            NPC.aiStyle = 23;
            //NPC.timeLeft = 22500;
            NPC.knockBackResist = 0;
            NPC.damage = 130;
            NPC.defense = 35;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 50000;
            NPC.value = 10000;
            NPC.noGravity = false;
            NPC.noTileCollide = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 18000, false);
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()))
            {
                for (int i = 0; i < 60; i++)
                {
                    int dustID = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 7f);
                    Main.dust[dustID].noGravity = true;
                }
                NPC.active = false;
            }
        }
    }
}
