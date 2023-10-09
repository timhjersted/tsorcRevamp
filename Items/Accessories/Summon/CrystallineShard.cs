using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Summon
{
    [LegacyName("Oxyale")]
    public class CrystallineShard : ModItem
    {
        public float CriticalStrikeChance = 8;
        public static float MaximumMinionIncrease = 2;
        public static float WhipRangeMultiplier = 33.4f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaximumMinionIncrease, WhipRangeMultiplier);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
        }
        public override void UpdateInventory(Player player)
        {
            CriticalStrikeChance = player.maxMinions * 5;
        }
        public override void UpdateEquip(Player player)
        {
            player.maxMinions += 2;
            player.whipRangeMultiplier *= 1f - (WhipRangeMultiplier / 100f);
            CriticalStrikeChance = player.maxMinions * 5;
            player.GetCritChance(DamageClass.Summon) += CriticalStrikeChance;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex1 != -1)
            {
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "CritChance", Language.GetTextValue("Mods.tsorcRevamp.Items.CrystallineShard.CriticalStrikeChance") + CriticalStrikeChance + "%"));
            }
        }
    }
}
