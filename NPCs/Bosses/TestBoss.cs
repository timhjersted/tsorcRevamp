using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses
{
    class TestBoss : ModNPC
    {
        public override void SetDefaults()
        {

            Main.npcFrameCount[npc.type] = 6;
            npc.npcSlots = 10;
            npc.aiStyle = 0;
            npc.width = 80;
            npc.height = 100;
            npc.damage = 1;
            npc.defense = 10;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath6;
            npc.lifeMax = Int32.MaxValue;
            npc.friendly = false;
            npc.boss = true;
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.knockBackResist = 0;
            npc.value = 1;

        }


        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = Int32.MaxValue / 10;
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            if(projectile.type == ModContent.ProjectileType<Projectiles.BlackFirelet>())
            {
                npc.active = false;
            }
        }

        public override void AI()
        {
            npc.life = npc.lifeMax;
        }



        public override void NPCLoot()
        {
           
        }

        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            if(item.type == ItemID.WoodenHammer)
            {
                npc.life = 0;
            }
        }
    }
}