using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Items.BossBags
{
    public class GwynRewardBagLoot : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            if (item.type != ModContent.ItemType<GwynBag>())
            {
                return;
            }

            // global:: bypasses BOTH the Mod-class/namespace collision AND the name clash with the
            // old NPCs.Bosses.SuperHardMode.SwordOfLordGwyn guardian (slated for removal).
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<global::tsorcRevamp.Items.Weapons.Melee.Broadswords.SwordOfLordGwyn>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<LordGwynHelm>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<LordGwynArmor>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<LordGwynLeggings>()));
        }
    }

    public class GwynRewardNPCLoot : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == ModContent.NPCType<Gwyn>())
            {
                npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<GwynBag>()));
            }
        }
    }
}
