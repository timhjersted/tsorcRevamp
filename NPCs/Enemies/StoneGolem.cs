using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies {
    public class StoneGolem : ModNPC {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.GraniteGolem];

        }
        public override void SetDefaults() {
            npc.CloneDefaults(NPCID.GraniteGolem);
            npc.damage = 15;
            npc.lifeMax = 85;
            npc.defense = 6;
            animationType = NPCID.GraniteGolem;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.StoneGolemBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            return SpawnCondition.Cavern.Chance * 0.4f;
        }

        public override void NPCLoot() {
            Item.NewItem(npc.getRect(), ItemID.StoneBlock, Main.rand.Next(5, 11));
            Item.NewItem(npc.getRect(), ItemID.IronOre, Main.rand.Next(1, 4)); //for ironskin potions
        }
    }
}
