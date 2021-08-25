using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Accessories;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode {
    class SwordOfLordGwyn : ModNPC {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sword of Lord Gwyn");
        }

        public override void SetDefaults() {
            npc.npcSlots = 100;
            npc.width = 152;
            npc.height = 152;
            npc.aiStyle = 23;
            npc.timeLeft = 22500;
            npc.knockBackResist = 0;
            npc.damage = 190;
            npc.defense = 75;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath6;
            npc.lifeMax = 7500;
            npc.value = 10000;
            npc.noGravity = true;
            npc.noTileCollide = true;
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()))
            {
                for (int i = 0; i < 60; i++)
                {
                    int dustID = Dust.NewDust(npc.position, npc.width, npc.height, 6, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 7f);
                    Main.dust[dustID].noGravity = true;
                }
                npc.active = false;
            }
        }
    }
}
