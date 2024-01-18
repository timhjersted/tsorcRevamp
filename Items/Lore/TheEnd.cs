using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Lore
{
    class TheEnd : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/DeathStrikeScroll";
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.maxStack = 1;
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
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())))
                    {
                        tooltips[i].Text = LangUtils.GetTextValue("Items.TheEnd.Obscured");
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
