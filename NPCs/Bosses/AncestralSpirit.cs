using FullSerializer.Internal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Lore;
using tsorcRevamp.Items.Placeable.Relics;
using tsorcRevamp.Items.Placeable.Trophies;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Vanity;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    class AncestralSpirit : ModNPC
    {
        NPCDespawnHandler despawnHandler;

        public override void SetStaticDefaults()
        {
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frozen] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Deerclops);
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.AncestralSpirit.DespawnHandler"), Color.Gold, DustID.GoldFlame);
        }
        public override void AI()
        {
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.AncestralSpiritBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<AncestralSpiritMask>(), 7));
            npcLoot.Add(notExpertCondition);
            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<AncestralSpiritRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AncestralSpiritTrophy>(), 10));
        }
    }
}