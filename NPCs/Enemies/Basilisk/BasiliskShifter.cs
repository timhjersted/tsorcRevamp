using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Materials.Souls;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    public class BasiliskShifter : BasiliskPuppet
    {
        protected override FamilyKind Kind => FamilyKind.Shifter;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.width = 38;
            NPC.lifeMax = 350;
            NPC.defense = 55;
            NPC.value = 1750;
            NPC.npcSlots = 5;
            Banner = Type;
            BannerItem = ModContent.ItemType<Banners.BasiliskShifterBanner>();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 2, 1, 2));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight, 3));
            npcLoot.Add(hmCondition);
        }
    }
}
