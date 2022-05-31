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
            NPC.npcSlots = 1;
            NPC.width = 152;
            NPC.height = 152;
            NPC.aiStyle = 23;
            NPC.timeLeft = 22500;
            NPC.knockBackResist = 0;
            NPC.damage = 190;
            NPC.defense = 75;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 7500;
            NPC.value = 10000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
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
