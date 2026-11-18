using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Lore;
using tsorcRevamp.Content.Items.Materials.Souls;

namespace tsorcRevamp.NPCs.Bosses.Ornsmough
{
    // PLACEHOLDER sprite (copy of GwynBag) — replace with bespoke art.
    /// <summary>Golem's reward pool plus the four souls of each Warden: 3 armor pieces + 1 weapon per set.
    /// Outside Adventure Mode the AddedBossBagLoot row also gives Golem's 1-in-3 Picksaw.</summary>
    public class OrnsmoughBag : BossBag
    {
        public const int SoulsOfEachWarden = 4;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.BossBag[Type] = true;
        }

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfOrnstein>(), 1, SoulsOfEachWarden, SoulsOfEachWarden));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfSmough>(), 1, SoulsOfEachWarden, SoulsOfEachWarden));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrestOfStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BrokenPicksaw>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.StyngerBolt, 1, 200, 300));
            itemLoot.Add(ItemDropRule.Common(ItemID.Stynger));
            itemLoot.Add(ItemDropRule.Common(ItemID.PossessedHatchet));
            itemLoot.Add(ItemDropRule.Common(ItemID.SunStone));
            itemLoot.Add(ItemDropRule.Common(ItemID.EyeoftheGolem));
            itemLoot.Add(ItemDropRule.Common(ItemID.HeatRay));
            itemLoot.Add(ItemDropRule.Common(ItemID.StaffofEarth));
            itemLoot.Add(ItemDropRule.Common(ItemID.GolemFist));
            itemLoot.Add(ItemDropRule.Common(ItemID.ShinyStone));
            itemLoot.Add(ItemDropRule.Common(ItemID.BeetleHusk, 1, 40, 50));
        }
    }
}
