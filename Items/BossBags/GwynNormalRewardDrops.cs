using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Items.BossBags
{
    public class GwynNormalRewardDrops : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type != ModContent.NPCType<Gwyn>())
            {
                return;
            }

            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            // global:: bypasses BOTH the Mod-class/namespace collision AND the name clash with the
            // old NPCs.Bosses.SuperHardMode.SwordOfLordGwyn guardian (slated for removal).
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<global::tsorcRevamp.Items.Weapons.Melee.Broadswords.SwordOfLordGwyn>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<LordGwynHelm>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<LordGwynArmor>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<LordGwynLeggings>()));
            npcLoot.Add(notExpertCondition);
        }
    }
}
