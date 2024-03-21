using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Lore
{
    class Epilogue : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/DeathStrikeScroll";
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Pink;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Text.Contains("obscured") || tooltips[i].Text.Contains("模糊不清"))
                {
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new Terraria.ModLoader.Config.NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>())))
                    {
                        tooltips[i].Text = LangUtils.GetTextValue("Items.Epilogue.Obscured");
                    }
                    else
                    {
                        tooltips[i].OverrideColor = Main.DiscoColor;
                    }
                }
            }
        }
    }
}
