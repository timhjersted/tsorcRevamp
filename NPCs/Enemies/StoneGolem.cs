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

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.StoneBlock, 1, 5, 10));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.IronOre, 1, 1, 4));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.EndurancePotion, 6));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.Lifegem>(), 5));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.RadiantLifegem>(), 15));
        }
    }
}
