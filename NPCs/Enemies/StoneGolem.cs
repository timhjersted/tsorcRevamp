using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class StoneGolem : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.GraniteGolem];

        }
        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.GraniteGolem);
            AIType = NPCID.GraniteGolem;
            NPC.damage = 20;
            NPC.lifeMax = 60;
            NPC.defense = 14;
            NPC.value = 250;
            NPC.knockBackResist = 0.35f;
            AnimationType = NPCID.GraniteGolem;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.StoneGolemBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return Terraria.ModLoader.Utilities.SpawnCondition.Cavern.Chance * 0.15f;
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.StoneBlock, Main.rand.Next(5, 11));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronOre, Main.rand.Next(1, 4)); //for ironskin potions/other
            if (Main.rand.NextBool(6)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.EndurancePotion);
            if (Main.rand.NextBool(5)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.Lifegem>());
            if (Main.rand.NextBool(15)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.RadiantLifegem>());



        }
    }
}
