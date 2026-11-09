using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Weapons.Classless;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Content.Items.Weapons.Summon;

namespace tsorcRevamp.NPCs.Enemies
{
    public class BarrowWight : BarrowWightBase
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.npcSlots = 3;
            NPC.defense = 0;
            NPC.lifeMax = 900; // EnemyBalance owns 900 / 3500 / 18000 across progression.
            NPC.value = 900;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BarrowWightBanner>();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OilPot>(), 1, 1, 4));
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 55));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.SpelunkerPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 40));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpiritBell>(), 20));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofFlight));
            npcLoot.Add(hmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.SuperHardmodeRule, ModContent.ItemType<BarrowBlade>(), 5));
        }
    }
}
