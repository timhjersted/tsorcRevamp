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
            aiType = NPCID.GraniteGolem;
            npc.damage = 20;
            npc.lifeMax = 60;
            npc.defense = 14;
            npc.value = 250;
            npc.knockBackResist = 0.35f;
            animationType = NPCID.GraniteGolem;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.StoneGolemBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            return SpawnCondition.Cavern.Chance * 0.15f;
        }

        public override void NPCLoot() {
            Item.NewItem(npc.getRect(), ItemID.StoneBlock, Main.rand.Next(5, 11));
            Item.NewItem(npc.getRect(), ItemID.IronOre, Main.rand.Next(1, 4)); //for ironskin potions/other
            if (Main.rand.Next(6) == 0) Item.NewItem(npc.getRect(), ItemID.EndurancePotion);
            if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.Lifegem>());
            if (Main.rand.Next(15) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.RadiantLifegem>());



        }
    }
}
