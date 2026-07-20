using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Summon
{
    [LegacyName("Oxyale")]
    public class CrystallineShard : ModItem
    {
        public const int CrystallinePowerPerMinion = 4;
        public const int WhipDmgAmp = (int)(CrystallinePowerPerMinion * 1.25f);
        public const int BadWhipRange = (int)(CrystallinePowerPerMinion * 1.5f);
        public const float MaximumMinionIncrease = 2;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaximumMinionIncrease, CrystallinePowerPerMinion, WhipDmgAmp, BadWhipRange);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
        }
        public override void UpdateEquip(Player player)
        {
            player.maxMinions += 2;
            player.GetModPlayer<tsorcRevampPlayer>().CrystallineShard = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex1 != -1)
            {
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "WhipTagDmgAmp", Language.GetTextValue("Mods.tsorcRevamp.Items.CrystallineShard.Tooltip0", modPlayer.CrystallinePower, 
                    (int)(modPlayer.CrystallinePower * ((float)Items.Accessories.Summon.CrystallineShard.WhipDmgAmp / (float)Items.Accessories.Summon.CrystallineShard.CrystallinePowerPerMinion)),
                    (int)(modPlayer.CrystallinePower * ((float)Items.Accessories.Summon.CrystallineShard.BadWhipRange / (float)Items.Accessories.Summon.CrystallineShard.CrystallinePowerPerMinion)))));
            }
        }
    }
}
