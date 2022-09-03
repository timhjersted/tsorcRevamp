using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses
{
    class TestBoss : ModNPC
    {
        public override void SetDefaults()
        {

            Main.npcFrameCount[NPC.type] = 6;
            NPC.npcSlots = 10;
            NPC.aiStyle = 0;
            NPC.width = 80;
            NPC.height = 100;
            NPC.damage = 1;
            NPC.defense = 10;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = Int32.MaxValue;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0;
            NPC.value = 1;

        }


        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = Int32.MaxValue / 10;
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            if (projectile.type == ModContent.ProjectileType<Projectiles.BlackFirelet>())
            {
                NPC.active = false;
            }
        }

        public override void AI()
        {
            NPC.defense = 35;
            //NPC.life = NPC.lifeMax;
        }



        public override void OnKill()
        {

        }

        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            if (item.type == ItemID.WoodenHammer)
            {
                NPC.life = 0;
            }
        }
    }
}